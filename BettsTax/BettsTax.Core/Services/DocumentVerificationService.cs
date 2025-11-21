using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BettsTax.Core.Services
{
    public class DocumentVerificationService : IDocumentVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserContextService _userContext;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityTimelineService _activityService;
        private readonly ILogger<DocumentVerificationService> _logger;

        public DocumentVerificationService(
            ApplicationDbContext context,
            IUserContextService userContext,
            IAuditService auditService,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IActivityTimelineService activityService,
            ILogger<DocumentVerificationService> logger)
        {
            _context = context;
            _userContext = userContext;
            _auditService = auditService;
            _notificationService = notificationService;
            _userManager = userManager;
            _activityService = activityService;
            _logger = logger;
        }

        public async Task<Result<DocumentVerificationDto>> GetDocumentVerificationAsync(int documentId)
        {
            try
            {
                var verification = await _context.DocumentVerifications
                    .Include(dv => dv.Document)
                        .ThenInclude(d => d!.Client)
                    .Include(dv => dv.Document)
                        .ThenInclude(d => d!.UploadedBy)
                    .Include(dv => dv.ReviewedBy)
                    .Include(dv => dv.StatusChangedBy)
                    .FirstOrDefaultAsync(dv => dv.DocumentId == documentId);

                if (verification == null)
                {
                    return Result.Failure<DocumentVerificationDto>("Document verification not found");
                }

                var dto = MapToDto(verification);
                return Result.Success<DocumentVerificationDto>(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document verification for document {DocumentId}", documentId);
                return Result.Failure<DocumentVerificationDto>("Error retrieving document verification");
            }
        }

        public async Task<Result<DocumentVerificationDto>> CreateDocumentVerificationAsync(DocumentVerificationCreateDto dto)
        {
            try
            {
                var document = await _context.Documents.FindAsync(dto.DocumentId);
                if (document == null)
                {
                    return Result.Failure<DocumentVerificationDto>("Document not found");
                }

                var existingVerification = await _context.DocumentVerifications
                    .FirstOrDefaultAsync(dv => dv.DocumentId == dto.DocumentId);

                if (existingVerification != null)
                {
                    return Result.Failure<DocumentVerificationDto>("Document verification already exists");
                }

                var verification = new DocumentVerification
                {
                    DocumentId = dto.DocumentId,
                    Status = DocumentVerificationStatus.Submitted,
                    IsRequiredDocument = dto.IsRequiredDocument,
                    DocumentRequirementType = dto.DocumentRequirementType,
                    TaxFilingId = dto.TaxFilingId,
                    StatusChangedById = _userContext.GetCurrentUserId()
                };

                _context.DocumentVerifications.Add(verification);
                await _context.SaveChangesAsync();

                // Create history entry
                await CreateHistoryEntryAsync(dto.DocumentId, DocumentVerificationStatus.NotRequested, 
                    DocumentVerificationStatus.Submitted, "Document uploaded for verification");

                // Audit log
                await _auditService.LogAsync(
                    "DocumentVerification", 
                    "Create", 
                    $"Created verification for document {document.OriginalFileName}", 
                    verification.DocumentVerificationId.ToString());

                return await GetDocumentVerificationAsync(dto.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document verification");
                return Result.Failure<DocumentVerificationDto>("Error creating document verification");
            }
        }

        public async Task<Result<DocumentVerificationDto>> UpdateDocumentVerificationAsync(int documentId, DocumentVerificationUpdateDto dto)
        {
            try
            {
                var verification = await _context.DocumentVerifications
                    .Include(dv => dv.Document)
                    .FirstOrDefaultAsync(dv => dv.DocumentId == documentId);

                if (verification == null)
                {
                    return Result.Failure<DocumentVerificationDto>("Document verification not found");
                }

                var oldStatus = verification.Status;
                
                // Phase 3: Validate status transition
                try
                {
                    ValidateStatusTransition(oldStatus, dto.Status, documentId);
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<DocumentVerificationDto>(ex.Message);
                }
                
                verification.Status = dto.Status;
                verification.ReviewNotes = dto.ReviewNotes;
                verification.RejectionReason = dto.RejectionReason;
                verification.StatusChangedDate = DateTime.UtcNow;
                verification.StatusChangedById = _userContext.GetCurrentUserId();

                if (dto.Status == DocumentVerificationStatus.Verified || 
                    dto.Status == DocumentVerificationStatus.Rejected)
                {
                    verification.ReviewedById = _userContext.GetCurrentUserId();
                    verification.ReviewedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Create history entry
                await CreateHistoryEntryAsync(documentId, oldStatus, dto.Status, dto.ReviewNotes);

                // Send notification if rejected
                if (dto.Status == DocumentVerificationStatus.Rejected && verification.Document != null)
                {
                    await _notificationService.CreateAsync(
                        verification.Document.UploadedById ?? string.Empty,
                        $"Your document '{verification.Document.OriginalFileName}' has been rejected. Reason: {dto.RejectionReason}"
                    );
                }

                // Log activity
                if (verification.Document != null)
                {
                    if (dto.Status == DocumentVerificationStatus.Verified)
                    {
                        await _activityService.LogDocumentActivityAsync(documentId, ActivityType.DocumentVerified);
                    }
                    else if (dto.Status == DocumentVerificationStatus.Rejected)
                    {
                        await _activityService.LogDocumentActivityAsync(documentId, ActivityType.DocumentRejected, dto.RejectionReason);
                    }
                }

                // Audit log
                await _auditService.LogAsync(
                    "DocumentVerification", 
                    "Update", 
                    $"Updated verification status to {dto.Status}", 
                    verification.DocumentVerificationId.ToString());

                return await GetDocumentVerificationAsync(documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document verification");
                return Result.Failure<DocumentVerificationDto>("Error updating document verification");
            }
        }

        public async Task<Result> ReviewDocumentAsync(DocumentReviewRequestDto dto)
        {
            var updateDto = new DocumentVerificationUpdateDto
            {
                Status = dto.Approved ? DocumentVerificationStatus.Verified : DocumentVerificationStatus.Rejected,
                ReviewNotes = dto.ReviewNotes,
                RejectionReason = dto.RejectionReason
            };

            var result = await UpdateDocumentVerificationAsync(dto.DocumentId, updateDto);
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
        }

        public async Task<Result> BulkReviewDocumentsAsync(BulkDocumentReviewDto dto)
        {
            try
            {
                var verifications = await _context.DocumentVerifications
                    .Where(dv => dto.DocumentIds.Contains(dv.DocumentId))
                    .ToListAsync();

                if (verifications.Count != dto.DocumentIds.Count)
                {
                    return Result.Failure("One or more documents not found");
                }

                var newStatus = dto.Approved ? DocumentVerificationStatus.Verified : DocumentVerificationStatus.Rejected;
                var userId = _userContext.GetCurrentUserId();
                var now = DateTime.UtcNow;

                // Phase 3: Validate all transitions before applying
                var invalidTransitions = new List<string>();
                foreach (var verification in verifications)
                {
                    if (!IsValidStatusTransition(verification.Status, newStatus))
                    {
                        invalidTransitions.Add($"Document {verification.DocumentId}: {verification.Status} -> {newStatus}");
                    }
                }

                if (invalidTransitions.Any())
                {
                    return Result.Failure($"Invalid status transitions detected: {string.Join("; ", invalidTransitions)}");
                }

                foreach (var verification in verifications)
                {
                    var oldStatus = verification.Status;
                    verification.Status = newStatus;
                    verification.ReviewNotes = dto.ReviewNotes;
                    verification.ReviewedById = userId;
                    verification.ReviewedDate = now;
                    verification.StatusChangedDate = now;
                    verification.StatusChangedById = userId;

                    // Create history entry
                    await CreateHistoryEntryAsync(verification.DocumentId, oldStatus, newStatus, dto.ReviewNotes);
                }

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "DocumentVerification", 
                    "BulkReview", 
                    $"Bulk reviewed {verifications.Count} documents as {newStatus}", 
                    string.Join(",", dto.DocumentIds));

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk reviewing documents");
                return Result.Failure("Error processing bulk review");
            }
        }

        public async Task<Result<List<DocumentRequirementDto>>> GetDocumentRequirementsAsync(TaxType? taxType = null, TaxpayerCategory? category = null)
        {
            try
            {
                var query = _context.DocumentRequirements
                    .Where(dr => dr.IsActive);

                if (taxType.HasValue)
                {
                    query = query.Where(dr => dr.ApplicableTaxType == null || dr.ApplicableTaxType == taxType);
                }

                if (category.HasValue)
                {
                    query = query.Where(dr => dr.ApplicableTaxpayerCategory == null || dr.ApplicableTaxpayerCategory == category);
                }

                var requirements = await query
                    .OrderBy(dr => dr.DisplayOrder)
                    .ThenBy(dr => dr.Name)
                    .ToListAsync();

                var dtos = requirements.Select(MapToDto).ToList();
                return Result.Success<List<DocumentRequirementDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document requirements");
                return Result.Failure<List<DocumentRequirementDto>>("Error retrieving document requirements");
            }
        }

        public async Task<Result<DocumentRequirementDto>> GetDocumentRequirementAsync(int requirementId)
        {
            try
            {
                var requirement = await _context.DocumentRequirements.FindAsync(requirementId);
                if (requirement == null)
                {
                    return Result.Failure<DocumentRequirementDto>("Document requirement not found");
                }

                return Result.Success<DocumentRequirementDto>(MapToDto(requirement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document requirement {RequirementId}", requirementId);
                return Result.Failure<DocumentRequirementDto>("Error retrieving document requirement");
            }
        }

        public async Task<Result<DocumentRequirementDto>> CreateDocumentRequirementAsync(DocumentRequirementDto dto)
        {
            try
            {
                var existing = await _context.DocumentRequirements
                    .FirstOrDefaultAsync(dr => dr.RequirementCode == dto.RequirementCode);

                if (existing != null)
                {
                    return Result.Failure<DocumentRequirementDto>("Requirement code already exists");
                }

                var requirement = new DocumentRequirement
                {
                    RequirementCode = dto.RequirementCode,
                    Name = dto.Name,
                    Description = dto.Description,
                    ApplicableTaxType = dto.ApplicableTaxType,
                    ApplicableTaxpayerCategory = dto.ApplicableTaxpayerCategory,
                    IsRequired = dto.IsRequired,
                    DisplayOrder = dto.DisplayOrder,
                    AcceptedFormats = dto.AcceptedFormats,
                    MaxFileSizeInBytes = dto.MaxFileSizeInBytes,
                    MinimumQuantity = dto.MinimumQuantity,
                    MaximumQuantity = dto.MaximumQuantity,
                    RequiredMonthsOfData = dto.RequiredMonthsOfData
                };

                _context.DocumentRequirements.Add(requirement);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "DocumentRequirement", 
                    "Create", 
                    $"Created requirement: {requirement.Name}", 
                    requirement.DocumentRequirementId.ToString());

                return Result.Success<DocumentRequirementDto>(MapToDto(requirement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document requirement");
                return Result.Failure<DocumentRequirementDto>("Error creating document requirement");
            }
        }

        public async Task<Result<DocumentRequirementDto>> UpdateDocumentRequirementAsync(int requirementId, DocumentRequirementDto dto)
        {
            try
            {
                var requirement = await _context.DocumentRequirements.FindAsync(requirementId);
                if (requirement == null)
                {
                    return Result.Failure<DocumentRequirementDto>("Document requirement not found");
                }

                requirement.Name = dto.Name;
                requirement.Description = dto.Description;
                requirement.ApplicableTaxType = dto.ApplicableTaxType;
                requirement.ApplicableTaxpayerCategory = dto.ApplicableTaxpayerCategory;
                requirement.IsRequired = dto.IsRequired;
                requirement.DisplayOrder = dto.DisplayOrder;
                requirement.AcceptedFormats = dto.AcceptedFormats;
                requirement.MaxFileSizeInBytes = dto.MaxFileSizeInBytes;
                requirement.MinimumQuantity = dto.MinimumQuantity;
                requirement.MaximumQuantity = dto.MaximumQuantity;
                requirement.RequiredMonthsOfData = dto.RequiredMonthsOfData;
                requirement.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "DocumentRequirement", 
                    "Update", 
                    $"Updated requirement: {requirement.Name}", 
                    requirement.DocumentRequirementId.ToString());

                return Result.Success<DocumentRequirementDto>(MapToDto(requirement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document requirement");
                return Result.Failure<DocumentRequirementDto>("Error updating document requirement");
            }
        }

        public async Task<Result> DeleteDocumentRequirementAsync(int requirementId)
        {
            try
            {
                var requirement = await _context.DocumentRequirements.FindAsync(requirementId);
                if (requirement == null)
                {
                    return Result.Failure("Document requirement not found");
                }

                requirement.IsActive = false;
                requirement.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "DocumentRequirement", 
                    "Delete", 
                    $"Deactivated requirement: {requirement.Name}", 
                    requirement.DocumentRequirementId.ToString());

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document requirement");
                return Result.Failure("Error deleting document requirement");
            }
        }

        public async Task<Result<List<ClientDocumentRequirementDto>>> GetClientDocumentRequirementsAsync(int clientId, int taxFilingId)
        {
            try
            {
                var clientRequirements = await _context.ClientDocumentRequirements
                    .Include(cdr => cdr.DocumentRequirement)
                    .Include(cdr => cdr.RequestedBy)
                    .Where(cdr => cdr.ClientId == clientId && cdr.TaxFilingId == taxFilingId)
                    .ToListAsync();

                var dtos = new List<ClientDocumentRequirementDto>();

                foreach (var req in clientRequirements)
                {
                    var dto = MapToDto(req);
                    
                    // Get submitted documents for this requirement
                    if (!string.IsNullOrEmpty(req.DocumentIds))
                    {
                        var docIds = req.DocumentIds.Split(',')
                            .Where(id => int.TryParse(id, out _))
                            .Select(int.Parse)
                            .ToList();

                        var documents = await _context.DocumentVerifications
                            .Include(dv => dv.Document)
                                .ThenInclude(d => d!.UploadedBy)
                            .Where(dv => docIds.Contains(dv.DocumentId))
                            .ToListAsync();

                        dto.SubmittedDocuments = documents.Select(MapToDto).ToList();
                    }

                    dtos.Add(dto);
                }

                return Result.Success<List<ClientDocumentRequirementDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client document requirements");
                return Result.Failure<List<ClientDocumentRequirementDto>>("Error retrieving client document requirements");
            }
        }

        public async Task<Result> GenerateClientDocumentRequirementsAsync(int clientId, int taxFilingId)
        {
            try
            {
                var taxFiling = await _context.TaxFilings
                    .Include(tf => tf.Client)
                    .FirstOrDefaultAsync(tf => tf.TaxFilingId == taxFilingId && tf.ClientId == clientId);

                if (taxFiling == null)
                {
                    return Result.Failure("Tax filing not found");
                }

                // Get applicable requirements
                var requirements = await _context.DocumentRequirements
                    .Where(dr => dr.IsActive && 
                                (dr.ApplicableTaxType == null || dr.ApplicableTaxType == taxFiling.TaxType) &&
                                (dr.ApplicableTaxpayerCategory == null || dr.ApplicableTaxpayerCategory == taxFiling.Client!.TaxpayerCategory))
                    .ToListAsync();

                // Create client requirements that don't already exist
                foreach (var requirement in requirements)
                {
                    var existing = await _context.ClientDocumentRequirements
                        .FirstOrDefaultAsync(cdr => cdr.ClientId == clientId && 
                                                  cdr.TaxFilingId == taxFilingId && 
                                                  cdr.DocumentRequirementId == requirement.DocumentRequirementId);

                    if (existing == null)
                    {
                        var clientReq = new ClientDocumentRequirement
                        {
                            ClientId = clientId,
                            TaxFilingId = taxFilingId,
                            DocumentRequirementId = requirement.DocumentRequirementId,
                            Status = DocumentVerificationStatus.NotRequested
                        };

                        _context.ClientDocumentRequirements.Add(clientReq);
                    }
                }

                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAsync(
                    "ClientDocumentRequirement", 
                    "Generate", 
                    $"Generated document requirements for tax filing {taxFilingId}", 
                    taxFilingId.ToString());

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating client document requirements");
                return Result.Failure("Error generating document requirements");
            }
        }

        public async Task<Result> RequestDocumentsFromClientAsync(int clientId, int taxFilingId, List<int> requirementIds)
        {
            try
            {
                var clientRequirements = await _context.ClientDocumentRequirements
                    .Include(cdr => cdr.DocumentRequirement)
                    .Where(cdr => cdr.ClientId == clientId && 
                                 cdr.TaxFilingId == taxFilingId && 
                                 requirementIds.Contains(cdr.DocumentRequirementId))
                    .ToListAsync();

                if (clientRequirements.Count != requirementIds.Count)
                {
                    return Result.Failure("One or more requirements not found");
                }

                var userId = _userContext.GetCurrentUserId();
                var now = DateTime.UtcNow;

                foreach (var req in clientRequirements)
                {
                    req.Status = DocumentVerificationStatus.Requested;
                    req.RequestedDate = now;
                    req.RequestedById = userId;
                }

                await _context.SaveChangesAsync();

                // Send notification to client
                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client?.User != null)
                {
                    var requirementNames = clientRequirements
                        .Select(r => r.DocumentRequirement?.Name ?? "Document")
                        .ToList();

                    await _notificationService.CreateAsync(
                        client.UserId,
                        $"Documents Requested: The following documents have been requested for your tax filing: {string.Join(", ", requirementNames)}"
                    );
                }

                // Audit log
                await _auditService.LogAsync(
                    "ClientDocumentRequirement", 
                    "Request", 
                    $"Requested {requirementIds.Count} documents from client", 
                    taxFilingId.ToString());

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting documents from client");
                return Result.Failure("Error requesting documents");
            }
        }

        public async Task<Result<DocumentVerificationSummaryDto>> GetDocumentVerificationSummaryAsync(int clientId, int taxFilingId)
        {
            try
            {
                var requirements = await GetClientDocumentRequirementsAsync(clientId, taxFilingId);
                if (!requirements.IsSuccess)
                {
                    return Result.Failure<DocumentVerificationSummaryDto>(requirements.ErrorMessage);
                }

                var summary = new DocumentVerificationSummaryDto
                {
                    Requirements = requirements.Value
                };

                foreach (var req in requirements.Value)
                {
                    if (req.DocumentRequirement?.IsRequired == true)
                    {
                        summary.TotalDocuments += req.DocumentRequirement.MinimumQuantity;
                        
                        switch (req.Status)
                        {
                            case DocumentVerificationStatus.Verified:
                            case DocumentVerificationStatus.Filed:
                                summary.VerifiedDocuments += req.DocumentCount;
                                break;
                            case DocumentVerificationStatus.Submitted:
                            case DocumentVerificationStatus.UnderReview:
                                summary.PendingReview += req.DocumentCount;
                                break;
                            case DocumentVerificationStatus.Rejected:
                                summary.RejectedDocuments += req.DocumentCount;
                                break;
                            case DocumentVerificationStatus.NotRequested:
                            case DocumentVerificationStatus.Requested:
                                summary.MissingDocuments += req.DocumentRequirement.MinimumQuantity;
                                break;
                        }
                    }
                }

                if (summary.TotalDocuments > 0)
                {
                    summary.CompletionPercentage = (decimal)summary.VerifiedDocuments / summary.TotalDocuments * 100;
                }

                return Result.Success<DocumentVerificationSummaryDto>(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document verification summary");
                return Result.Failure<DocumentVerificationSummaryDto>("Error retrieving verification summary");
            }
        }

        public async Task<Result<List<DocumentVerificationHistoryDto>>> GetDocumentVerificationHistoryAsync(int documentId)
        {
            try
            {
                var history = await _context.DocumentVerificationHistories
                    .Include(dvh => dvh.ChangedBy)
                    .Where(dvh => dvh.DocumentId == documentId)
                    .OrderByDescending(dvh => dvh.ChangedDate)
                    .ToListAsync();

                var dtos = history.Select(h => new DocumentVerificationHistoryDto
                {
                    DocumentVerificationHistoryId = h.DocumentVerificationHistoryId,
                    DocumentId = h.DocumentId,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangedById = h.ChangedById,
                    ChangedByName = h.ChangedBy != null ? $"{h.ChangedBy.FirstName} {h.ChangedBy.LastName}" : "System",
                    ChangedDate = h.ChangedDate,
                    Notes = h.Notes
                }).ToList();

                return Result.Success<List<DocumentVerificationHistoryDto>>(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document verification history");
                return Result.Failure<List<DocumentVerificationHistoryDto>>("Error retrieving verification history");
            }
        }

        public async Task<Result<PagedResult<DocumentVerificationDto>>> GetPendingDocumentReviewsAsync(string? associateId = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.DocumentVerifications
                    .Include(dv => dv.Document)
                        .ThenInclude(d => d!.Client)
                    .Include(dv => dv.Document)
                        .ThenInclude(d => d!.UploadedBy)
                    .Where(dv => dv.Status == DocumentVerificationStatus.Submitted || 
                                dv.Status == DocumentVerificationStatus.UnderReview);

                if (!string.IsNullOrEmpty(associateId))
                {
                    query = query.Where(dv => dv.Document!.Client!.AssignedAssociateId == associateId);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderBy(dv => dv.StatusChangedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = items.Select(MapToDto).ToList();

                return Result.Success<PagedResult<DocumentVerificationDto>>(new PagedResult<DocumentVerificationDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending document reviews");
                return Result.Failure<PagedResult<DocumentVerificationDto>>("Error retrieving pending reviews");
            }
        }

        public async Task<Result<PagedResult<DocumentVerificationDto>>> GetDocumentsByStatusAsync(DocumentVerificationStatus status, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.DocumentVerifications
                    .Include(dv => dv.Document)
                        .ThenInclude(d => d!.Client)
                    .Include(dv => dv.Document)
                        .ThenInclude(d => d!.UploadedBy)
                    .Where(dv => dv.Status == status);

                var totalCount = await query.CountAsync();
                var items = await query
                    .OrderByDescending(dv => dv.StatusChangedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = items.Select(MapToDto).ToList();

                return Result.Success<PagedResult<DocumentVerificationDto>>(new PagedResult<DocumentVerificationDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents by status");
                return Result.Failure<PagedResult<DocumentVerificationDto>>("Error retrieving documents");
            }
        }

        public async Task<Result> ValidateDocumentForRequirementAsync(int documentId, int requirementId)
        {
            try
            {
                var document = await _context.Documents.FindAsync(documentId);
                var requirement = await _context.DocumentRequirements.FindAsync(requirementId);

                if (document == null || requirement == null)
                {
                    return Result.Failure("Document or requirement not found");
                }

                // Validate file format
                var acceptedFormats = requirement.AcceptedFormats.Split(',').Select(f => f.Trim().ToLower()).ToList();
                var fileExtension = Path.GetExtension(document.OriginalFileName).TrimStart('.').ToLower();

                if (!acceptedFormats.Contains(fileExtension))
                {
                    return Result.Failure($"File format not accepted. Accepted formats: {requirement.AcceptedFormats}");
                }

                // Validate file size
                if (document.Size > requirement.MaxFileSizeInBytes)
                {
                    var maxSizeMB = requirement.MaxFileSizeInBytes / (1024.0 * 1024.0);
                    return Result.Failure($"File size exceeds maximum allowed size of {maxSizeMB:F2} MB");
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating document for requirement");
                return Result.Failure("Error validating document");
            }
        }

        public async Task<Result<bool>> CheckAllRequiredDocumentsVerifiedAsync(int clientId, int taxFilingId)
        {
            try
            {
                var requirements = await _context.ClientDocumentRequirements
                    .Include(cdr => cdr.DocumentRequirement)
                    .Where(cdr => cdr.ClientId == clientId && 
                                 cdr.TaxFilingId == taxFilingId && 
                                 cdr.DocumentRequirement!.IsRequired)
                    .ToListAsync();

                var allVerified = requirements.All(r => 
                    r.Status == DocumentVerificationStatus.Verified || 
                    r.Status == DocumentVerificationStatus.Filed);

                return Result.Success<bool>(allVerified);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking document verification status");
                return Result.Failure<bool>("Error checking verification status");
            }
        }

        private async Task CreateHistoryEntryAsync(int documentId, DocumentVerificationStatus oldStatus, 
            DocumentVerificationStatus newStatus, string? notes = null)
        {
            var history = new DocumentVerificationHistory
            {
                DocumentId = documentId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedById = _userContext.GetCurrentUserId(),
                Notes = notes
            };

            _context.DocumentVerificationHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        private DocumentVerificationDto MapToDto(DocumentVerification verification)
        {
            return new DocumentVerificationDto
            {
                DocumentVerificationId = verification.DocumentVerificationId,
                DocumentId = verification.DocumentId,
                Status = verification.Status,
                ReviewedById = verification.ReviewedById,
                ReviewedByName = verification.ReviewedBy != null ? 
                    $"{verification.ReviewedBy.FirstName} {verification.ReviewedBy.LastName}" : null,
                ReviewedDate = verification.ReviewedDate,
                ReviewNotes = verification.ReviewNotes,
                RejectionReason = verification.RejectionReason,
                StatusChangedDate = verification.StatusChangedDate,
                StatusChangedById = verification.StatusChangedById,
                StatusChangedByName = verification.StatusChangedBy != null ? 
                    $"{verification.StatusChangedBy.FirstName} {verification.StatusChangedBy.LastName}" : null,
                IsRequiredDocument = verification.IsRequiredDocument,
                DocumentRequirementType = verification.DocumentRequirementType,
                TaxFilingId = verification.TaxFilingId,
                
                // Document details
                OriginalFileName = verification.Document?.OriginalFileName ?? string.Empty,
                ContentType = verification.Document?.ContentType ?? string.Empty,
                Size = verification.Document?.Size ?? 0,
                Category = verification.Document?.Category ?? DocumentCategory.Other,
                Description = verification.Document?.Description ?? string.Empty,
                UploadedAt = verification.Document?.UploadedAt ?? DateTime.UtcNow,
                UploadedByName = verification.Document?.UploadedBy != null ? 
                    $"{verification.Document.UploadedBy.FirstName} {verification.Document.UploadedBy.LastName}" : null,
                
                // Client details
                ClientId = verification.Document?.ClientId ?? 0,
                ClientName = verification.Document?.Client?.BusinessName ?? string.Empty,
                ClientNumber = verification.Document?.Client?.ClientNumber ?? string.Empty
            };
        }

        private DocumentRequirementDto MapToDto(DocumentRequirement requirement)
        {
            return new DocumentRequirementDto
            {
                DocumentRequirementId = requirement.DocumentRequirementId,
                RequirementCode = requirement.RequirementCode,
                Name = requirement.Name,
                Description = requirement.Description,
                ApplicableTaxType = requirement.ApplicableTaxType,
                ApplicableTaxpayerCategory = requirement.ApplicableTaxpayerCategory,
                IsRequired = requirement.IsRequired,
                DisplayOrder = requirement.DisplayOrder,
                AcceptedFormats = requirement.AcceptedFormats,
                MaxFileSizeInBytes = requirement.MaxFileSizeInBytes,
                MinimumQuantity = requirement.MinimumQuantity,
                MaximumQuantity = requirement.MaximumQuantity,
                RequiredMonthsOfData = requirement.RequiredMonthsOfData
            };
        }

        private ClientDocumentRequirementDto MapToDto(ClientDocumentRequirement requirement)
        {
            return new ClientDocumentRequirementDto
            {
                ClientDocumentRequirementId = requirement.ClientDocumentRequirementId,
                ClientId = requirement.ClientId,
                TaxFilingId = requirement.TaxFilingId,
                DocumentRequirementId = requirement.DocumentRequirementId,
                Status = requirement.Status,
                RequestedDate = requirement.RequestedDate,
                RequestedById = requirement.RequestedById,
                RequestedByName = requirement.RequestedBy != null ? 
                    $"{requirement.RequestedBy.FirstName} {requirement.RequestedBy.LastName}" : null,
                FulfilledDate = requirement.FulfilledDate,
                DocumentCount = requirement.DocumentCount,
                DocumentRequirement = requirement.DocumentRequirement != null ? 
                    MapToDto(requirement.DocumentRequirement) : null
            };
        }

        #region Status Transition Validation - Phase 3 (fixes_plan.md §2.5)

        /// <summary>
        /// Validates if a status transition is allowed
        /// Phase 3: Document Status Transitions
        /// </summary>
        private bool IsValidStatusTransition(DocumentVerificationStatus currentStatus, DocumentVerificationStatus newStatus)
        {
            // Define valid transitions
            var validTransitions = new Dictionary<DocumentVerificationStatus, List<DocumentVerificationStatus>>
            {
                [DocumentVerificationStatus.NotRequested] = new List<DocumentVerificationStatus> 
                { 
                    DocumentVerificationStatus.Requested 
                },
                [DocumentVerificationStatus.Requested] = new List<DocumentVerificationStatus> 
                { 
                    DocumentVerificationStatus.Submitted,
                    DocumentVerificationStatus.NotRequested // Allow cancellation
                },
                [DocumentVerificationStatus.Submitted] = new List<DocumentVerificationStatus> 
                { 
                    DocumentVerificationStatus.UnderReview,
                    DocumentVerificationStatus.Rejected // Can reject without review
                },
                [DocumentVerificationStatus.UnderReview] = new List<DocumentVerificationStatus> 
                { 
                    DocumentVerificationStatus.Verified,
                    DocumentVerificationStatus.Rejected,
                    DocumentVerificationStatus.Submitted // Return for corrections
                },
                [DocumentVerificationStatus.Rejected] = new List<DocumentVerificationStatus> 
                { 
                    DocumentVerificationStatus.Requested, // Request resubmission
                    DocumentVerificationStatus.Submitted  // Direct resubmission
                },
                [DocumentVerificationStatus.Verified] = new List<DocumentVerificationStatus> 
                { 
                    DocumentVerificationStatus.Filed,
                    DocumentVerificationStatus.UnderReview // Allow re-review if needed
                },
                [DocumentVerificationStatus.Filed] = new List<DocumentVerificationStatus>() // Terminal state
            };

            // Same status is always valid (no-op)
            if (currentStatus == newStatus)
                return true;

            // Check if transition is in valid list
            return validTransitions.ContainsKey(currentStatus) && 
                   validTransitions[currentStatus].Contains(newStatus);
        }

        /// <summary>
        /// Validates and enforces status transition rules
        /// Throws InvalidOperationException if transition is invalid
        /// </summary>
        private void ValidateStatusTransition(DocumentVerificationStatus currentStatus, DocumentVerificationStatus newStatus, int documentId)
        {
            if (!IsValidStatusTransition(currentStatus, newStatus))
            {
                var errorMessage = $"Invalid status transition for document {documentId}: " +
                                 $"Cannot change from {currentStatus} to {newStatus}. " +
                                 $"Valid transitions from {currentStatus} are: {GetValidTransitionsText(currentStatus)}";
                
                _logger.LogWarning(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation(
                "Valid status transition for document {DocumentId}: {OldStatus} -> {NewStatus}",
                documentId, currentStatus, newStatus);
        }

        /// <summary>
        /// Gets human-readable text of valid transitions for error messages
        /// </summary>
        private string GetValidTransitionsText(DocumentVerificationStatus currentStatus)
        {
            var validTransitions = new Dictionary<DocumentVerificationStatus, List<DocumentVerificationStatus>>
            {
                [DocumentVerificationStatus.NotRequested] = new List<DocumentVerificationStatus> { DocumentVerificationStatus.Requested },
                [DocumentVerificationStatus.Requested] = new List<DocumentVerificationStatus> { DocumentVerificationStatus.Submitted, DocumentVerificationStatus.NotRequested },
                [DocumentVerificationStatus.Submitted] = new List<DocumentVerificationStatus> { DocumentVerificationStatus.UnderReview, DocumentVerificationStatus.Rejected },
                [DocumentVerificationStatus.UnderReview] = new List<DocumentVerificationStatus> { DocumentVerificationStatus.Verified, DocumentVerificationStatus.Rejected, DocumentVerificationStatus.Submitted },
                [DocumentVerificationStatus.Rejected] = new List<DocumentVerificationStatus> { DocumentVerificationStatus.Requested, DocumentVerificationStatus.Submitted },
                [DocumentVerificationStatus.Verified] = new List<DocumentVerificationStatus> { DocumentVerificationStatus.Filed, DocumentVerificationStatus.UnderReview },
                [DocumentVerificationStatus.Filed] = new List<DocumentVerificationStatus>()
            };

            if (!validTransitions.ContainsKey(currentStatus))
                return "None (unknown status)";

            var transitions = validTransitions[currentStatus];
            return transitions.Any() 
                ? string.Join(", ", transitions) 
                : "None (terminal state)";
        }

        #endregion
    }
}