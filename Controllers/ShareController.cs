using Microsoft.AspNetCore.Mvc;
using CloudStorage.Services;
using CloudStorage.Models;

namespace CloudStorage.Controllers
{
    public class ShareController : Controller
    {
        private readonly ISharingService _sharingService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<ShareController> _logger;

        public ShareController(
            ISharingService sharingService,
            IFileStorageService fileStorageService,
            ILogger<ShareController> logger)
        {
            _sharingService = sharingService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> PublicShare(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var sharedItem = await _sharingService.GetSharedItemByTokenAsync(token);

            if (sharedItem == null)
            {
                ViewBag.ErrorMessage = "This shared link is invalid or has expired.";
                return View("ShareError");
            }

            // Track access
            await _sharingService.TrackAccessAsync(token);

            return View(sharedItem);
        }

        [HttpGet]
        public async Task<IActionResult> Download(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var sharedItem = await _sharingService.GetSharedItemByTokenAsync(token);

            if (sharedItem == null)
            {
                return NotFound();
            }

            // Check permissions
            if (!sharedItem.AllowDownload && sharedItem.Permission == SharePermission.Viewer)
            {
                return Forbid("This share does not allow downloading.");
            }

            var item = sharedItem.StorageItem;
            if (item.Type != StorageItemType.File)
            {
                return BadRequest("Cannot download a folder.");
            }

            try
            {
                var fileBytes = await _fileStorageService.GetFileAsync(item.FilePath);
                
                _logger.LogInformation("File {FileId} downloaded via share token {Token}", item.Id, token);
                
                return File(fileBytes, item.MimeType, item.Name);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading shared file {FileId} with token {Token}", item.Id, token);
                return StatusCode(500, "An error occurred while downloading the file.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var sharedItem = await _sharingService.GetSharedItemByTokenAsync(token);

            if (sharedItem == null)
            {
                return NotFound();
            }

            var item = sharedItem.StorageItem;
            if (item.Type != StorageItemType.File)
            {
                return BadRequest("Cannot preview a folder.");
            }

            // Check if file type supports preview
            var supportedTypes = new[]
            {
                "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp", "image/svg+xml",
                "text/plain", "text/html", "text/css", "application/javascript", "application/json",
                "application/pdf"
            };

            if (!supportedTypes.Contains(item.MimeType))
            {
                return BadRequest("File type not supported for preview.");
            }

            try
            {
                var fileBytes = await _fileStorageService.GetFileAsync(item.FilePath);

                _logger.LogInformation("File {FileId} previewed via share token {Token}", item.Id, token);

                return File(fileBytes, item.MimeType);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing shared file {FileId} with token {Token}", item.Id, token);
                return StatusCode(500, "An error occurred while previewing the file.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Revoke(int shareId)
        {
            // This would need to be called from the StorageController with proper user authorization
            // For now, we'll just return a not found since we can't verify the user
            return NotFound();
        }

        // New Google Drive-like actions
        [HttpGet]
        public async Task<IActionResult> GetSharesForItem(int itemId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var shares = await _sharingService.GetSharesForItemAsync(itemId, userId);
                
                // Project to DTO to avoid circular reference
                var shareData = shares.Select(s => new
                {
                    id = s.Id,
                    permission = s.Permission.ToString(),
                    allowDownload = s.AllowDownload,
                    sharedWithEmail = s.SharedWithEmail,
                    sharedWithUserId = s.SharedWithUserId,
                    sharedWithUser = s.SharedWithUser != null ? new { 
                        email = s.SharedWithUser.Email, 
                        firstName = s.SharedWithUser.FirstName,
                        lastName = s.SharedWithUser.LastName
                    } : null,
                    accessToken = s.AccessToken,
                    expiresAt = s.ExpiresAt,
                    createdAt = s.CreatedAt,
                    accessCount = s.AccessCount,
                    lastAccessedAt = s.LastAccessedAt,
                    message = s.Message,
                    isPublicLink = string.IsNullOrEmpty(s.SharedWithUserId) && string.IsNullOrEmpty(s.SharedWithEmail)
                }).ToList();
                
                return Json(new { success = true, shares = shareData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shares for item {ItemId}", itemId);
                return Json(new { success = false, message = "Failed to load shares" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePermission(int shareId, SharePermission permission, bool allowDownload = true)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var success = await _sharingService.ChangePermissionAsync(shareId, permission, allowDownload, userId);
                if (success)
                {
                    return Json(new { success = true, message = "Permission updated successfully" });
                }
                return Json(new { success = false, message = "Share not found or permission denied" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing permission for share {ShareId}", shareId);
                return Json(new { success = false, message = "Failed to update permission" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAccess(int shareId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                await _sharingService.DeleteShareAsync(shareId);
                return Json(new { success = true, message = "Access removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing access for share {ShareId}", shareId);
                return Json(new { success = false, message = "Failed to remove access" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShareLink(int itemId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var link = await _sharingService.GetShareLinkAsync(itemId, userId);
                return Json(new { success = true, link });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting share link for item {ItemId}", itemId);
                return Json(new { success = false, message = "Failed to get share link" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAccessInfo(int shareId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var info = await _sharingService.GetAccessInfoAsync(shareId, userId);
                return Json(new { success = true, info });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access info for share {ShareId}", shareId);
                return Json(new { success = false, message = "Failed to get access info" });
            }
        }
    }
}