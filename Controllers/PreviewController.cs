using CloudStorage.Services;
using CloudStorage.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CloudStorage.Data;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class PreviewController : Controller
    {
        private readonly IDocumentPreviewService _previewService;
        private readonly IStorageService _storageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IActivityService _activityService;
        private readonly ILogger<PreviewController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;

        public PreviewController(
            IDocumentPreviewService previewService,
            IStorageService storageService,
            IFileStorageService fileStorageService,
            IActivityService activityService,
            ILogger<PreviewController> logger,
            IWebHostEnvironment environment,
            ApplicationDbContext context)
        {
            _previewService = previewService;
            _storageService = storageService;
            _fileStorageService = fileStorageService;
            _activityService = activityService;
            _logger = logger;
            _environment = environment;
            _context = context;
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

            // Check if user has access to this file (owner or shared with them)
            var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            // Check if file is a video
            var extension = Path.GetExtension(item.Name).ToLowerInvariant();
            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".m4v" };
            if (videoExtensions.Contains(extension))
            {
                return RedirectToAction("Video", new { id });
            }

            // Check if file type is supported
            if (!_previewService.IsSupportedFormat(item.Name))
            {
                TempData["Error"] = "This file type is not supported for preview.";
                return RedirectToAction("Index", "Storage", new { folderId = item.ParentFolderId });
            }

            // Log activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            await _activityService.LogActivityAsync(
                item.Id,
                userId,
                ActivityType.FileViewed,
                $"Viewed file: {item.Name}",
                ipAddress,
                userAgent
            );

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

                // Check if user has access to this file (owner or shared with them)
                var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
                if (!hasAccess)
                {
                    _logger.LogWarning("GetPreview: User {UserId} does not have access to item {ItemId}", userId, id);
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

        // GET: Preview/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
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

            // Check if user has edit permission
            var canEdit = item.OwnerId == userId || await _storageService.CanUserEditItemAsync(id, userId);
            if (!canEdit)
            {
                TempData["Error"] = "You don't have permission to edit this file.";
                return RedirectToAction("Index", "Storage", new { folderId = item.ParentFolderId });
            }

            // Check if file is editable
            if (item.Type != StorageItemType.File || !_fileStorageService.IsEditableTextFile(item.Name))
            {
                TempData["Error"] = "This file type cannot be edited in the browser.";
                return RedirectToAction("Index", "Storage", new { folderId = item.ParentFolderId });
            }

            try
            {
                var content = await _fileStorageService.GetFileContentAsync(item.FilePath);
                ViewBag.Item = item;
                ViewBag.Content = content;
                ViewBag.Language = GetEditorLanguage(item.Name);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading file content for editing: {ItemId}", id);
                TempData["Error"] = "Error loading file content.";
                return RedirectToAction("Index", "Storage", new { folderId = item.ParentFolderId });
            }
        }

        // POST: Preview/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string content)
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

            // Check if user has edit permission
            var canEdit = item.OwnerId == userId || await _storageService.CanUserEditItemAsync(id, userId);
            if (!canEdit)
            {
                return Json(new { success = false, message = "You don't have permission to edit this file." });
            }

            // Check if file is editable
            if (item.Type != StorageItemType.File || !_fileStorageService.IsEditableTextFile(item.Name))
            {
                return Json(new { success = false, message = "This file type cannot be edited." });
            }

            try
            {
                var saved = await _fileStorageService.SaveFileContentAsync(item.FilePath, content ?? string.Empty);
                if (saved)
                {
                    // Update modified date
                    item.ModifiedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // Log activity
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                    await _activityService.LogActivityAsync(
                        item.Id,
                        userId,
                        ActivityType.FileEdited,
                        $"Edited file: {item.Name}",
                        ipAddress,
                        userAgent
                    );

                    _logger.LogInformation("User {UserId} edited file {ItemId}", userId, id);
                    return Json(new { success = true, message = "File saved successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to save file." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file content: {ItemId}", id);
                return Json(new { success = false, message = "An error occurred while saving the file." });
            }
        }

        private string GetEditorLanguage(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".js" or ".jsx" => "javascript",
                ".ts" or ".tsx" => "typescript",
                ".json" => "json",
                ".html" or ".htm" => "html",
                ".css" => "css",
                ".md" or ".markdown" => "markdown",
                ".py" => "python",
                ".java" => "java",
                ".cs" => "csharp",
                ".cpp" or ".c" or ".h" or ".hpp" => "cpp",
                ".php" => "php",
                ".rb" => "ruby",
                ".go" => "go",
                ".rs" => "rust",
                ".swift" => "swift",
                ".kt" => "kotlin",
                ".sql" => "sql",
                ".xml" => "xml",
                ".yaml" or ".yml" => "yaml",
                ".sh" => "shell",
                ".ps1" => "powershell",
                ".bat" => "bat",
                _ => "plaintext"
            };
        }

        // GET: Preview/Video/5
        public async Task<IActionResult> Video(int id)
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

            // Check if user has access to this file (owner or shared with them)
            var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            // Verify it's a video file
            var extension = Path.GetExtension(item.Name).ToLowerInvariant();
            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".m4v" };
            if (!videoExtensions.Contains(extension))
            {
                TempData["Error"] = "This file is not a video.";
                return RedirectToAction("Index", "Storage", new { folderId = item.ParentFolderId });
            }

            // Log activity
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            await _activityService.LogActivityAsync(
                item.Id,
                userId,
                ActivityType.FileViewed,
                $"Viewed video: {item.Name}",
                ipAddress,
                userAgent
            );

            ViewBag.Item = item;
            return View();
        }

        // GET: Preview/GetVideoStream/5
        [HttpGet]
        public async Task<IActionResult> GetVideoStream(int id)
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

            // Check if user has access to this file
            var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            try
            {
                // Build the full file path
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                var fullPath = Path.Combine(uploadsPath, item.FilePath);
                
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogError("Video file not found: {FilePath}", fullPath);
                    return NotFound();
                }
                
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var mimeType = GetVideoMimeType(item.Name);
                
                // Enable range requests for video seeking
                return File(fileStream, mimeType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming video file {ItemId}", id);
                return NotFound();
            }
        }

        private string GetVideoMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".mp4" or ".m4v" => "video/mp4",
                ".webm" => "video/webm",
                ".ogg" => "video/ogg",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                _ => "application/octet-stream"
            };
        }
    }
}
