using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CloudStorage.Services
{
    public interface ISharingService
    {
        Task<SharedItem> ShareItemAsync(int itemId, string sharedByUserId, string? sharedWithEmail, SharePermission permission, DateTime? expiresAt = null, bool allowDownload = true, bool notify = true, string? message = null);
        Task<SharedItem> CreatePublicLinkAsync(int itemId, string userId, SharePermission permission, DateTime? expiresAt = null, bool allowDownload = true);
        Task<bool> RevokeShareAsync(int shareId, string userId);
        Task<IEnumerable<SharedItem>> GetMySharesAsync(string userId);
        Task<IEnumerable<SharedItem>> GetSharedWithMeAsync(string userId);
        Task<SharedItem?> GetSharedItemByTokenAsync(string token);
        Task<bool> CanAccessSharedItemAsync(string token, string? userId = null);
        Task<bool> UpdateSharePermissionAsync(int shareId, SharePermission permission, string userId);
        Task<SharedItem?> GetShareByIdAsync(int shareId);
        Task UpdateShareAsync(int shareId, SharePermission permission, DateTime? expiresAt);
        Task DeleteShareAsync(int shareId);
        
        // New Google Drive-like methods
        Task<IEnumerable<SharedItem>> GetSharesForItemAsync(int itemId, string userId);
        Task<bool> ChangePermissionAsync(int shareId, SharePermission permission, bool allowDownload, string userId);
        Task<string> GetShareLinkAsync(int itemId, string userId);
        Task<Dictionary<string, object>> GetAccessInfoAsync(int shareId, string userId);
        Task SendShareNotificationAsync(SharedItem share);
        Task TrackAccessAsync(string token);
    }

    public class SharingService : ISharingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SharingService> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public SharingService(
            ApplicationDbContext context, 
            ILogger<SharingService> logger,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<SharedItem> ShareItemAsync(int itemId, string sharedByUserId, string? sharedWithEmail, SharePermission permission, DateTime? expiresAt = null, bool allowDownload = true, bool notify = true, string? message = null)
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
                existingShare.AllowDownload = allowDownload;
                existingShare.Message = message;
                _context.SharedItems.Update(existingShare);
                await _context.SaveChangesAsync();
                
                // Send notification if requested
                if (notify && !existingShare.NotificationSent)
                {
                    await SendShareNotificationAsync(existingShare);
                }
                
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
                IsActive = true,
                AllowDownload = allowDownload,
                Notify = notify,
                Message = message
            };

            _context.SharedItems.Add(share);
            await _context.SaveChangesAsync();

            // Send notification if requested
            if (notify)
            {
                await SendShareNotificationAsync(share);
            }

            _logger.LogInformation("Item {ItemId} shared by {SharedByUserId} with {SharedWithEmail}", itemId, sharedByUserId, sharedWithEmail);
            return share;
        }

        public async Task<SharedItem> CreatePublicLinkAsync(int itemId, string userId, SharePermission permission, DateTime? expiresAt = null, bool allowDownload = true)
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
                IsActive = true,
                AllowDownload = allowDownload
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

        // New Google Drive-like methods
        public async Task<IEnumerable<SharedItem>> GetSharesForItemAsync(int itemId, string userId)
        {
            // Verify ownership
            var item = await _context.StorageItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.OwnerId == userId && !i.IsDeleted);

            if (item == null)
            {
                throw new InvalidOperationException("Item not found or you don't have permission.");
            }

            return await _context.SharedItems
                .Include(s => s.SharedWithUser)
                .Where(s => s.StorageItemId == itemId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ChangePermissionAsync(int shareId, SharePermission permission, bool allowDownload, string userId)
        {
            var share = await _context.SharedItems
                .Include(s => s.StorageItem)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.SharedByUserId == userId);

            if (share == null) return false;

            share.Permission = permission;
            share.AllowDownload = allowDownload;
            
            _context.SharedItems.Update(share);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission changed for share {ShareId} to {Permission}", shareId, permission);
            return true;
        }

        public async Task<string> GetShareLinkAsync(int itemId, string userId)
        {
            // Get or create public link
            var existingPublicLink = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.StorageItemId == itemId && 
                                        s.SharedByUserId == userId &&
                                        s.SharedWithUserId == null && 
                                        s.SharedWithEmail == null && 
                                        s.IsActive);

            if (existingPublicLink != null)
            {
                var appUrl = _configuration["AppUrl"] ?? "http://localhost:5248";
                return $"{appUrl}/Share/PublicShare?token={existingPublicLink.AccessToken}";
            }

            // Create new public link if none exists
            var newShare = await CreatePublicLinkAsync(itemId, userId, SharePermission.Viewer);
            var baseUrl = _configuration["AppUrl"] ?? "http://localhost:5248";
            return $"{baseUrl}/Share/PublicShare?token={newShare.AccessToken}";
        }

        public async Task<Dictionary<string, object>> GetAccessInfoAsync(int shareId, string userId)
        {
            var share = await _context.SharedItems
                .Include(s => s.StorageItem)
                .Include(s => s.SharedWithUser)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.SharedByUserId == userId);

            if (share == null)
            {
                throw new InvalidOperationException("Share not found or you don't have permission.");
            }

            return new Dictionary<string, object>
            {
                ["shareId"] = share.Id,
                ["sharedWith"] = share.SharedWithEmail ?? share.SharedWithUser?.Email ?? "Anyone with the link",
                ["permission"] = share.Permission.ToString(),
                ["createdAt"] = share.CreatedAt,
                ["lastAccessedAt"] = share.LastAccessedAt,
                ["accessCount"] = share.AccessCount,
                ["expiresAt"] = share.ExpiresAt,
                ["allowDownload"] = share.AllowDownload
            };
        }

        public async Task SendShareNotificationAsync(SharedItem share)
        {
            try
            {
                // Load related data
                await _context.Entry(share)
                    .Reference(s => s.StorageItem)
                    .LoadAsync();
                await _context.Entry(share)
                    .Reference(s => s.SharedByUser)
                    .LoadAsync();

                if (string.IsNullOrEmpty(share.SharedWithEmail))
                {
                    return; // No email to send to (public link)
                }

                var appUrl = _configuration["AppUrl"] ?? "http://localhost:5248";
                var shareLink = $"{appUrl}/Share/PublicShare?token={share.AccessToken}";
                
                var sharedByName = !string.IsNullOrEmpty(share.SharedByUser.FirstName) 
                    ? $"{share.SharedByUser.FirstName} {share.SharedByUser.LastName}"
                    : share.SharedByUser.Email;

                var permissionText = share.Permission switch
                {
                    SharePermission.Viewer => "view",
                    SharePermission.Commenter => "comment on",
                    SharePermission.Editor => "edit",
                    SharePermission.Owner => "manage",
                    _ => "access"
                };

                var emailSubject = $"{sharedByName} shared \"{share.StorageItem.Name}\" with you";
                
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='color: white; margin: 0;'>📂 MyCloudStorage</h1>
                        </div>
                        <div style='background: white; padding: 40px; border: 1px solid #e0e0e0; border-radius: 0 0 10px 10px;'>
                            <h2 style='color: #333; margin-top: 0;'>{sharedByName} shared a file with you</h2>
                            <div style='background: #f5f5f5; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <p style='margin: 0; color: #666;'><strong>File:</strong> {share.StorageItem.Name}</p>
                                <p style='margin: 10px 0 0; color: #666;'><strong>Permission:</strong> You can {permissionText} this file</p>
                                {(!string.IsNullOrEmpty(share.Message) ? $"<p style='margin: 10px 0 0; color: #666;'><strong>Message:</strong> {share.Message}</p>" : "")}
                            </div>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{shareLink}' style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 50px; display: inline-block; font-weight: bold;'>
                                    Open File
                                </a>
                            </div>
                            {(share.ExpiresAt.HasValue ? $"<p style='color: #999; font-size: 14px; text-align: center;'>This link expires on {share.ExpiresAt.Value:MMMM dd, yyyy}</p>" : "")}
                        </div>
                        <div style='text-align: center; margin-top: 20px; color: #999; font-size: 12px;'>
                            <p>This is an automated message from MyCloudStorage</p>
                        </div>
                    </div>";

                await _emailService.SendEmailAsync(share.SharedWithEmail, emailSubject, emailBody);
                
                // Mark notification as sent
                share.NotificationSent = true;
                _context.SharedItems.Update(share);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Share notification sent to {Email} for share {ShareId}", share.SharedWithEmail, share.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send share notification for share {ShareId}", share.Id);
                // Don't throw - notification failure shouldn't stop the share
            }
        }

        public async Task TrackAccessAsync(string token)
        {
            var share = await _context.SharedItems
                .FirstOrDefaultAsync(s => s.AccessToken == token && s.IsActive);

            if (share != null)
            {
                share.LastAccessedAt = DateTime.UtcNow;
                share.AccessCount++;
                
                _context.SharedItems.Update(share);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Access tracked for share {ShareId}, count: {AccessCount}", share.Id, share.AccessCount);
            }
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