using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.Data;
using CloudStorage.Models;
using CloudStorage.Services;
using Microsoft.EntityFrameworkCore;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class VersionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileVersionService _versionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VersionController> _logger;

        public VersionController(
            ApplicationDbContext context,
            IFileVersionService versionService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<VersionController> logger)
        {
            _context = context;
            _versionService = versionService;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _context.StorageItems
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (item == null)
            {
                return NotFound();
            }

            var versions = await _versionService.GetVersionHistoryAsync(id);

            ViewBag.Item = item;
            return View(versions);
        }

        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var version = await _versionService.GetVersionAsync(id);
            if (version == null || version.StorageItem == null)
            {
                return NotFound();
            }

            // Check if the user owns the file
            if (version.StorageItem.OwnerId != userId)
            {
                return Forbid();
            }

            var result = await _versionService.RestoreVersionAsync(id, userId);

            if (result)
            {
                TempData["Success"] = $"Version {version.VersionNumber} has been restored successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to restore the version.";
            }

            return RedirectToAction(nameof(History), new { id = version.StorageItemId });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var userId = _userManager.GetUserId(User);
            var version = await _versionService.GetVersionAsync(id);

            if (version == null || version.StorageItem == null)
            {
                return NotFound();
            }

            // Check if the user owns the file
            if (version.StorageItem.OwnerId != userId)
            {
                return Forbid();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", version.FilePath);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Version file not found: {FilePath}", filePath);
                return NotFound("Version file not found.");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var fileName = $"{Path.GetFileNameWithoutExtension(version.StorageItem.Name)}_v{version.VersionNumber}{Path.GetExtension(version.StorageItem.Name)}";
            return File(memory, version.MimeType, fileName);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var version = await _versionService.GetVersionAsync(id);

            if (version == null || version.StorageItem == null)
            {
                return NotFound();
            }

            // Check if the user owns the file
            if (version.StorageItem.OwnerId != userId)
            {
                return Forbid();
            }

            var storageItemId = version.StorageItemId;
            var result = await _versionService.DeleteVersionAsync(id);

            if (result)
            {
                TempData["Success"] = "Version deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete the version.";
            }

            return RedirectToAction(nameof(History), new { id = storageItemId });
        }
    }
}
