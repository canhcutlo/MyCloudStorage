using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudStorage.Models
{
    public enum ActivityType
    {
        FileUploaded,
        FileDownloaded,
        FileViewed,
        FileEdited,
        FileModified,
        FileDeleted,
        FileRestored,
        FileMoved,
        FileRenamed,
        FileShared,
        FileUnshared,
        FolderCreated,
        FolderDeleted,
        FolderRenamed,
        CommentAdded,
        CommentEdited,
        CommentDeleted,
        PermissionChanged,
        FileAddedToFavorites,
        FileRemovedFromFavorites
    }

    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int? StorageItemId { get; set; }

        [ForeignKey("StorageItemId")]
        public StorageItem? StorageItem { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public ActivityType ActivityType { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        // Additional context (e.g., shared with user email, old/new filename)
        [MaxLength(1000)]
        public string? AdditionalData { get; set; }
    }
}
