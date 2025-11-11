using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudStorage.Services
{
    public interface IStorageService
    {
        Task<IEnumerable<StorageItem>> GetUserItemsAsync(string userId, int? parentFolderId = null);
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
    }

    public class StorageService : IStorageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StorageService> _logger;

        public StorageService(ApplicationDbContext context, ILogger<StorageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<StorageItem>> GetUserItemsAsync(string userId, int? parentFolderId = null)
        {
            return await _context.StorageItems
                .Where(item => item.OwnerId == userId && 
                              item.ParentFolderId == parentFolderId && 
                              !item.IsDeleted)
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Name)
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

            // Soft delete
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;

            // If it's a folder, soft delete all sub-items recursively
            if (item.Type == StorageItemType.Folder)
            {
                await SoftDeleteSubItemsAsync(id, userId);
            }

            // Update user's storage usage if it's a file
            if (item.Type == StorageItemType.File)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.UsedStorage -= item.Size;
                    _context.Users.Update(user);
                }
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
            var searchQuery = _context.StorageItems
                .Where(item => item.OwnerId == userId && 
                              !item.IsDeleted &&
                              item.Name.Contains(query));

            if (itemType.HasValue)
            {
                searchQuery = searchQuery.Where(item => item.Type == itemType.Value);
            }

            return await searchQuery
                .OrderBy(item => item.Type)
                .ThenBy(item => item.Name)
                .Take(100) // Limit results
                .ToListAsync();
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
    }
}