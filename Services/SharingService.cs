using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CloudStorage.Services
{
    public interface ISharingService
    {
        Task<SharedItem> ShareItemAsync(int itemId, string sharedByUserId, string? sharedWithEmail, SharePermission permission, DateTime? expiresAt = null);
        Task<SharedItem> CreatePublicLinkAsync(int itemId, string userId, SharePermission permission, DateTime? expiresAt = null);
        Task<bool> RevokeShareAsync(int shareId, string userId);
        Task<IEnumerable<SharedItem>> GetMySharesAsync(string userId);
        Task<IEnumerable<SharedItem>> GetSharedWithMeAsync(string userId);
        Task<SharedItem?> GetSharedItemByTokenAsync(string token);
        Task<bool> CanAccessSharedItemAsync(string token, string? userId = null);
        Task<bool> UpdateSharePermissionAsync(int shareId, SharePermission permission, string userId);
        Task<SharedItem?> GetShareByIdAsync(int shareId);
        Task UpdateShareAsync(int shareId, SharePermission permission, DateTime? expiresAt);
        Task DeleteShareAsync(int shareId);
    }

    public class SharingService : ISharingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SharingService> _logger;

        public SharingService(ApplicationDbContext context, ILogger<SharingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SharedItem> ShareItemAsync(int itemId, string sharedByUserId, string? sharedWithEmail, SharePermission permission, DateTime? expiresAt = null)
        {
            // Verify the item exists and the user owns it
            var item = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == sharedByUserId && !i.IsDeleted);

            if (item == null)
            {
                throw new InvalidOperationException("Item not found or you don't have permission to share it.");
            }

            // Check if already shared with this email
            var existingShare = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.StorageItemId == itemId && 
                                        s.SharedWithEmail == sharedWithEmail && 
                                        s.IsActive);

            if (existingShare != null)
            {
                // Update existing share
                existingShare.Permission = permission;
                existingShare.ExpiresAt = expiresAt;
                _context.SharedItems.Update(existingShare);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated share for item {ItemId} with {SharedWithEmail}", itemId, sharedWithEmail);
                return existingShare;
            }

            // Find user by email if they exist
            ApplicationUser? sharedWithUser = null;
            if (!string.IsNullOrEmpty(sharedWithEmail))
            {
                sharedWithUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == sharedWithEmail);
            }

            var share = new SharedItem
            {
                StorageItemId = itemId,
                SharedByUserId = sharedByUserId,
                SharedWithUserId = sharedWithUser?.Id,
                SharedWithEmail = sharedWithEmail,
                Permission = permission,
                ExpiresAt = expiresAt,
                AccessToken = GenerateAccessToken(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.SharedItems.Add(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Item {ItemId} shared by {SharedByUserId} with {SharedWithEmail}", itemId, sharedByUserId, sharedWithEmail);
            return share;
        }

        public async Task<SharedItem> CreatePublicLinkAsync(int itemId, string userId, SharePermission permission, DateTime? expiresAt = null)
        {
            // Verify the item exists and the user owns it
            var item = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == userId && !i.IsDeleted);

            if (item == null)
            {
                throw new InvalidOperationException("Item not found or you don't have permission to share it.");
            }

            // Check if a public link already exists
            var existingPublicLink = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.StorageItemId == itemId && 
                                        s.SharedWithUserId == null && 
                                        s.SharedWithEmail == null && 
                                        s.IsActive);

            if (existingPublicLink != null)
            {
                // Update existing public link
                existingPublicLink.Permission = permission;
                existingPublicLink.ExpiresAt = expiresAt;
                existingPublicLink.AccessToken = GenerateAccessToken(); // Generate new token
                _context.SharedItems.Update(existingPublicLink);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated public link for item {ItemId}", itemId);
                return existingPublicLink;
            }

            var publicShare = new SharedItem
            {
                StorageItemId = itemId,
                SharedByUserId = userId,
                SharedWithUserId = null,
                SharedWithEmail = null,
                Permission = permission,
                ExpiresAt = expiresAt,
                AccessToken = GenerateAccessToken(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.SharedItems.Add(publicShare);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Public link created for item {ItemId} by user {UserId}", itemId, userId);
            return publicShare;
        }

        public async Task<bool> RevokeShareAsync(int shareId, string userId)
        {
            var share = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.Id == shareId && s.SharedByUserId == userId);

            if (share == null) return false;

            share.IsActive = false;
            _context.SharedItems.Update(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Share {ShareId} revoked by user {UserId}", shareId, userId);
            return true;
        }

        public async Task<IEnumerable<SharedItem>> GetMySharesAsync(string userId)
        {
            return await _context.SharedItems
                .Include(s => s.StorageItem)
                .Include(s => s.SharedWithUser)
                .Where(s => s.SharedByUserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SharedItem>> GetSharedWithMeAsync(string userId)
        {
            return await _context.SharedItems
                .Include(s => s.StorageItem)
                .Include(s => s.SharedByUser)
                .Where(s => s.SharedWithUserId == userId && 
                           s.IsActive &&
                           (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<SharedItem?> GetSharedItemByTokenAsync(string token)
        {
            return await _context.SharedItems
                .Include(s => s.StorageItem)
                .ThenInclude(i => i.Owner)
                .Include(s => s.SharedByUser)
                .FirstOrDefaultAsync(s => s.AccessToken == token && 
                                        s.IsActive &&
                                        (s.ExpiresAt == null || s.ExpiresAt > DateTime.UtcNow));
        }

        public async Task<bool> CanAccessSharedItemAsync(string token, string? userId = null)
        {
            var sharedItem = await GetSharedItemByTokenAsync(token);
            
            if (sharedItem == null) return false;

            // Check if it's a public link or user-specific share
            if (sharedItem.SharedWithUserId == null) return true; // Public link
            
            return sharedItem.SharedWithUserId == userId; // User-specific share
        }

        public async Task<bool> UpdateSharePermissionAsync(int shareId, SharePermission permission, string userId)
        {
            var share = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.Id == shareId && s.SharedByUserId == userId);

            if (share == null) return false;

            share.Permission = permission;
            _context.SharedItems.Update(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Share permission updated for share {ShareId} by user {UserId}", shareId, userId);
            return true;
        }

        public async Task<SharedItem?> GetShareByIdAsync(int shareId)
        {
            return await _context.SharedItems
                .Include(s => s.StorageItem)
                .Include(s => s.SharedWithUser)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.IsActive);
        }

        public async Task UpdateShareAsync(int shareId, SharePermission permission, DateTime? expiresAt)
        {
            var share = await _context.SharedItems.FindAsync(shareId);
            
            if (share == null)
            {
                throw new InvalidOperationException("Share not found.");
            }

            share.Permission = permission;
            share.ExpiresAt = expiresAt;

            _context.SharedItems.Update(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Share {ShareId} updated: Permission={Permission}, ExpiresAt={ExpiresAt}", 
                shareId, permission, expiresAt);
        }

        public async Task DeleteShareAsync(int shareId)
        {
            var share = await _context.SharedItems.FindAsync(shareId);
            
            if (share == null)
            {
                throw new InvalidOperationException("Share not found.");
            }

            share.IsActive = false;

            _context.SharedItems.Update(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Share {ShareId} removed", shareId);
        }

        private static string GenerateAccessToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}