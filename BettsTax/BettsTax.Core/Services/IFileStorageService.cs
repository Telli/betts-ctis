using Microsoft.AspNetCore.Http;

namespace BettsTax.Core.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string fileName, string subfolder = "");
        Task<byte[]> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        Task<long> GetFileSizeAsync(string filePath);
        Task<bool> ValidateFileTypeAsync(IFormFile file, string[] allowedExtensions);
        Task<bool> ValidateFileSizeAsync(IFormFile file, long maxSizeBytes);
        Task<bool> ScanFileForVirusAsync(string filePath);
        string GenerateSecureFileName(string originalFileName);
        string GetFileExtension(string fileName);
        Task<long> GetTotalStorageUsedAsync();
    }
}