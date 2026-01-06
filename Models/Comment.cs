using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudStorage.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StorageItemId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        public int? ParentCommentId { get; set; } // For replies

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("StorageItemId")]
        public virtual StorageItem StorageItem { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
