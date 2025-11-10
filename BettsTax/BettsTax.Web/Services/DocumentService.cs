using BettsTax.Core.DTOs.Document;
using BettsTax.Core.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;

namespace BettsTax.Web.Services;

/// <summary>
/// Document service implementation
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(ILogger<DocumentService> logger)
    {
        _logger = logger;
    }

    public async Task<List<DocumentDto>> GetDocumentsAsync(
        string? searchTerm = null,
        string? type = null,
        string? year = null,
        int? clientId = null)
    {
        _logger.LogInformation("Retrieving documents with filters: search={Search}, type={Type}, year={Year}, clientId={ClientId}",
            searchTerm, type, year, clientId);

        await Task.CompletedTask;

        // Mock data - replace with actual database query
        var documents = new List<DocumentDto>
        {
            new() { Id = 1, Name = "Tax Return 2024.pdf", Type = "Tax Return", Client = "Sierra Leone Breweries Ltd", Year = 2024, TaxType = "Corporate Tax", Version = 1, UploadedBy = "John Kamara", UploadDate = "2025-01-15", Hash = "a1b2c3d4e5f6", Status = "Approved" },
            new() { Id = 2, Name = "Financial Statements Q4.xlsx", Type = "Financial Statement", Client = "Standard Chartered Bank SL", Year = 2024, TaxType = "Corporate Tax", Version = 2, UploadedBy = "Sarah Conteh", UploadDate = "2025-01-10", Hash = "b2c3d4e5f6g7", Status = "Pending Review" },
            new() { Id = 3, Name = "VAT Report January.pdf", Type = "VAT Report", Client = "Orange Sierra Leone", Year = 2025, TaxType = "VAT", Version = 1, UploadedBy = "Mohamed Sesay", UploadDate = "2025-01-20", Hash = "c3d4e5f6g7h8", Status = "Approved" },
            new() { Id = 4, Name = "Payroll Records December.xlsx", Type = "Payroll", Client = "Rokel Commercial Bank", Year = 2024, TaxType = "PAYE", Version = 1, UploadedBy = "Fatmata Koroma", UploadDate = "2025-01-08", Hash = "d4e5f6g7h8i9", Status = "Approved" }
        };

        // Apply filters
        if (clientId.HasValue)
        {
            // In real implementation, filter by actual client ID
            documents = documents.Take(2).ToList();
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            documents = documents.Where(d =>
                d.Name.ToLower().Contains(searchTerm) ||
                d.Client.ToLower().Contains(searchTerm) ||
                d.Type.ToLower().Contains(searchTerm)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(type) && type != "all")
        {
            documents = documents.Where(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(year) && year != "all" && int.TryParse(year, out var yearNum))
        {
            documents = documents.Where(d => d.Year == yearNum).ToList();
        }

        return documents;
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving document with id {DocumentId}", id);

        await Task.CompletedTask;

        var documents = new List<DocumentDto>
        {
            new() { Id = 1, Name = "Tax Return 2024.pdf", Type = "Tax Return", Client = "Sierra Leone Breweries Ltd", Year = 2024, TaxType = "Corporate Tax", Version = 1, UploadedBy = "John Kamara", UploadDate = "2025-01-15", Hash = "a1b2c3d4e5f6", Status = "Approved" },
            new() { Id = 2, Name = "Financial Statements Q4.xlsx", Type = "Financial Statement", Client = "Standard Chartered Bank SL", Year = 2024, TaxType = "Corporate Tax", Version = 2, UploadedBy = "Sarah Conteh", UploadDate = "2025-01-10", Hash = "b2c3d4e5f6g7", Status = "Pending Review" },
        };

        return documents.FirstOrDefault(d => d.Id == id);
    }

    public async Task<DocumentDto> UploadDocumentAsync(IFormFile file, UploadDocumentDto dto, string uploadedBy)
    {
        _logger.LogInformation("Uploading document: {FileName} for client {ClientId}", file.FileName, dto.ClientId);

        // Calculate file hash
        using var stream = file.OpenReadStream();
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 12).ToLower();

        // Mock implementation - replace with actual file storage and database insert
        var newDocument = new DocumentDto
        {
            Id = new Random().Next(100, 999),
            Name = file.FileName,
            Type = dto.Type,
            Client = $"Client-{dto.ClientId}", // In real impl, fetch client name from database
            Year = dto.Year,
            TaxType = dto.TaxType,
            Version = 1,
            UploadedBy = uploadedBy,
            UploadDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Hash = hash,
            Status = "Pending Review"
        };

        // In production: Save file to blob storage or file system
        // In production: Save document metadata to database

        return newDocument;
    }

    public async Task<byte[]?> DownloadDocumentAsync(int id)
    {
        _logger.LogInformation("Downloading document {DocumentId}", id);

        await Task.CompletedTask;

        // Mock implementation - return sample PDF bytes
        // In production: Retrieve file from blob storage or file system
        var samplePdfContent = Encoding.UTF8.GetBytes("Mock PDF Content for document " + id);

        return samplePdfContent;
    }
}
