using Microsoft.AspNetCore.Identity;

namespace CloudStorage.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public long StorageQuota { get; set; } = 5_000_000_000; // 5GB in bytes
        public long UsedStorage { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<StorageItem> StorageItems { get; set; } = new List<StorageItem>();
        public virtual ICollection<SharedItem> SharedItems { get; set; } = new List<SharedItem>();
        public virtual ICollection<SharedItem> ReceivedShares { get; set; } = new List<SharedItem>();
    }
}