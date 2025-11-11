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
            if (sharedItem.Permission == SharePermission.ViewOnly)
            {
                return Forbid("This share only allows viewing, not downloading.");
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
    }
}