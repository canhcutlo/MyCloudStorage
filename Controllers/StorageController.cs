using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.Models;
using CloudStorage.Models.ViewModels;
using CloudStorage.Services;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class StorageController : Controller
    {
        private readonly IStorageService _storageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ISharingService _sharingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StorageController> _logger;
        private readonly GeminiAIService _aiService;

        public StorageController(
            IStorageService storageService,
            IFileStorageService fileStorageService,
            ISharingService sharingService,
            UserManager<ApplicationUser> userManager,
            ILogger<StorageController> logger,
            GeminiAIService aiService)
        {
            _storageService = storageService;
            _fileStorageService = fileStorageService;
            _sharingService = sharingService;
            _userManager = userManager;
            _logger = logger;
            _aiService = aiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? folderId)
        {
            var userId = _userManager.GetUserId(User)!;
            
            var items = await _storageService.GetUserItemsAsync(userId, folderId);
            var currentFolder = folderId.HasValue ? 
                await _storageService.GetFolderPathAsync(folderId.Value, userId) : null;
            
            var breadcrumbs = await _storageService.GetBreadcrumbPathAsync(folderId, userId);
            var user = await _userManager.FindByIdAsync(userId);

            var viewModel = new StorageViewModel
            {
                Items = items,
                CurrentFolder = currentFolder,
                BreadcrumbPath = string.Join(" / ", breadcrumbs.Select(b => b.Name)),
                TotalUsedStorage = user?.UsedStorage ?? 0,
                TotalStorageQuota = user?.StorageQuota ?? 0,
                TotalFiles = items.Count(i => i.Type == StorageItemType.File),
                TotalFolders = items.Count(i => i.Type == StorageItemType.Folder)
            };

            ViewBag.CurrentFolderId = folderId;
            ViewBag.Breadcrumbs = breadcrumbs;

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

            // Check storage quota
            if (user.UsedStorage + model.File.Length > user.StorageQuota)
            {
                ModelState.AddModelError("File", "File size exceeds your storage quota.");
                return View(model);
            }

            // Check if file with same name exists
            var fileExists = await _storageService.ItemExistsAsync(
                model.File.FileName, model.ParentFolderId, userId);

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

                // Save physical file
                var filePath = await _fileStorageService.SaveFileAsync(model.File, userId);
                
                // Calculate file hash
                var fileHash = "";
                using (var stream = model.File.OpenReadStream())
                {
                    fileHash = _fileStorageService.CalculateFileHash(stream);
                }

                // Save file record to database
                var storageItem = await _storageService.CreateFileAsync(
                    model.File.FileName,
                    filePath,
                    model.File.Length,
                    _fileStorageService.GetMimeType(model.File.FileName),
                    fileHash,
                    userId,
                    targetFolderId,
                    model.IsPublic,
                    model.Description);

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

            try
            {
                await _storageService.CreateFolderAsync(
                    model.Name, 
                    model.Description, 
                    userId, 
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
            var item = await _storageService.GetItemAsync(id, userId);

            if (item == null || item.Type != StorageItemType.File)
            {
                return NotFound();
            }

            try
            {
                var fileBytes = await _fileStorageService.GetFileAsync(item.FilePath);
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
                var item = await _storageService.GetItemAsync(id, userId);
                if (item == null)
                {
                    return NotFound();
                }

                // Move to trash (soft delete) - physical file kept for 15 days
                await _storageService.DeleteItemAsync(id, userId);
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
                var item = await _context.StorageItems
                    .FirstOrDefaultAsync(i => i.Id == id && i.OwnerId == userId && i.IsDeleted);
                
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
                await _storageService.RenameItemAsync(model.Id, model.Name, model.Description, userId);
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


    }
}