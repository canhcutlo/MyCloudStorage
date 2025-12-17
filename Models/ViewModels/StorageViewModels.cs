using CloudStorage.Models;
using System.ComponentModel.DataAnnotations;

namespace CloudStorage.Models.ViewModels
{
    public class StorageViewModel
    {
        public IEnumerable<StorageItem> Items { get; set; } = new List<StorageItem>();
        public StorageItem? CurrentFolder { get; set; }
        public string BreadcrumbPath { get; set; } = string.Empty;
        public long TotalUsedStorage { get; set; }
        public long TotalStorageQuota { get; set; }
        public int TotalFiles { get; set; }
        public int TotalFolders { get; set; }
    }

    public class UploadFileViewModel
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        public int? ParentFolderId { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = false;

        [Display(Name = "Auto-classify into folder")]
        public bool AutoClassify { get; set; } = true;
    }

    public class CreateFolderViewModel
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public int? ParentFolderId { get; set; }

        public bool IsPublic { get; set; } = false;
    }

    public class RenameItemViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    public class ShareItemViewModel
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        
        [EmailAddress]
        public string? ShareWithEmail { get; set; }

        public SharePermission Permission { get; set; } = SharePermission.Viewer;

        [DataType(DataType.Date)]
        public DateTime? ExpiresAt { get; set; }

        public bool CreatePublicLink { get; set; } = false;
    }

    public class SharedItemsViewModel
    {
        public IEnumerable<SharedItem> MyShares { get; set; } = new List<SharedItem>();
        public IEnumerable<SharedItem> SharedWithMe { get; set; } = new List<SharedItem>();
    }

    public class SearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public StorageItemType? ItemType { get; set; }
        public IEnumerable<StorageItem> Results { get; set; } = new List<StorageItem>();
        public int TotalResults { get; set; }
    }

    // AI Feature ViewModels
    public class AICreateFolderViewModel
    {
        [Required]
        [StringLength(1000, MinimumLength = 10)]
        [Display(Name = "AI Prompt")]
        public string Prompt { get; set; } = string.Empty;

        public int? ParentFolderId { get; set; }
    }

    public class EditShareViewModel
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public bool IsPublicLink { get; set; }
        public string? SharedWithEmail { get; set; }
        
        [Required]
        public SharePermission Permission { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime? ExpiresAt { get; set; }
    }
}