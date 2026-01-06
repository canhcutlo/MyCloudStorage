using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.Models;
using CloudStorage.Models.ViewModels;
using CloudStorage.Services;
using CloudStorage.Data;
using System.IO.Compression;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class StorageController : Controller
    {
        private readonly IStorageService _storageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ISharingService _sharingService;
        private readonly ICommentService _commentService;
        private readonly IActivityService _activityService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StorageController> _logger;
        private readonly GeminiAIService _aiService;
        private readonly IFileVersionService _versionService;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public StorageController(
            IStorageService storageService,
            IFileStorageService fileStorageService,
            ISharingService sharingService,
            ICommentService commentService,
            IActivityService activityService,
            UserManager<ApplicationUser> userManager,
            ILogger<StorageController> logger,
            GeminiAIService aiService,
            IFileVersionService versionService,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _storageService = storageService;
            _fileStorageService = fileStorageService;
            _sharingService = sharingService;
            _commentService = commentService;
            _activityService = activityService;
            _userManager = userManager;
            _logger = logger;
            _aiService = aiService;
            _versionService = versionService;
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? folderId, string sortBy = "name", string sortOrder = "asc")
        {
            var userId = _userManager.GetUserId(User)!;
            
            IEnumerable<StorageItem> items;
            
            // Check if accessing a shared folder
            if (folderId.HasValue)
            {
                var folder = await _storageService.GetItemByIdAsync(folderId.Value);
                if (folder != null && folder.OwnerId != userId)
                {
                    // This is a shared folder - verify user has permission
                    var sharedWithMe = await _sharingService.GetSharedWithMeAsync(userId);
                    var hasAccess = sharedWithMe.Any(s => s.StorageItemId == folderId.Value && s.IsActive);
                    
                    if (!hasAccess)
                    {
                        _logger.LogWarning("User {UserId} attempted to access folder {FolderId} without permission", userId, folderId);
                        return Forbid();
                    }
                    
                    // User has permission - get items owned by the folder owner
                    items = await _storageService.GetUserItemsAsync(folder.OwnerId, folderId, sortBy, sortOrder);
                }
                else
                {
                    // User's own folder
                    items = await _storageService.GetUserItemsAsync(userId, folderId, sortBy, sortOrder);
                }
            }
            else
            {
                // Root level - only show user's own items
                items = await _storageService.GetUserItemsAsync(userId, folderId, sortBy, sortOrder);
            }
            
            var currentFolder = folderId.HasValue ? 
                await _storageService.GetFolderPathAsync(folderId.Value, userId) : null;
            
            var breadcrumbs = await _storageService.GetBreadcrumbPathAsync(folderId, userId);
            var user = await _userManager.FindByIdAsync(userId);

            // Check edit permissions for each item
            var itemEditPermissions = new Dictionary<int, bool>();
            foreach (var item in items)
            {
                itemEditPermissions[item.Id] = item.OwnerId == userId || 
                    await _storageService.CanUserEditItemAsync(item.Id, userId);
            }

            // Check if user can edit current folder (for upload/create actions)
            bool canEditCurrentFolder = true;
            if (folderId.HasValue)
            {
                var folderItem = await _storageService.GetItemByIdAsync(folderId.Value);
                if (folderItem != null && folderItem.OwnerId != userId)
                {
                    canEditCurrentFolder = await _storageService.CanUserEditFolderAsync(folderId.Value, userId);
                }
            }

            // Get favorite statuses for all items
            var itemIds = items.Select(i => i.Id).ToList();
            var favoriteStatuses = await _storageService.GetFavoriteStatusesAsync(itemIds, userId);

            var viewModel = new StorageViewModel
            {
                Items = items,
                CurrentFolder = currentFolder,
                BreadcrumbPath = string.Join(" / ", breadcrumbs.Select(b => b.Name)),
                TotalUsedStorage = user?.UsedStorage ?? 0,
                TotalStorageQuota = user?.StorageQuota ?? 0,
                TotalFiles = items.Count(i => i.Type == StorageItemType.File),
                TotalFolders = items.Count(i => i.Type == StorageItemType.Folder),
                ItemEditPermissions = itemEditPermissions,
                CanEditCurrentFolder = canEditCurrentFolder,
                ItemFavoriteStatuses = favoriteStatuses
            };

            ViewBag.CurrentFolderId = folderId;
            ViewBag.Breadcrumbs = breadcrumbs;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Upload(int? folderId)
        {
            return View(new UploadFileViewModel { ParentFolderId = folderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadFileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Determine the owner of the target folder
            string targetOwnerId = userId;
            bool isUploadingToSharedFolder = false;

            if (model.ParentFolderId.HasValue)
            {
                var targetFolder = await _storageService.GetItemByIdAsync(model.ParentFolderId.Value);
                if (targetFolder != null && targetFolder.OwnerId != userId)
                {
                    // Check if user has edit permission on the shared folder
                    var canEdit = await _storageService.CanUserEditFolderAsync(model.ParentFolderId.Value, userId);
                    if (!canEdit)
                    {
                        return Forbid();
                    }
                    targetOwnerId = targetFolder.OwnerId;
                    isUploadingToSharedFolder = true;
                }
            }

            // Check storage quota (use target folder owner's quota)
            var targetUser = await _userManager.FindByIdAsync(targetOwnerId);
            if (targetUser == null)
            {
                return BadRequest("Target user not found.");
            }

            if (targetUser.UsedStorage + model.File.Length > targetUser.StorageQuota)
            {
                ModelState.AddModelError("File", "File size exceeds the folder owner's storage quota.");
                return View(model);
            }

            // Check if file with same name exists
            var fileExists = isUploadingToSharedFolder
                ? await _storageService.ItemExistsInSharedFolderAsync(model.File.FileName, model.ParentFolderId)
                : await _storageService.ItemExistsAsync(model.File.FileName, model.ParentFolderId, userId);

            if (fileExists)
            {
                ModelState.AddModelError("File", "A file with this name already exists in this location.");
                return View(model);
            }

            try
            {
                // AI Feature 2: Auto-classify file into appropriate folder
                int? targetFolderId = model.ParentFolderId;
                string? categoryName = null;
                
                if (model.AutoClassify && model.ParentFolderId == null)
                {
                    categoryName = await _aiService.ClassifyFileByNameAsync(model.File.FileName);
                    
                    // Find or create category folder
                    var categoryFolder = await _storageService.GetUserItemsAsync(userId, null);
                    var existingFolder = categoryFolder.FirstOrDefault(f => 
                        f.Type == StorageItemType.Folder && f.Name == categoryName);
                    
                    if (existingFolder == null)
                    {
                        existingFolder = await _storageService.CreateFolderAsync(
                            categoryName,
                            $"Auto-created folder for {categoryName}",
                            userId,
                            null,
                            false);
                    }
                    
                    targetFolderId = existingFolder.Id;
                }

                // Save physical file (use target owner ID for storage location)
                var filePath = await _fileStorageService.SaveFileAsync(model.File, targetOwnerId);
                
                // Calculate file hash
                var fileHash = "";
                using (var stream = model.File.OpenReadStream())
                {
                    fileHash = _fileStorageService.CalculateFileHash(stream);
                }

                // Save file record to database (use target owner ID)
                var storageItem = await _storageService.CreateFileAsync(
                    model.File.FileName,
                    filePath,
                    model.File.Length,
                    _fileStorageService.GetMimeType(model.File.FileName),
                    fileHash,
                    targetOwnerId,
                    targetFolderId,
                    model.IsPublic,
                    model.Description);

                // Log activity
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                await _activityService.LogActivityAsync(
                    storageItem.Id,
                    userId,
                    ActivityType.FileUploaded,
                    $"Uploaded file: {model.File.FileName}",
                    ipAddress,
                    userAgent
                );

                TempData["SuccessMessage"] = model.AutoClassify && targetFolderId != model.ParentFolderId
                    ? $"File uploaded and auto-classified to '{categoryName}' folder!"
                    : "File uploaded successfully!";
                return RedirectToAction("Index", new { folderId = targetFolderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} for user {UserId}", model.File.FileName, userId);
                ModelState.AddModelError("", "An error occurred while uploading the file.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Replace(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _storageService.GetItemByIdAsync(id);

            if (item == null || item.OwnerId != userId || item.Type != StorageItemType.File)
            {
                return NotFound();
            }

            return View(new ReplaceFileViewModel
            {
                ItemId = id,
                ItemName = item.Name,
                ParentFolderId = item.ParentFolderId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Replace(ReplaceFileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            var item = await _storageService.GetItemByIdAsync(model.ItemId);

            if (item == null || item.OwnerId != userId || item.Type != StorageItemType.File)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Check storage quota (subtract old file size, add new file size)
            var sizeDifference = model.File.Length - item.Size;
            if (user.UsedStorage + sizeDifference > user.StorageQuota)
            {
                ModelState.AddModelError("File", "File size exceeds your storage quota.");
                return View(model);
            }

            try
            {
                // Create a version of the current file before replacing
                await _versionService.CreateVersionAsync(item, userId, model.ChangeDescription ?? "");

                // Save new physical file
                var oldFilePath = item.FilePath;
                var newFilePath = await _fileStorageService.SaveFileAsync(model.File, userId);

                // Calculate new file hash
                string newFileHash;
                using (var stream = model.File.OpenReadStream())
                {
                    newFileHash = _fileStorageService.CalculateFileHash(stream);
                }

                // Update item metadata
                item.FilePath = newFilePath;
                item.Size = model.File.Length;
                item.FileHash = newFileHash;
                item.MimeType = _fileStorageService.GetMimeType(model.File.FileName);
                item.ModifiedAt = DateTime.UtcNow;

                _context.StorageItems.Update(item);
                await _context.SaveChangesAsync();

                // Update user storage
                user.UsedStorage += sizeDifference;
                await _userManager.UpdateAsync(user);

                // Delete old physical file
                try
                {
                    var oldFileFullPath = Path.Combine(_environment.WebRootPath, "uploads", oldFilePath);
                    if (System.IO.File.Exists(oldFileFullPath))
                    {
                        System.IO.File.Delete(oldFileFullPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old file: {FilePath}", oldFilePath);
                }

                // Log activity
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                await _activityService.LogActivityAsync(
                    item.Id,
                    userId,
                    ActivityType.FileModified,
                    $"Replaced file: {item.Name}",
                    ipAddress,
                    userAgent
                );

                TempData["SuccessMessage"] = "File replaced successfully! Previous version has been saved.";
                return RedirectToAction("Index", new { folderId = item.ParentFolderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing file {ItemId} for user {UserId}", model.ItemId, userId);
                ModelState.AddModelError("", "An error occurred while replacing the file.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult CreateFolder(int? parentFolderId)
        {
            return View(new CreateFolderViewModel { ParentFolderId = parentFolderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder(CreateFolderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;

            // Determine the owner of the target folder
            string targetOwnerId = userId;

            if (model.ParentFolderId.HasValue)
            {
                var targetFolder = await _storageService.GetItemByIdAsync(model.ParentFolderId.Value);
                if (targetFolder != null && targetFolder.OwnerId != userId)
                {
                    // Check if user has edit permission on the shared folder
                    var canEdit = await _storageService.CanUserEditFolderAsync(model.ParentFolderId.Value, userId);
                    if (!canEdit)
                    {
                        return Forbid();
                    }
                    targetOwnerId = targetFolder.OwnerId;
                }
            }

            try
            {
                await _storageService.CreateFolderAsync(
                    model.Name, 
                    model.Description, 
                    targetOwnerId, 
                    model.ParentFolderId, 
                    model.IsPublic);

                TempData["SuccessMessage"] = "Folder created successfully!";
                return RedirectToAction("Index", new { folderId = model.ParentFolderId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder {FolderName} for user {UserId}", model.Name, userId);
                ModelState.AddModelError("", "An error occurred while creating the folder.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Get item by ID first
            var item = await _storageService.GetItemByIdAsync(id);
            
            if (item == null || item.Type != StorageItemType.File)
            {
                return NotFound();
            }
            
            // Check if user has access to this file (owner or shared with them)
            var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
            if (!hasAccess)
            {
                return Forbid();
            }

            try
            {
                var fileBytes = await _fileStorageService.GetFileAsync(item.FilePath);
                
                // Log activity
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                await _activityService.LogActivityAsync(
                    item.Id,
                    userId,
                    ActivityType.FileDownloaded,
                    $"Downloaded file: {item.Name}",
                    ipAddress,
                    userAgent
                );
                
                return File(fileBytes, item.MimeType, item.Name);
            }
            catch (FileNotFoundException)
            {
                TempData["ErrorMessage"] = "File not found on storage.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId} for user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while downloading the file.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? currentFolderId)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // First, try to get item as owner
                var item = await _storageService.GetItemAsync(id, userId);
                
                // If not owner, check if user has edit permission on shared item
                if (item == null)
                {
                    var sharedItem = await _storageService.GetItemByIdAsync(id);
                    if (sharedItem != null)
                    {
                        var canEdit = await _storageService.CanUserEditItemAsync(id, userId);
                        if (!canEdit)
                        {
                            return Forbid();
                        }
                        item = sharedItem;
                    }
                }

                if (item == null)
                {
                    return NotFound();
                }

                // Move to trash (soft delete) - physical file kept for 15 days
                await _storageService.DeleteItemAsync(id, item.OwnerId);
                
                // Log activity
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                await _activityService.LogActivityAsync(
                    item.Id,
                    userId,
                    ActivityType.FileDeleted,
                    $"Deleted {item.Type.ToString().ToLower()}: {item.Name}",
                    ipAddress,
                    userAgent
                );
                
                TempData["SuccessMessage"] = $"{(item.Type == StorageItemType.File ? "File" : "Folder")} moved to trash!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId} for user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while deleting the item.";
            }

            return RedirectToAction("Index", new { folderId = currentFolderId });
        }

        [HttpGet]
        public async Task<IActionResult> Trash()
        {
            var userId = _userManager.GetUserId(User)!;
            var deletedItems = await _storageService.GetDeletedItemsAsync(userId);
            return View(deletedItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                var success = await _storageService.RestoreItemAsync(id, userId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Item restored successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to restore item";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring item {ItemId} for user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while restoring the item.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // Get the deleted items to find this one
                var deletedItems = await _storageService.GetDeletedItemsAsync(userId);
                var item = deletedItems.FirstOrDefault(i => i.Id == id);
                
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found in trash";
                    return RedirectToAction(nameof(Trash));
                }

                // Delete physical file if it's a file type
                if (item.Type == StorageItemType.File && !string.IsNullOrEmpty(item.FilePath))
                {
                    await _fileStorageService.DeleteFileAsync(item.FilePath);
                }

                // Permanently remove from database
                var success = await _storageService.PermanentlyDeleteItemAsync(id, userId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Item permanently deleted!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to permanently delete item";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error permanently deleting item {ItemId} for user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while permanently deleting the item.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                var deletedItems = await _storageService.GetDeletedItemsAsync(userId);
                int count = 0;

                foreach (var item in deletedItems)
                {
                    // Delete physical file if it exists
                    if (item.Type == StorageItemType.File && !string.IsNullOrEmpty(item.FilePath))
                    {
                        await _fileStorageService.DeleteFileAsync(item.FilePath);
                    }

                    // Permanently delete from database
                    await _storageService.PermanentlyDeleteItemAsync(item.Id, userId);
                    count++;
                }

                TempData["SuccessMessage"] = $"Trash emptied: {count} item(s) permanently deleted!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error emptying trash for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while emptying trash.";
            }

            return RedirectToAction(nameof(Trash));
        }

        [HttpGet]
        public async Task<IActionResult> Rename(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _storageService.GetItemAsync(id, userId);

            // If not owner, check if user has edit permission on shared item
            if (item == null)
            {
                var sharedItem = await _storageService.GetItemByIdAsync(id);
                if (sharedItem != null)
                {
                    var canEdit = await _storageService.CanUserEditItemAsync(id, userId);
                    if (!canEdit)
                    {
                        return Forbid();
                    }
                    item = sharedItem;
                }
            }

            if (item == null)
            {
                return NotFound();
            }

            var model = new RenameItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(RenameItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;

            try
            {
                // First, try to get item as owner
                var item = await _storageService.GetItemAsync(model.Id, userId);
                
                // If not owner, check if user has edit permission on shared item
                string targetOwnerId = userId;
                if (item == null)
                {
                    var sharedItem = await _storageService.GetItemByIdAsync(model.Id);
                    if (sharedItem != null)
                    {
                        var canEdit = await _storageService.CanUserEditItemAsync(model.Id, userId);
                        if (!canEdit)
                        {
                            return Forbid();
                        }
                        targetOwnerId = sharedItem.OwnerId;
                    }
                    else
                    {
                        return NotFound();
                    }
                }

                await _storageService.RenameItemAsync(model.Id, model.Name, model.Description, targetOwnerId);
                TempData["SuccessMessage"] = "Item renamed successfully!";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming item {ItemId} for user {UserId}", model.Id, userId);
                ModelState.AddModelError("", "An error occurred while renaming the item.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, StorageItemType? itemType)
        {
            var viewModel = new SearchViewModel { Query = query ?? "", ItemType = itemType };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var userId = _userManager.GetUserId(User)!;
                var results = await _storageService.SearchItemsAsync(userId, query, itemType);
                
                viewModel.Results = results;
                viewModel.TotalResults = results.Count();
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SharedItems()
        {
            var userId = _userManager.GetUserId(User)!;
            
            var myShares = await _sharingService.GetMySharesAsync(userId);
            var sharedWithMe = await _sharingService.GetSharedWithMeAsync(userId);

            var viewModel = new SharedItemsViewModel
            {
                MyShares = myShares,
                SharedWithMe = sharedWithMe
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditShare(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var share = await _sharingService.GetShareByIdAsync(id);

            if (share == null || share.SharedByUserId != userId)
            {
                return NotFound();
            }

            var model = new EditShareViewModel
            {
                Id = share.Id,
                ItemName = share.StorageItem.Name,
                IsPublicLink = share.SharedWithUserId == null && share.SharedWithEmail == null,
                SharedWithEmail = share.SharedWithEmail ?? share.SharedWithUser?.Email,
                Permission = share.Permission,
                ExpiresAt = share.ExpiresAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShare(EditShareViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;

            try
            {
                var share = await _sharingService.GetShareByIdAsync(model.Id);
                
                if (share == null || share.SharedByUserId != userId)
                {
                    return NotFound();
                }

                await _sharingService.UpdateShareAsync(model.Id, model.Permission, model.ExpiresAt);

                TempData["SuccessMessage"] = "Share updated successfully!";
                return RedirectToAction("SharedItems");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share {ShareId} for user {UserId}", model.Id, userId);
                ModelState.AddModelError("", "An error occurred while updating the share.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShare(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                var share = await _sharingService.GetShareByIdAsync(id);
                
                if (share == null || share.SharedByUserId != userId)
                {
                    return NotFound();
                }

                await _sharingService.DeleteShareAsync(id);

                TempData["SuccessMessage"] = "Share removed successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share {ShareId} for user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while removing the share.";
            }

            return RedirectToAction("SharedItems");
        }

        [HttpGet]
        public async Task<IActionResult> Share(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _storageService.GetItemAsync(id, userId);

            if (item == null)
            {
                return NotFound();
            }

            var model = new ShareItemViewModel
            {
                ItemId = item.Id,
                ItemName = item.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(ShareItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;

            try
            {
                SharedItem share;

                if (model.CreatePublicLink)
                {
                    share = await _sharingService.CreatePublicLinkAsync(
                        model.ItemId, userId, model.Permission, model.ExpiresAt);
                    
                    var publicUrl = Url.Action("PublicShare", "Share", 
                        new { token = share.AccessToken }, Request.Scheme);
                    
                    TempData["PublicShareUrl"] = publicUrl;
                    TempData["SuccessMessage"] = "Public link created successfully!";
                }
                else if (!string.IsNullOrEmpty(model.ShareWithEmail))
                {
                    share = await _sharingService.ShareItemAsync(
                        model.ItemId, userId, model.ShareWithEmail, model.Permission, model.ExpiresAt);
                    
                    TempData["SuccessMessage"] = $"Item shared successfully with {model.ShareWithEmail}!";
                }
                else
                {
                    ModelState.AddModelError("", "Please provide an email address or create a public link.");
                    return View(model);
                }

                return RedirectToAction("SharedItems");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing item {ItemId} for user {UserId}", model.ItemId, userId);
                ModelState.AddModelError("", "An error occurred while sharing the item.");
                return View(model);
            }
        }

        // Share with group
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShareWithGroup(int itemId, int groupId, SharePermission permission, DateTime? expiresAt = null, bool allowDownload = true, bool notify = true, string? message = null)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                var (successCount, failedCount, failedEmails) = await _sharingService.ShareWithGroupAsync(
                    itemId, userId, groupId, permission, expiresAt, allowDownload, notify, message);

                return Json(new
                {
                    success = true,
                    successCount,
                    failedCount,
                    failedEmails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing item {ItemId} with group {GroupId} for user {UserId}", itemId, groupId, userId);
                return Json(new { success = false, message = "An error occurred while sharing with the group." });
            }
        }

        // AI Feature 1: Create folder and files from AI prompt
        [HttpGet]
        public IActionResult AICreateFolder(int? parentFolderId)
        {
            return View(new AICreateFolderViewModel { ParentFolderId = parentFolderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AICreateFolder(AICreateFolderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;

            try
            {
                // Parse prompt using AI
                var instruction = await _aiService.ParseFolderCreationPromptAsync(model.Prompt);

                // Ensure unique folder name
                var folderName = instruction.FolderName;
                var counter = 1;
                while (await _storageService.ItemExistsAsync(folderName, model.ParentFolderId, userId))
                {
                    folderName = $"{instruction.FolderName}_{counter}";
                    counter++;
                }

                // Create folder
                var folder = await _storageService.CreateFolderAsync(
                    folderName,
                    $"Created by AI from prompt: {model.Prompt}",
                    userId,
                    model.ParentFolderId,
                    false);

                // Create files inside folder
                foreach (var fileInstruction in instruction.Files)
                {
                    var fileName = fileInstruction.FileName;
                    var content = fileInstruction.Content;

                    // Create a temporary file with content
                    var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
                    await System.IO.File.WriteAllTextAsync(tempFilePath, content);

                    // Save to storage
                    var filePath = await _fileStorageService.SaveFileFromPathAsync(tempFilePath, userId);
                    
                    var fileInfo = new FileInfo(tempFilePath);
                    var fileHash = "";
                    using (var stream = System.IO.File.OpenRead(tempFilePath))
                    {
                        fileHash = _fileStorageService.CalculateFileHash(stream);
                    }

                    // Save file record
                    await _storageService.CreateFileAsync(
                        fileName,
                        filePath,
                        fileInfo.Length,
                        _fileStorageService.GetMimeType(fileName),
                        fileHash,
                        userId,
                        folder.Id,
                        false,
                        "Created by AI");

                    // Clean up temp file
                    System.IO.File.Delete(tempFilePath);
                }

                TempData["SuccessMessage"] = $"✨ AI created folder '{folderName}' with {instruction.Files.Count} file(s) successfully!";
                return RedirectToAction("Index", new { folderId = folder.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder with AI for user {UserId}: {Message}", userId, ex.Message);
                
                if (ex.Message.Contains("quota") || ex.Message.Contains("API"))
                {
                    ModelState.AddModelError("", $"🚫 AI Service Error: {ex.Message}");
                }
                else if (ex.Message.Contains("failed to understand") || ex.Message.Contains("Failed to process"))
                {
                    ModelState.AddModelError("", $"❌ {ex.Message}");
                }
                else
                {
                    ModelState.AddModelError("", "An error occurred while creating the folder with AI. Please try again.");
                }
                
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id, int? currentFolderId)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                // First check if the item exists
                var item = await _storageService.GetItemByIdAsync(id);
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction("Index", new { folderId = currentFolderId });
                }

                // Check if user has access to the item
                var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "You don't have permission to favorite this item.";
                    return RedirectToAction("Index", new { folderId = currentFolderId });
                }

                var success = await _storageService.ToggleFavoriteAsync(id, userId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Favorite updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update favorite";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for item {ItemId} by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while updating favorite.";
            }

            return RedirectToAction("Index", new { folderId = currentFolderId });
        }

        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = _userManager.GetUserId(User)!;
            var favorites = await _storageService.GetFavoritesAsync(userId);

            // Get favorite statuses (all should be true, but for consistency)
            var itemIds = favorites.Select(i => i.Id).ToList();
            var favoriteStatuses = await _storageService.GetFavoriteStatusesAsync(itemIds, userId);

            // Get edit permissions for each item
            var itemEditPermissions = new Dictionary<int, bool>();
            foreach (var item in favorites)
            {
                itemEditPermissions[item.Id] = item.OwnerId == userId || 
                    await _storageService.CanUserEditItemAsync(item.Id, userId);
            }

            var viewModel = new StorageViewModel
            {
                Items = favorites,
                TotalFiles = favorites.Count(i => i.Type == StorageItemType.File),
                TotalFolders = favorites.Count(i => i.Type == StorageItemType.Folder),
                ItemEditPermissions = itemEditPermissions,
                ItemFavoriteStatuses = favoriteStatuses
            };

            return View(viewModel);
        }

        // Comment Actions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int itemId, string commentText, int? parentCommentId = null)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                if (string.IsNullOrWhiteSpace(commentText))
                {
                    return Json(new { success = false, message = "Comment text cannot be empty." });
                }

                var comment = await _commentService.AddCommentAsync(itemId, userId, commentText.Trim(), parentCommentId);

                return Json(new 
                { 
                    success = true, 
                    message = "Comment added successfully!",
                    comment = new
                    {
                        id = comment.Id,
                        text = comment.Text,
                        userName = $"{comment.User.FirstName} {comment.User.LastName}",
                        userId = comment.UserId,
                        createdAt = comment.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        isOwner = comment.UserId == userId
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to item {ItemId}", itemId);
                return Json(new { success = false, message = "An error occurred while adding the comment." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComment(int commentId, string commentText)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                if (string.IsNullOrWhiteSpace(commentText))
                {
                    return Json(new { success = false, message = "Comment text cannot be empty." });
                }

                var updated = await _commentService.UpdateCommentAsync(commentId, userId, commentText.Trim());

                if (updated)
                {
                    return Json(new { success = true, message = "Comment updated successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Comment not found." });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
                return Json(new { success = false, message = "An error occurred while updating the comment." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                var deleted = await _commentService.DeleteCommentAsync(commentId, userId);

                if (deleted)
                {
                    return Json(new { success = true, message = "Comment deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Comment not found." });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return Json(new { success = false, message = "An error occurred while deleting the comment." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(int itemId)
        {
            var userId = _userManager.GetUserId(User)!;

            try
            {
                var hasAccess = await _commentService.CanUserAccessCommentsAsync(itemId, userId);
                if (!hasAccess)
                {
                    return Json(new { success = false, message = "You don't have access to view comments." });
                }

                var comments = await _commentService.GetFileCommentsAsync(itemId);

                var commentList = comments.Select(c => new
                {
                    id = c.Id,
                    text = c.Text,
                    userName = $"{c.User.FirstName} {c.User.LastName}",
                    userId = c.UserId,
                    createdAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    modifiedAt = c.ModifiedAt?.ToString("yyyy-MM-dd HH:mm"),
                    isOwner = c.UserId == userId,
                    replies = c.Replies.Select(r => new
                    {
                        id = r.Id,
                        text = r.Text,
                        userName = $"{r.User.FirstName} {r.User.LastName}",
                        userId = r.UserId,
                        createdAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        modifiedAt = r.ModifiedAt?.ToString("yyyy-MM-dd HH:mm"),
                        isOwner = r.UserId == userId
                    }).ToList()
                }).ToList();

                return Json(new { success = true, comments = commentList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for item {ItemId}", itemId);
                return Json(new { success = false, message = "An error occurred while loading comments." });
            }
        }


    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
            {
                return Json(new { success = false, message = "No items selected." });
            }

            var userId = _userManager.GetUserId(User)!;
            var deletedCount = 0;
            var errors = new List<string>();

            foreach (var itemId in itemIds)
            {
                try
                {
                    var success = await _storageService.DeleteItemAsync(itemId, userId);
                    if (success)
                    {
                        deletedCount++;
                    }
                    else
                    {
                        errors.Add($"Failed to delete item {itemId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting item {ItemId}", itemId);
                    errors.Add($"Error deleting item {itemId}");
                }
            }

            if (deletedCount > 0)
            {
                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} item(s).";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }

            return Json(new { success = deletedCount > 0, deletedCount, errors });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkMove(List<int> itemIds, int? targetFolderId)
        {
            if (itemIds == null || !itemIds.Any())
            {
                return Json(new { success = false, message = "No items selected." });
            }

            var userId = _userManager.GetUserId(User)!;
            var movedCount = 0;
            var errors = new List<string>();

            foreach (var itemId in itemIds)
            {
                try
                {
                    var success = await _storageService.MoveItemAsync(itemId, targetFolderId, userId);
                    if (success)
                    {
                        movedCount++;
                    }
                    else
                    {
                        errors.Add($"Failed to move item {itemId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error moving item {ItemId}", itemId);
                    errors.Add($"Error deleting item {itemId}");
                }
            }

            if (movedCount > 0)
            {
                TempData["SuccessMessage"] = $"Successfully moved {movedCount} item(s).";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }

            return Json(new { success = movedCount > 0, movedCount, errors });
        }

        [HttpPost]
        public async Task<IActionResult> BulkDownload(List<int> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
            {
                return BadRequest("No items selected.");
            }

            var userId = _userManager.GetUserId(User)!;

            try
            {
                if (itemIds.Count == 1)
                {
                    return await Download(itemIds[0]);
                }

                var zipFileName = $"download_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
                var zipPath = Path.Combine(Path.GetTempPath(), zipFileName);

                using (var zipArchive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var itemId in itemIds)
                    {
                        var item = await _storageService.GetItemAsync(itemId, userId);
                        if (item == null || item.Type != StorageItemType.File)
                            continue;

                        var filePath = Path.Combine(_environment.WebRootPath, "uploads", item.FilePath);
                        if (System.IO.File.Exists(filePath))
                        {
                            zipArchive.CreateEntryFromFile(filePath, item.Name);
                        }
                    }
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                System.IO.File.Delete(zipPath);

                return File(memory, "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk download");
                return BadRequest("An error occurred while creating the download.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadMultiple(List<IFormFile> files, int? folderId)
        {
            if (files == null || !files.Any())
            {
                return Json(new { success = false, message = "No files provided." });
            }

            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var uploadedCount = 0;
            var errors = new List<string>();
            var uploadedFiles = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    if (user.UsedStorage + file.Length > user.StorageQuota)
                    {
                        errors.Add($"{file.FileName}: Storage quota exceeded");
                        continue;
                    }

                    var fileExists = await _storageService.ItemExistsAsync(file.FileName, folderId, userId);
                    if (fileExists)
                    {
                        errors.Add($"{file.FileName}: File already exists");
                        continue;
                    }

                    var filePath = await _fileStorageService.SaveFileAsync(file, userId);

                    string fileHash;
                    using (var stream = file.OpenReadStream())
                    {
                        fileHash = _fileStorageService.CalculateFileHash(stream);
                    }

                    var storageItem = await _storageService.CreateFileAsync(
                        file.FileName,
                        filePath,
                        file.Length,
                        _fileStorageService.GetMimeType(file.FileName),
                        fileHash,
                        userId,
                        folderId,
                        false,
                        ""
                    );

                    user.UsedStorage += file.Length;

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
                    await _activityService.LogActivityAsync(
                        storageItem.Id,
                        userId,
                        ActivityType.FileUploaded,
                        $"Uploaded file: {file.FileName}",
                        ipAddress,
                        userAgent
                    );

                    uploadedCount++;
                    uploadedFiles.Add(new { id = storageItem.Id, name = file.FileName });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
                    errors.Add($"{file.FileName}: Upload failed");
                }
            }

            if (uploadedCount > 0)
            {
                await _userManager.UpdateAsync(user);
            }

            return Json(new
            {
                success = uploadedCount > 0,
                uploadedCount,
                errors,
                files = uploadedFiles
            });
        }
    }
}
