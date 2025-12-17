using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CloudStorage.Models;

namespace CloudStorage.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<StorageItem> StorageItems { get; set; }
        public DbSet<SharedItem> SharedItems { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure StorageItem relationships
            builder.Entity<StorageItem>()
                .HasOne(s => s.Owner)
                .WithMany(u => u.StorageItems)
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StorageItem>()
                .HasOne(s => s.ParentFolder)
                .WithMany(s => s.SubItems)
                .HasForeignKey(s => s.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure SharedItem relationships
            builder.Entity<SharedItem>()
                .HasOne(s => s.StorageItem)
                .WithMany(i => i.Shares)
                .HasForeignKey(s => s.StorageItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SharedItem>()
                .HasOne(s => s.SharedByUser)
                .WithMany(u => u.SharedItems)
                .HasForeignKey(s => s.SharedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SharedItem>()
                .HasOne(s => s.SharedWithUser)
                .WithMany(u => u.ReceivedShares)
                .HasForeignKey(s => s.SharedWithUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for better performance
            builder.Entity<StorageItem>()
                .HasIndex(s => s.OwnerId);

            builder.Entity<StorageItem>()
                .HasIndex(s => s.ParentFolderId);

            builder.Entity<StorageItem>()
                .HasIndex(s => new { s.Name, s.ParentFolderId, s.OwnerId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0"); // Unique constraint only for non-deleted items

            builder.Entity<SharedItem>()
                .HasIndex(s => s.AccessToken)
                .IsUnique()
                .HasFilter("[AccessToken] IS NOT NULL");

            // Configure PasswordResetToken relationships
            builder.Entity<PasswordResetToken>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PasswordResetToken>()
                .HasIndex(p => p.Token)
                .IsUnique();
        }
    }
}