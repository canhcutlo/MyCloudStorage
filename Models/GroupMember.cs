using System.ComponentModel.DataAnnotations;

namespace CloudStorage.Models
{
    public class GroupMember
    {
        public int Id { get; set; }

        public int GroupId { get; set; }
        public Group? Group { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; set; }

        // Optional: If the email belongs to a registered user
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Indicates whether this contact was auto-added from sharing history
        public bool IsFromSharingHistory { get; set; } = false;
    }
}
