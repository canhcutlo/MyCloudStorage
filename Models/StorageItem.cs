using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudStorage.Models
{
    public class StorageItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public StorageItemType Type { get; set; }

        public long Size { get; set; } = 0; // Size in bytes

        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty; // Physical file path

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        [MaxLength(32)]
        public string FileHash { get; set; } = string.Empty; // MD5 hash for file integrity

        public int? ParentFolderId { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        public bool IsPublic { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser Owner { get; set; } = null!;

        [ForeignKey("ParentFolderId")]
        public virtual StorageItem? ParentFolder { get; set; }

        public virtual ICollection<StorageItem> SubItems { get; set; } = new List<StorageItem>();
        public virtual ICollection<SharedItem> Shares { get; set; } = new List<SharedItem>();
    }

    public enum StorageItemType
    {
        File = 1,
        Folder = 2
    }
}