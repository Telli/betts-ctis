using BettsTax.Core.DTOs;
using BettsTax.Data;
using BettsTax.Shared;
using Microsoft.AspNetCore.Http;

namespace BettsTax.Core.Services
{
    public interface IDocumentService
    {
        Task<PagedResult<DocumentDto>> GetDocumentsAsync(int page, int pageSize, string? searchTerm = null, DocumentCategory? category = null, int? clientId = null);
        Task<DocumentDto?> GetDocumentByIdAsync(int id);
        Task<IEnumerable<DocumentDto>> GetClientDocumentsAsync(int clientId);
        Task<List<DocumentDto>> GetTaxFilingDocumentsAsync(int taxFilingId);
        Task<DocumentDto> UploadAsync(UploadDocumentDto dto, IFormFile file, string userId);
        Task<DocumentDto> UpdateAsync(int id, UpdateDocumentDto dto, string userId);
        Task<bool> DeleteAsync(int documentId, string userId);
        Task<(string Path, string ContentType, string FileName)?> GetFileInfoAsync(int documentId);
        Task<bool> ValidateFileAsync(IFormFile file);
        Task<long> GetTotalStorageUsedAsync(int? clientId = null);
    }

    public class UploadDocumentDto
    {
        public int ClientId { get; set; }
        public int? TaxYearId { get; set; }
        public int? TaxFilingId { get; set; }
        public DocumentCategory Category { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateDocumentDto
    {
        public DocumentCategory? Category { get; set; }
        public string? Description { get; set; }
    }
}
