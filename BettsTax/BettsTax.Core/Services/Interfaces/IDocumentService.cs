using BettsTax.Core.DTOs.Document;
using Microsoft.AspNetCore.Http;

namespace BettsTax.Core.Services.Interfaces;

/// <summary>
/// Document service interface
/// </summary>
public interface IDocumentService
{
    Task<List<DocumentDto>> GetDocumentsAsync(string? searchTerm = null, string? type = null, string? year = null, int? clientId = null);
    Task<DocumentDto?> GetDocumentByIdAsync(int id);
    Task<DocumentDto> UploadDocumentAsync(IFormFile file, UploadDocumentDto dto, string uploadedBy);
    Task<byte[]?> DownloadDocumentAsync(int id);
}
