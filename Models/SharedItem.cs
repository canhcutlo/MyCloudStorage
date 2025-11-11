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
        ViewOnly = 1,
        Download = 2,
        Edit = 3,
        FullAccess = 4
    }
}