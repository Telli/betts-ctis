using AutoMapper;
using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentService> _logger;
        private readonly IAuditService _auditService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IActivityTimelineService _activityService;
        private readonly IMessageService _messageService;

        public DocumentService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<DocumentService> logger,
            IAuditService auditService,
            IFileStorageService fileStorageService,
            IActivityTimelineService activityService,
            IMessageService messageService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
            _fileStorageService = fileStorageService;
            _activityService = activityService;
            _messageService = messageService;
        }

        public async Task<PagedResult<DocumentDto>> GetDocumentsAsync(
            int page, 
            int pageSize, 
            string? searchTerm = null, 
            DocumentCategory? category = null, 
            int? clientId = null)
        {
            var query = _context.Documents
                .Include(d => d.Client)
                .Include(d => d.TaxFiling)
                .Include(d => d.UploadedBy)
                .Where(d => !d.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d => 
                    d.OriginalFileName.Contains(searchTerm) ||
                    d.Description.Contains(searchTerm) ||
                    d.Client!.BusinessName.Contains(searchTerm));
            }

            if (category.HasValue)
                query = query.Where(d => d.Category == category.Value);

            if (clientId.HasValue)
                query = query.Where(d => d.ClientId == clientId.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<DocumentDto>>(items);

            return new PagedResult<DocumentDto>
            {
                Items = dtos,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Client)
                .Include(d => d.TaxFiling)
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.DocumentId == id && !d.IsDeleted);

            return document == null ? null : _mapper.Map<DocumentDto>(document);
        }

        public async Task<IEnumerable<DocumentDto>> GetClientDocumentsAsync(int clientId)
        {
            var documents = await _context.Documents
                .Include(d => d.Client)
                .Include(d => d.TaxFiling)
                .Include(d => d.UploadedBy)
                .Where(d => d.ClientId == clientId && !d.IsDeleted)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<DocumentDto>>(documents);
        }

        public async Task<List<DocumentDto>> GetTaxFilingDocumentsAsync(int taxFilingId)
        {
            var documents = await _context.Documents
                .Include(d => d.Client)
                .Include(d => d.UploadedBy)
                .Where(d => d.TaxFilingId == taxFilingId && !d.IsDeleted)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return _mapper.Map<List<DocumentDto>>(documents);
        }

        public async Task<DocumentDto> UploadAsync(UploadDocumentDto dto, IFormFile file, string userId)
        {
            // Validate client exists
            var client = await _context.Clients.FindAsync(dto.ClientId);
            if (client == null)
                throw new InvalidOperationException("Client not found");

            // Validate file
            if (!await ValidateFileAsync(file))
                throw new InvalidOperationException("File validation failed");

            // Save file using storage service
            var subfolder = $"clients/{dto.ClientId}";
            var storagePath = await _fileStorageService.SaveFileAsync(file, file.FileName, subfolder);

            var document = new Document
            {
                ClientId = dto.ClientId,
                TaxYearId = dto.TaxYearId,
                TaxFilingId = dto.TaxFilingId,
                OriginalFileName = file.FileName,
                StoredFileName = Path.GetFileName(storagePath),
                ContentType = file.ContentType,
                Size = file.Length,
                Category = dto.Category,
                Description = dto.Description,
                StoragePath = storagePath,
                UploadedById = userId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Create document verification entry
            var verification = new DocumentVerification
            {
                DocumentId = document.DocumentId,
                Status = DocumentVerificationStatus.Submitted,
                IsRequiredDocument = true,
                TaxFilingId = dto.TaxFilingId,
                StatusChangedById = userId,
                StatusChangedDate = DateTime.UtcNow
            };
            _context.DocumentVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "UPLOAD", "Document", document.DocumentId.ToString(),
                $"Uploaded document {file.FileName} for client {client.BusinessName}");

            // Log activity
            await _activityService.LogDocumentActivityAsync(document.DocumentId, ActivityType.DocumentUploaded);

            // Send notification to assigned associate if client uploaded
            if (client.AssignedAssociateId != null && client.UserId == userId)
            {
                await _messageService.SendSystemMessageAsync(
                    client.AssignedAssociateId,
                    $"New Document Uploaded - {client.BusinessName}",
                    $"Client {client.BusinessName} has uploaded a new document: {file.FileName}\n\nCategory: {dto.Category}\nDescription: {dto.Description}",
                    MessageCategory.DocumentRequest,
                    MessagePriority.Normal
                );
            }

            _logger.LogInformation("Uploaded document {FileName} for client {ClientId}", 
                file.FileName, dto.ClientId);

            return await GetDocumentByIdAsync(document.DocumentId) ??
                throw new InvalidOperationException("Failed to retrieve uploaded document");
        }

        public async Task<DocumentDto> UpdateAsync(int id, UpdateDocumentDto dto, string userId)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                throw new InvalidOperationException("Document not found");

            if (document.IsDeleted)
                throw new InvalidOperationException("Cannot update deleted document");

            var oldValues = new { document.Category, document.Description };

            // Update fields
            if (dto.Category.HasValue)
                document.Category = dto.Category.Value;
            if (dto.Description != null)
                document.Description = dto.Description;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "UPDATE", "Document", document.DocumentId.ToString(),
                $"Updated document {document.OriginalFileName}");

            _logger.LogInformation("Updated document {DocumentId}", id);

            return await GetDocumentByIdAsync(id) ??
                throw new InvalidOperationException("Failed to retrieve updated document");
        }

        public async Task<bool> DeleteAsync(int documentId, string userId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                return false;

            if (document.IsDeleted)
                return false;

            // Soft delete
            document.IsDeleted = true;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(userId, "DELETE", "Document", document.DocumentId.ToString(),
                $"Deleted document {document.OriginalFileName}");

            _logger.LogInformation("Deleted document {DocumentId}", documentId);

            // Delete physical file (async operation)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(document.StoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical file for document {DocumentId}", documentId);
                }
            });

            return true;
        }

        public async Task<(string Path, string ContentType, string FileName)?> GetFileInfoAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null || document.IsDeleted)
                return null;

            if (!await _fileStorageService.FileExistsAsync(document.StoragePath))
                return null;

            // For security, we'll return the file bytes instead of path
            var fileBytes = await _fileStorageService.GetFileAsync(document.StoragePath);
            var tempPath = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempPath, fileBytes);

            return (tempPath, document.ContentType, document.OriginalFileName);
        }

        public async Task<bool> ValidateFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Define allowed extensions
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".gif", ".txt" };
            
            if (!await _fileStorageService.ValidateFileTypeAsync(file, allowedExtensions))
                return false;

            if (!await _fileStorageService.ValidateFileSizeAsync(file, 50 * 1024 * 1024)) // 50MB
                return false;

            return true;
        }

        public async Task<long> GetTotalStorageUsedAsync(int? clientId = null)
        {
            if (clientId.HasValue)
            {
                var clientDocuments = await _context.Documents
                    .Where(d => d.ClientId == clientId.Value && !d.IsDeleted)
                    .ToListAsync();

                return clientDocuments.Sum(d => d.Size);
            }

            return await _fileStorageService.GetTotalStorageUsedAsync();
        }
    }
}
