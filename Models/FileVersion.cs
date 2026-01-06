using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudStorage.Models
{
    public class FileVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StorageItemId { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty; // Physical file path of this version

        [MaxLength(32)]
        public string FileHash { get; set; } = string.Empty; // MD5 hash for file integrity

        public long Size { get; set; } = 0; // Size in bytes

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string ChangeDescription { get; set; } = string.Empty; // Optional description of changes

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("StorageItemId")]
        public virtual StorageItem StorageItem { get; set; } = null!;

        [ForeignKey("CreatedByUserId")]
        public virtual ApplicationUser CreatedBy { get; set; } = null!;
    }
}
