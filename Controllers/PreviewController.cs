using CloudStorage.Services;
using CloudStorage.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class PreviewController : Controller
    {
        private readonly IDocumentPreviewService _previewService;
        private readonly IStorageService _storageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<PreviewController> _logger;
        private readonly IWebHostEnvironment _environment;

        public PreviewController(
            IDocumentPreviewService previewService,
            IStorageService storageService,
            IFileStorageService fileStorageService,
            ILogger<PreviewController> logger,
            IWebHostEnvironment environment)
        {
            _previewService = previewService;
            _storageService = storageService;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _environment = environment;
        }

        // GET: Preview/Document/5
        public async Task<IActionResult> Document(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var item = await _storageService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Check if user owns this file
            if (item.OwnerId != userId)
            {
                return Forbid();
            }

            // Check if file type is supported
            if (!_previewService.IsSupportedFormat(item.Name))
            {
                TempData["Error"] = "This file type is not supported for preview.";
                return RedirectToAction("Index", "Storage", new { folderId = item.ParentFolderId });
            }

            ViewBag.Item = item;
            return View();
        }

        // API endpoint to get preview data
        [HttpGet]
        public async Task<IActionResult> GetPreview(int id, int maxPages = 10)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetPreview: User not authenticated");
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var item = await _storageService.GetItemByIdAsync(id);
                if (item == null)
                {
                    _logger.LogWarning("GetPreview: File not found for id {ItemId}", id);
                    return NotFound(new { error = "File not found", itemId = id });
                }

                _logger.LogInformation("GetPreview: Found item {ItemId}, Name={Name}, Type={Type}, FilePath={FilePath}", 
                    item.Id, item.Name, item.Type, item.FilePath);

                // Check if user owns this file
                if (item.OwnerId != userId)
                {
                    _logger.LogWarning("GetPreview: User {UserId} does not own item {ItemId}", userId, id);
                    return Forbid();
                }

                if (item.Type != StorageItemType.File)
                {
                    _logger.LogWarning("GetPreview: Item {ItemId} is not a file (Type={Type})", id, item.Type);
                    return BadRequest(new { error = "Only files can be previewed" });
                }

                if (string.IsNullOrEmpty(item.FilePath))
                {
                    _logger.LogError("GetPreview: Item {ItemId} has empty FilePath", id);
                    return BadRequest(new { error = "File path is not set" });
                }

                // Convert relative path to absolute path
                var absolutePath = Path.IsPathRooted(item.FilePath) 
                    ? item.FilePath 
                    : Path.Combine(_environment.WebRootPath, "uploads", item.FilePath);

                _logger.LogInformation("GetPreview: Relative path: {RelativePath}, Absolute path: {AbsolutePath}", 
                    item.FilePath, absolutePath);

                var preview = await _previewService.GetPreviewAsync(absolutePath, maxPages);
                
                if (!string.IsNullOrEmpty(preview.Error))
                {
                    _logger.LogError("GetPreview: Preview error for {ItemId}: {Error}", id, preview.Error);
                    return BadRequest(new { error = preview.Error });
                }

                return Json(new
                {
                    success = true,
                    fileName = item.Name,
                    fileType = preview.FileType,
                    content = preview.Content,
                    pages = preview.Pages,
                    totalPages = preview.TotalPages,
                    metadata = preview.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview for item {ItemId}", id);
                return StatusCode(500, new { error = "An error occurred while generating preview" });
            }
        }

        // Download original file
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var item = await _storageService.GetItemByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Check if user owns this file
            if (item.OwnerId != userId)
            {
                return Forbid();
            }

            // Convert relative path to absolute path for file storage service
            var absolutePath = Path.IsPathRooted(item.FilePath) 
                ? item.FilePath 
                : Path.Combine(_environment.WebRootPath, "uploads", item.FilePath);

            var fileBytes = await _fileStorageService.GetFileAsync(absolutePath);
            var mimeType = _fileStorageService.GetMimeType(item.Name);

            return File(fileBytes, mimeType, item.Name);
        }
    }
}
