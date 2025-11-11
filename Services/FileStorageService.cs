using CloudStorage.Models;
using System.Security.Cryptography;
using System.Text;

namespace CloudStorage.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string userId);
        Task<byte[]> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        string GetMimeType(string fileName);
        string CalculateFileHash(Stream fileStream);
        string GenerateUniqueFileName(string originalFileName, string userId);
        long GetDirectorySize(string directoryPath);
        Task<string> GetFileContentAsync(string filePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _uploadsPath;

        public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _logger = logger;
            _uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string userId)
        {
            try
            {
                var userDirectory = Path.Combine(_uploadsPath, userId);
                if (!Directory.Exists(userDirectory))
                {
                    Directory.CreateDirectory(userDirectory);
                }

                var uniqueFileName = GenerateUniqueFileName(file.FileName, userId);
                var filePath = Path.Combine(userDirectory, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                return Path.Combine(userId, uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName} for user {UserId}", file.FileName, userId);
                throw;
            }
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadsPath, filePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadsPath, filePath);
                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
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
            var fullPath = Path.Combine(_uploadsPath, filePath);
            return await Task.FromResult(File.Exists(fullPath));
        }

        public string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".tar" => "application/x-tar",
                ".gz" => "application/gzip",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                _ => "application/octet-stream"
            };
        }

        public string CalculateFileHash(Stream fileStream)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(fileStream);
            fileStream.Position = 0; // Reset stream position
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public string GenerateUniqueFileName(string originalFileName, string userId)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var randomSuffix = Guid.NewGuid().ToString("N")[..8];
            
            return $"{fileNameWithoutExtension}_{timestamp}_{randomSuffix}{extension}";
        }

        public long GetDirectorySize(string directoryPath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadsPath, directoryPath);
                if (!Directory.Exists(fullPath))
                    return 0;

                return new DirectoryInfo(fullPath)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating directory size for {DirectoryPath}", directoryPath);
                return 0;
            }
        }

        public async Task<string> GetFileContentAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadsPath, filePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                return await File.ReadAllTextAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file content {FilePath}", filePath);
                throw;
            }
        }
    }
}