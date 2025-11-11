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

        public StorageController(
            IStorageService storageService,
            IFileStorageService fileStorageService,
            ISharingService sharingService,
            UserManager<ApplicationUser> userManager,
            ILogger<StorageController> logger)
        {
            _storageService = storageService;
            _fileStorageService = fileStorageService;
            _sharingService = sharingService;
            _userManager = userManager;
            _logger = logger;
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
                    model.ParentFolderId,
                    model.IsPublic,
                    model.Description);

                TempData["SuccessMessage"] = "File uploaded successfully!";
                return RedirectToAction("Index", new { folderId = model.ParentFolderId });
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

                // Delete physical file if it's a file
                if (item.Type == StorageItemType.File && !string.IsNullOrEmpty(item.FilePath))
                {
                    await _fileStorageService.DeleteFileAsync(item.FilePath);
                }

                await _storageService.DeleteItemAsync(id, userId);
                TempData["SuccessMessage"] = $"{(item.Type == StorageItemType.File ? "File" : "Folder")} deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {ItemId} for user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while deleting the item.";
            }

            return RedirectToAction("Index", new { folderId = currentFolderId });
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
    }
}