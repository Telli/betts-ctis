using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BettsTax.Core.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _storageBasePath;
        private readonly string[] _allowedExtensions;
        private readonly long _maxFileSize;

        public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
        {
            _logger = logger;
            _storageBasePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>() ?? 
                new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".gif", ".txt" };
            _maxFileSize = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 50 * 1024 * 1024); // 50MB default

            // Ensure storage directory exists
            Directory.CreateDirectory(_storageBasePath);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string fileName, string subfolder = "")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty");

            // Validate file
            if (!await ValidateFileTypeAsync(file, _allowedExtensions))
                throw new InvalidOperationException("File type not allowed");

            if (!await ValidateFileSizeAsync(file, _maxFileSize))
                throw new InvalidOperationException("File size exceeds maximum allowed size");

            // Create subfolder path
            var targetDirectory = string.IsNullOrEmpty(subfolder) 
                ? _storageBasePath 
                : Path.Combine(_storageBasePath, subfolder);

            Directory.CreateDirectory(targetDirectory);

            // Generate secure file name
            var secureFileName = GenerateSecureFileName(fileName);
            var filePath = Path.Combine(targetDirectory, secureFileName);

            // Save file (ensure stream is closed before scanning)
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            // Scan for virus (placeholder implementation) - after stream is disposed
            await ScanFileForVirusAsync(filePath);

            _logger.LogInformation("Saved file {FileName} to {FilePath}", fileName, filePath);

            // Return relative path from base storage path
            return Path.GetRelativePath(_storageBasePath, filePath).Replace('\\', '/');
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_storageBasePath, filePath);
            
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found");

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_storageBasePath, filePath);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Deleted file {FilePath}", filePath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var fullPath = Path.Combine(_storageBasePath, filePath);
            return File.Exists(fullPath);
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            var fullPath = Path.Combine(_storageBasePath, filePath);
            
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found");

            var fileInfo = new FileInfo(fullPath);
            return fileInfo.Length;
        }

        public async Task<bool> ValidateFileTypeAsync(IFormFile file, string[] allowedExtensions)
        {
            if (file == null || allowedExtensions == null || allowedExtensions.Length == 0)
                return false;

            var extension = GetFileExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Any(ext => ext.ToLowerInvariant() == extension);
        }

        public async Task<bool> ValidateFileSizeAsync(IFormFile file, long maxSizeBytes)
        {
            return file?.Length <= maxSizeBytes;
        }

        public async Task<bool> ScanFileForVirusAsync(string filePath)
        {
            // Placeholder implementation for virus scanning
            // In production, integrate with antivirus solution like ClamAV, Windows Defender, etc.
            
            try
            {
                // Basic file validation checks
                var fileInfo = new FileInfo(filePath);
                
                // Check if file is too large (suspicious)
                if (fileInfo.Length > 100 * 1024 * 1024) // 100MB
                {
                    _logger.LogWarning("File {FilePath} is suspiciously large: {Size} bytes", filePath, fileInfo.Length);
                    return false;
                }

                // Check for suspicious file signatures
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[1024];
                var totalRead = 0;

                while (totalRead < buffer.Length)
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead));
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalRead += bytesRead;
                }

                // Basic checks for known malicious patterns (very basic implementation)
                var content = Encoding.UTF8.GetString(buffer, 0, totalRead);
                if (content.Contains("eval(") || content.Contains("exec(") || content.Contains("<script"))
                {
                    _logger.LogWarning("File {FilePath} contains suspicious content", filePath);
                    return false;
                }

                _logger.LogDebug("File {FilePath} passed virus scan", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning file {FilePath} for viruses", filePath);
                return false;
            }
        }

        public string GenerateSecureFileName(string originalFileName)
        {
            var extension = GetFileExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomBytes = new byte[8];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            
            var randomString = Convert.ToHexString(randomBytes).ToLowerInvariant();
            
            return $"{timestamp}_{randomString}{extension}";
        }

        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName);
        }

        public async Task<long> GetTotalStorageUsedAsync()
        {
            try
            {
                var directoryInfo = new DirectoryInfo(_storageBasePath);
                return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total storage used");
                return 0;
            }
        }
    }
}