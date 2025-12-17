using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudStorage.Models
{
    public class SharedItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StorageItemId { get; set; }

        [Required]
        public string SharedByUserId { get; set; } = string.Empty;

        public string? SharedWithUserId { get; set; } // Null for public shares

        [MaxLength(255)]
        public string? SharedWithEmail { get; set; } // For sharing with non-users

        [Required]
        public SharePermission Permission { get; set; }

        [MaxLength(100)]
        public string? AccessToken { get; set; } // For link-based sharing

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;
        
        // Google Drive-like features
        public bool AllowDownload { get; set; } = true; // Can viewers/commenters download
        public bool Notify { get; set; } = true; // Send email notification when sharing
        public bool NotificationSent { get; set; } = false; // Track if notification was sent
        public DateTime? LastAccessedAt { get; set; } // Track when share was last accessed
        public int AccessCount { get; set; } = 0; // Number of times accessed
        public string? Message { get; set; } // Optional message when sharing

        // Navigation properties
        [ForeignKey("StorageItemId")]
        public virtual StorageItem StorageItem { get; set; } = null!;

        [ForeignKey("SharedByUserId")]
        public virtual ApplicationUser SharedByUser { get; set; } = null!;

        [ForeignKey("SharedWithUserId")]
        public virtual ApplicationUser? SharedWithUser { get; set; }
    }

    public enum SharePermission
    {
        Viewer = 1,      // Can only view (like Google Drive Viewer)
        Commenter = 2,   // Can view and comment (future feature)
        Editor = 3,      // Can view, comment, and edit
        Owner = 4        // Full access (rarely used for shares)
    }
}