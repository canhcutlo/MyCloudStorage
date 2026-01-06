using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CloudStorage.Services
{
    public interface IFileVersionService
    {
        Task<FileVersion> CreateVersionAsync(StorageItem item, string userId, string changeDescription = "");
        Task<List<FileVersion>> GetVersionHistoryAsync(int storageItemId);
        Task<FileVersion?> GetVersionAsync(int versionId);
        Task<bool> RestoreVersionAsync(int versionId, string userId);
        Task<bool> DeleteVersionAsync(int versionId);
    }

    public class FileVersionService : IFileVersionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FileVersionService> _logger;

        public FileVersionService(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            IWebHostEnvironment environment,
            ILogger<FileVersionService> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _environment = environment;
            _logger = logger;
        }

        public async Task<FileVersion> CreateVersionAsync(StorageItem item, string userId, string changeDescription = "")
        {
            // Get the next version number
            var lastVersion = await _context.FileVersions
                .Where(v => v.StorageItemId == item.Id && !v.IsDeleted)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync();

            int nextVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;

            // Create version folder if it doesn't exist
            var versionsFolder = Path.Combine(_environment.WebRootPath, "uploads", "versions");
            Directory.CreateDirectory(versionsFolder);

            // Copy the current file to the versions folder
            var sourceFilePath = Path.Combine(_environment.WebRootPath, "uploads", item.FilePath);
            var versionFileName = $"{Path.GetFileNameWithoutExtension(item.FilePath)}_v{nextVersionNumber}{Path.GetExtension(item.FilePath)}";
            var versionFilePath = Path.Combine(versionsFolder, versionFileName);

            if (File.Exists(sourceFilePath))
            {
                File.Copy(sourceFilePath, versionFilePath, true);
            }
            else
            {
                throw new FileNotFoundException("Source file not found", sourceFilePath);
            }

            // Calculate file hash
            var fileHash = await CalculateFileHashAsync(versionFilePath);

            // Create version record
            var version = new FileVersion
            {
                StorageItemId = item.Id,
                VersionNumber = nextVersionNumber,
                FilePath = Path.Combine("versions", versionFileName),
                FileHash = fileHash,
                Size = item.Size,
                MimeType = item.MimeType,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                ChangeDescription = changeDescription ?? "",
                IsDeleted = false
            };

            _context.FileVersions.Add(version);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created version {VersionNumber} for file {ItemId}", nextVersionNumber, item.Id);

            return version;
        }

        public async Task<List<FileVersion>> GetVersionHistoryAsync(int storageItemId)
        {
            return await _context.FileVersions
                .Where(v => v.StorageItemId == storageItemId && !v.IsDeleted)
                .Include(v => v.CreatedBy)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();
        }

        public async Task<FileVersion?> GetVersionAsync(int versionId)
        {
            return await _context.FileVersions
                .Include(v => v.StorageItem)
                .Include(v => v.CreatedBy)
                .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted);
        }

        public async Task<bool> RestoreVersionAsync(int versionId, string userId)
        {
            var version = await GetVersionAsync(versionId);
            if (version == null || version.StorageItem == null)
            {
                return false;
            }

            // Before restoring, create a version of the current file
            await CreateVersionAsync(version.StorageItem, userId, "Before restoring version " + version.VersionNumber);

            // Copy the version file to replace the current file
            var versionFilePath = Path.Combine(_environment.WebRootPath, "uploads", version.FilePath);
            var currentFilePath = Path.Combine(_environment.WebRootPath, "uploads", version.StorageItem.FilePath);

            if (File.Exists(versionFilePath))
            {
                File.Copy(versionFilePath, currentFilePath, true);

                // Update the storage item's metadata
                version.StorageItem.Size = version.Size;
                version.StorageItem.FileHash = version.FileHash;
                version.StorageItem.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Restored version {VersionId} for file {ItemId}", versionId, version.StorageItemId);
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteVersionAsync(int versionId)
        {
            var version = await _context.FileVersions.FindAsync(versionId);
            if (version == null)
            {
                return false;
            }

            version.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted version {VersionId}", versionId);
            return true;
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var md5 = MD5.Create();
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
