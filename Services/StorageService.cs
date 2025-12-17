using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudStorage.Services
{
    public interface IStorageService
    {
        Task<IEnumerable<StorageItem>> GetUserItemsAsync(string userId, int? parentFolderId = null, string sortBy = "name", string sortOrder = "asc");
        Task<IEnumerable<StorageItem>> GetAllUserItemsAsync(string userId);
        Task<StorageItem?> GetItemAsync(int id, string userId);
        Task<StorageItem?> GetItemByIdAsync(int id);
        Task<StorageItem> CreateFolderAsync(string name, string description, string userId, int? parentFolderId = null, bool isPublic = false);
        Task<StorageItem> CreateFileAsync(string name, string filePath, long size, string mimeType, string fileHash, string userId, int? parentFolderId = null, bool isPublic = false, string description = "");
        Task<bool> RenameItemAsync(int id, string newName, string description, string userId);
        Task<bool> DeleteItemAsync(int id, string userId);
        Task<bool> MoveItemAsync(int id, int? newParentFolderId, string userId);
        Task<IEnumerable<StorageItem>> SearchItemsAsync(string userId, string query, StorageItemType? itemType = null);
        Task<long> GetUserStorageUsageAsync(string userId);
        Task<StorageItem?> GetFolderPathAsync(int folderId, string userId);
        Task<IEnumerable<StorageItem>> GetBreadcrumbPathAsync(int? folderId, string userId);
        Task<bool> CanUserAccessItemAsync(int itemId, string userId);
        Task<bool> ItemExistsAsync(string name, int? parentFolderId, string userId);
        Task<IEnumerable<StorageItem>> GetDeletedItemsAsync(string userId);
        Task<bool> RestoreItemAsync(int id, string userId);
        Task<bool> PermanentlyDeleteItemAsync(int id, string userId);
        Task<int> CleanupOldDeletedItemsAsync();
    }

    public class StorageService : IStorageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StorageService> _logger;
        private readonly ISemanticSearchService _semanticSearchService;

        public StorageService(ApplicationDbContext context, ILogger<StorageService> logger, ISemanticSearchService semanticSearchService)
        {
            _context = context;
            _logger = logger;
            _semanticSearchService = semanticSearchService;
        }

        public async Task<IEnumerable<StorageItem>> GetUserItemsAsync(string userId, int? parentFolderId = null, string sortBy = "name", string sortOrder = "asc")
        {
            var query = _context.StorageItems
                .Include(item => item.Shares)
                .Where(item => item.OwnerId == userId && 
                              item.ParentFolderId == parentFolderId && 
                              !item.IsDeleted);

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "date" when sortOrder == "desc" => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.CreatedAt),
                "date" => query.OrderBy(item => item.Type).ThenBy(item => item.CreatedAt),
                "size" when sortOrder == "desc" => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.Size),
                "size" => query.OrderBy(item => item.Type).ThenBy(item => item.Size),
                "name" when sortOrder == "desc" => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.Name),
                _ => query.OrderBy(item => item.Type).ThenBy(item => item.Name) // default: name asc
            };

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<StorageItem>> GetAllUserItemsAsync(string userId)
        {
            return await _context.StorageItems
                .Where(item => item.OwnerId == userId && !item.IsDeleted)
                .ToListAsync();
        }

        public async Task<StorageItem?> GetItemAsync(int id, string userId)
        {
            return await _context.StorageItems
                .FirstOrDefaultAsync(item => item.Id == id && 
                                           item.OwnerId == userId && 
                                           !item.IsDeleted);
        }

        public async Task<StorageItem?> GetItemByIdAsync(int id)
        {
            return await _context.StorageItems
                .Include(item => item.Owner)
                .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        }

        public async Task<StorageItem> CreateFolderAsync(string name, string description, string userId, int? parentFolderId = null, bool isPublic = false)
        {
            // Check if folder with same name exists in the same location
            var existingFolder = await _context.StorageItems
                .FirstOrDefaultAsync(item => item.Name == name && 
                                           item.ParentFolderId == parentFolderId && 
                                           item.OwnerId == userId && 
                                           !item.IsDeleted);

            if (existingFolder != null)
            {
                throw new InvalidOperationException($"A folder with the name '{name}' already exists in this location.");
            }

            var folder = new StorageItem
            {
                Name = name,
                Description = description,
                Type = StorageItemType.Folder,
                OwnerId = userId,
                ParentFolderId = parentFolderId,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _context.StorageItems.Add(folder);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Folder '{FolderName}' created by user {UserId}", name, userId);
            return folder;
        }

        public async Task<StorageItem> CreateFileAsync(string name, string filePath, long size, string mimeType, string fileHash, string userId, int? parentFolderId = null, bool isPublic = false, string description = "")
        {
            // Check if file with same name exists in the same location
            var existingFile = await _context.StorageItems
                .FirstOrDefaultAsync(item => item.Name == name && 
                                           item.ParentFolderId == parentFolderId && 
                                           item.OwnerId == userId && 
                                           !item.IsDeleted);

            if (existingFile != null)
            {
                throw new InvalidOperationException($"A file with the name '{name}' already exists in this location.");
            }

            var file = new StorageItem
            {
                Name = name,
                Description = description,
                Type = StorageItemType.File,
                Size = size,
                FilePath = filePath,
                MimeType = mimeType,
                FileHash = fileHash,
                OwnerId = userId,
                ParentFolderId = parentFolderId,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            _context.StorageItems.Add(file);

            // Update user's storage usage
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.UsedStorage += size;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("File '{FileName}' uploaded by user {UserId}, size: {FileSize} bytes", name, userId, size);
            return file;
        }

        public async Task<bool> RenameItemAsync(int id, string newName, string description, string userId)
        {
            var item = await GetItemAsync(id, userId);
            if (item == null) return false;

            // Check if another item with the new name exists in the same location
            var existingItem = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Name == newName && 
                                        i.ParentFolderId == item.ParentFolderId && 
                                        i.OwnerId == userId && 
                                        i.Id != id && 
                                        !i.IsDeleted);

            if (existingItem != null)
            {
                throw new InvalidOperationException($"An item with the name '{newName}' already exists in this location.");
            }

            item.Name = newName;
            item.Description = description;
            item.ModifiedAt = DateTime.UtcNow;

            _context.StorageItems.Update(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} renamed to '{NewName}' by user {UserId}", id, newName, userId);
            return true;
        }

        public async Task<bool> DeleteItemAsync(int id, string userId)
        {
            var item = await GetItemAsync(id, userId);
            if (item == null) return false;

            // Soft delete - storage quota remains unchanged until permanent delete
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;

            // If it's a folder, soft delete all sub-items recursively
            if (item.Type == StorageItemType.Folder)
            {
                await SoftDeleteSubItemsAsync(id, userId);
            }

            _context.StorageItems.Update(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} deleted by user {UserId}", id, userId);
            return true;
        }

        private async Task SoftDeleteSubItemsAsync(int folderId, string userId)
        {
            var subItems = await _context.StorageItems
                .Where(item => item.ParentFolderId == folderId && 
                              item.OwnerId == userId && 
                              !item.IsDeleted)
                .ToListAsync();

            foreach (var subItem in subItems)
            {
                subItem.IsDeleted = true;
                subItem.DeletedAt = DateTime.UtcNow;

                if (subItem.Type == StorageItemType.Folder)
                {
                    await SoftDeleteSubItemsAsync(subItem.Id, userId);
                }
                else if (subItem.Type == StorageItemType.File)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.UsedStorage -= subItem.Size;
                        _context.Users.Update(user);
                    }
                }
            }

            _context.StorageItems.UpdateRange(subItems);
        }

        public async Task<bool> MoveItemAsync(int id, int? newParentFolderId, string userId)
        {
            var item = await GetItemAsync(id, userId);
            if (item == null) return false;

            // Check if target is not a descendant of the item being moved (prevent circular references)
            if (item.Type == StorageItemType.Folder && newParentFolderId.HasValue)
            {
                var isDescendant = await IsDescendantAsync(newParentFolderId.Value, id, userId);
                if (isDescendant)
                {
                    throw new InvalidOperationException("Cannot move a folder into its own subfolder.");
                }
            }

            // Check if an item with the same name already exists in the target location
            var existingItem = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Name == item.Name && 
                                        i.ParentFolderId == newParentFolderId && 
                                        i.OwnerId == userId && 
                                        i.Id != id && 
                                        !i.IsDeleted);

            if (existingItem != null)
            {
                throw new InvalidOperationException($"An item with the name '{item.Name}' already exists in the target location.");
            }

            item.ParentFolderId = newParentFolderId;
            item.ModifiedAt = DateTime.UtcNow;

            _context.StorageItems.Update(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} moved to folder {NewParentFolderId} by user {UserId}", id, newParentFolderId, userId);
            return true;
        }

        private async Task<bool> IsDescendantAsync(int folderId, int ancestorId, string userId)
        {
            var folder = await _context.StorageItems
                .FirstOrDefaultAsync(f => f.Id == folderId && 
                                        f.OwnerId == userId && 
                                        !f.IsDeleted);

            while (folder?.ParentFolderId != null)
            {
                if (folder.ParentFolderId == ancestorId)
                    return true;

                folder = await _context.StorageItems
                    .FirstOrDefaultAsync(f => f.Id == folder.ParentFolderId && 
                                            f.OwnerId == userId && 
                                            !f.IsDeleted);
            }

            return false;
        }

        public async Task<IEnumerable<StorageItem>> SearchItemsAsync(string userId, string query, StorageItemType? itemType = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<StorageItem>();
            }

            // Get all items for the user
            var allItemsQuery = _context.StorageItems
                .Where(item => item.OwnerId == userId && !item.IsDeleted);

            if (itemType.HasValue)
            {
                allItemsQuery = allItemsQuery.Where(item => item.Type == itemType.Value);
            }

            var allItems = await allItemsQuery.ToListAsync();

            // Perform semantic search in memory
            var results = new List<(StorageItem item, double score)>();

            foreach (var item in allItems)
            {
                // Calculate similarity for name
                double nameScore = _semanticSearchService.CalculateSimilarity(query, item.Name);
                
                // Calculate similarity for description
                double descScore = 0.0;
                if (!string.IsNullOrWhiteSpace(item.Description))
                {
                    descScore = _semanticSearchService.CalculateSimilarity(query, item.Description);
                }

                // Calculate similarity for file content (only for files with FilePath)
                double contentScore = 0.0;
                if (item.Type == StorageItemType.File && !string.IsNullOrEmpty(item.FilePath))
                {
                    try
                    {
                        var absolutePath = Path.IsPathRooted(item.FilePath)
                            ? item.FilePath
                            : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", item.FilePath);
                        
                        var content = await _semanticSearchService.ExtractFileContentAsync(absolutePath);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            // Search in first 10000 characters for performance
                            var searchableContent = content.Length > 10000 ? content.Substring(0, 10000) : content;
                            
                            // Normalize content: replace multiple whitespace/newlines with single space for better matching
                            var normalizedContent = System.Text.RegularExpressions.Regex.Replace(searchableContent, @"\s+", " ");
                            
                            // Check for exact phrase match (case-insensitive, whitespace-normalized)
                            if (normalizedContent.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                contentScore = 1.0; // Perfect match for exact phrase
                                _logger.LogInformation("Exact phrase match found in {FileName}", item.Name);
                            }
                            else
                            {
                                // Fall back to semantic similarity
                                contentScore = _semanticSearchService.CalculateSimilarity(query, searchableContent);
                            }
                            
                            _logger.LogInformation("{FileName} content score: {Score}", item.Name, contentScore);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract content for search from {FilePath}: {Error}", item.FilePath, ex.Message);
                    }
                }

                // Take the highest score (name > content > description)
                // Content gets higher weight (0.95) to make content-based search more effective
                double finalScore = Math.Max(Math.Max(nameScore, contentScore * 0.95), descScore * 0.7);

                _logger.LogInformation("{FileName}: Name={NameScore}, Content={ContentScore}, Desc={DescScore}, Final={FinalScore}", 
                    item.Name, nameScore, contentScore, descScore, finalScore);

                // Include items with score >= 0.3 (lower threshold for better content recall)
                if (finalScore >= 0.3)
                {
                    results.Add((item, finalScore));
                }
            }

            // Sort by score descending, then by type and name
            return results
                .OrderByDescending(r => r.score)
                .ThenBy(r => r.item.Type)
                .ThenBy(r => r.item.Name)
                .Take(100) // Limit results
                .Select(r => r.item)
                .ToList();
        }

        public async Task<long> GetUserStorageUsageAsync(string userId)
        {
            return await _context.StorageItems
                .Where(item => item.OwnerId == userId && 
                              item.Type == StorageItemType.File && 
                              !item.IsDeleted)
                .SumAsync(item => item.Size);
        }

        public async Task<StorageItem?> GetFolderPathAsync(int folderId, string userId)
        {
            return await _context.StorageItems
                .FirstOrDefaultAsync(item => item.Id == folderId && 
                                           item.OwnerId == userId && 
                                           item.Type == StorageItemType.Folder && 
                                           !item.IsDeleted);
        }

        public async Task<IEnumerable<StorageItem>> GetBreadcrumbPathAsync(int? folderId, string userId)
        {
            var breadcrumbs = new List<StorageItem>();

            while (folderId.HasValue)
            {
                var folder = await GetFolderPathAsync(folderId.Value, userId);
                if (folder == null) break;

                breadcrumbs.Insert(0, folder);
                folderId = folder.ParentFolderId;
            }

            return breadcrumbs;
        }

        public async Task<bool> CanUserAccessItemAsync(int itemId, string userId)
        {
            var item = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted);

            if (item == null) return false;

            // User owns the item
            if (item.OwnerId == userId) return true;

            // Item is public
            if (item.IsPublic) return true;

            // Item is shared with the user
            var sharedItem = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.StorageItemId == itemId && 
                                        s.SharedWithUserId == userId && 
                                        s.IsActive &&
                                        (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow));

            return sharedItem != null;
        }

        public async Task<bool> ItemExistsAsync(string name, int? parentFolderId, string userId)
        {
            return await _context.StorageItems
                .AnyAsync(item => item.Name == name && 
                                 item.ParentFolderId == parentFolderId && 
                                 item.OwnerId == userId && 
                                 !item.IsDeleted);
        }

        public async Task<IEnumerable<StorageItem>> GetDeletedItemsAsync(string userId)
        {
            return await _context.StorageItems
                .Where(item => item.OwnerId == userId && 
                              item.IsDeleted && 
                              item.DeletedAt.HasValue)
                .OrderByDescending(item => item.DeletedAt)
                .ToListAsync();
        }

        public async Task<bool> RestoreItemAsync(int id, string userId)
        {
            var item = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId && i.IsDeleted);
            
            if (item == null) return false;

            // Restore the item
            item.IsDeleted = false;
            item.DeletedAt = null;

            // If it's a folder, restore all sub-items recursively
            if (item.Type == StorageItemType.Folder)
            {
                await RestoreSubItemsAsync(id, userId);
            }

            // Update user's storage usage if it's a file
            if (item.Type == StorageItemType.File)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.UsedStorage += item.Size;
                    _context.Users.Update(user);
                }
            }

            _context.StorageItems.Update(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} restored by user {UserId}", id, userId);
            return true;
        }

        private async Task RestoreSubItemsAsync(int folderId, string userId)
        {
            var subItems = await _context.StorageItems
                .Where(item => item.ParentFolderId == folderId && 
                              item.OwnerId == userId && 
                              item.IsDeleted)
                .ToListAsync();

            foreach (var subItem in subItems)
            {
                subItem.IsDeleted = false;
                subItem.DeletedAt = null;

                if (subItem.Type == StorageItemType.Folder)
                {
                    await RestoreSubItemsAsync(subItem.Id, userId);
                }
                else if (subItem.Type == StorageItemType.File)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.UsedStorage += subItem.Size;
                    }
                }

                _context.StorageItems.Update(subItem);
            }
        }

        public async Task<bool> PermanentlyDeleteItemAsync(int id, string userId)
        {
            var item = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId && i.IsDeleted);
            
            if (item == null) return false;

            // Calculate total size to reduce from storage quota
            long totalSize = await CalculateItemSizeAsync(item);

            // If it's a folder, permanently delete all sub-items
            if (item.Type == StorageItemType.Folder)
            {
                await PermanentlyDeleteSubItemsAsync(id);
            }

            // Update user's storage usage
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.UsedStorage -= totalSize;
                _context.Users.Update(user);
            }

            // Remove from database
            _context.StorageItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} permanently deleted by user {UserId}", id, userId);
            return true;
        }

        private async Task<long> CalculateItemSizeAsync(StorageItem item)
        {
            if (item.Type == StorageItemType.File)
            {
                return item.Size;
            }
            
            // For folders, calculate total size of all files inside
            long totalSize = 0;
            var subItems = await _context.StorageItems
                .Where(i => i.ParentFolderId == item.Id)
                .ToListAsync();

            foreach (var subItem in subItems)
            {
                if (subItem.Type == StorageItemType.File)
                {
                    totalSize += subItem.Size;
                }
                else
                {
                    totalSize += await CalculateItemSizeAsync(subItem);
                }
            }

            return totalSize;
        }

        private async Task PermanentlyDeleteSubItemsAsync(int folderId)
        {
            var subItems = await _context.StorageItems
                .Where(item => item.ParentFolderId == folderId)
                .ToListAsync();

            foreach (var subItem in subItems)
            {
                if (subItem.Type == StorageItemType.Folder)
                {
                    await PermanentlyDeleteSubItemsAsync(subItem.Id);
                }
                _context.StorageItems.Remove(subItem);
            }
        }

        public async Task<int> CleanupOldDeletedItemsAsync()
        {
            var fifteenDaysAgo = DateTime.UtcNow.AddDays(-15);
            
            var oldDeletedItems = await _context.StorageItems
                .Where(item => item.IsDeleted && 
                              item.DeletedAt.HasValue && 
                              item.DeletedAt.Value <= fifteenDaysAgo &&
                              item.ParentFolderId == null) // Only get top-level items
                .ToListAsync();

            int count = 0;
            foreach (var item in oldDeletedItems)
            {
                // Calculate total size
                long totalSize = await CalculateItemSizeAsync(item);

                // Delete physical files
                await DeletePhysicalFilesAsync(item);

                // Update user's storage usage
                var user = await _context.Users.FindAsync(item.OwnerId);
                if (user != null)
                {
                    user.UsedStorage -= totalSize;
                    _context.Users.Update(user);
                }

                // If it's a folder, permanently delete all sub-items
                if (item.Type == StorageItemType.Folder)
                {
                    await PermanentlyDeleteSubItemsAsync(item.Id);
                }

                _context.StorageItems.Remove(item);
                count++;
            }

            if (count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} old deleted items (older than 15 days)", count);
            }

            return count;
        }

        private async Task DeletePhysicalFilesAsync(StorageItem item)
        {
            if (item.Type == StorageItemType.File && !string.IsNullOrEmpty(item.FilePath))
            {
                try
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", item.FilePath.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        _logger.LogInformation("Deleted physical file: {FilePath}", item.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting physical file {FilePath}", item.FilePath);
                }
            }
            else if (item.Type == StorageItemType.Folder)
            {
                // Recursively delete files in folder
                var subItems = await _context.StorageItems
                    .Where(i => i.ParentFolderId == item.Id)
                    .ToListAsync();

                foreach (var subItem in subItems)
                {
                    await DeletePhysicalFilesAsync(subItem);
                }
            }
        }
    }
}