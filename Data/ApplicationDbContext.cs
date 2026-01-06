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
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<FileVersion> FileVersions { get; set; }

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

            // Configure Favorite relationships
            builder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Favorite>()
                .HasOne(f => f.StorageItem)
                .WithMany()
                .HasForeignKey(f => f.StorageItemId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Favorite>()
                .HasIndex(f => new { f.UserId, f.StorageItemId })
                .IsUnique();

            // Configure Group relationships
            builder.Entity<Group>()
                .HasOne(g => g.Owner)
                .WithMany()
                .HasForeignKey(g => g.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Group>()
                .HasIndex(g => new { g.OwnerId, g.Name })
                .IsUnique();

            // Configure GroupMember relationships
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.GroupId, gm.Email })
                .IsUnique();

            builder.Entity<GroupMember>()
                .HasIndex(gm => gm.Email);

            // Configure Comment relationships
            builder.Entity<Comment>()
                .HasOne(c => c.StorageItem)
                .WithMany()
                .HasForeignKey(c => c.StorageItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ActivityLog relationships
            builder.Entity<ActivityLog>()
                .HasOne(a => a.StorageItem)
                .WithMany()
                .HasForeignKey(a => a.StorageItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ActivityLog>()
                .HasIndex(a => a.Timestamp);

            builder.Entity<ActivityLog>()
                .HasIndex(a => new { a.StorageItemId, a.Timestamp });

            builder.Entity<ActivityLog>()
                .HasIndex(a => new { a.UserId, a.Timestamp });

            // Configure FileVersion relationships
            builder.Entity<FileVersion>()
                .HasOne(fv => fv.StorageItem)
                .WithMany()
                .HasForeignKey(fv => fv.StorageItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FileVersion>()
                .HasOne(fv => fv.CreatedBy)
                .WithMany()
                .HasForeignKey(fv => fv.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascade cycle

            builder.Entity<FileVersion>()
                .HasIndex(fv => new { fv.StorageItemId, fv.VersionNumber })
                .IsUnique();

            builder.Entity<FileVersion>()
                .HasIndex(fv => fv.CreatedAt);
        }
    }
}