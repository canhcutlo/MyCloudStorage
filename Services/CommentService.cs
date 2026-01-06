using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudStorage.Services
{
    public interface ICommentService
    {
        Task<Comment> AddCommentAsync(int storageItemId, string userId, string text, int? parentCommentId = null);
        Task<bool> UpdateCommentAsync(int commentId, string userId, string text);
        Task<bool> DeleteCommentAsync(int commentId, string userId);
        Task<IEnumerable<Comment>> GetFileCommentsAsync(int storageItemId);
        Task<Comment?> GetCommentByIdAsync(int commentId);
        Task<int> GetFileCommentCountAsync(int storageItemId);
        Task<bool> CanUserAccessCommentsAsync(int storageItemId, string userId);
    }

    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;
        private readonly ILogger<CommentService> _logger;

        public CommentService(
            ApplicationDbContext context,
            IStorageService storageService,
            ILogger<CommentService> logger)
        {
            _context = context;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<Comment> AddCommentAsync(int storageItemId, string userId, string text, int? parentCommentId = null)
        {
            // Verify user has access to the file
            var hasAccess = await _storageService.CanUserAccessItemAsync(storageItemId, userId);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("You don't have access to comment on this file.");
            }

            // Verify parent comment exists if provided
            if (parentCommentId.HasValue)
            {
                var parentComment = await _context.Comments
                    .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value && !c.IsDeleted);
                
                if (parentComment == null || parentComment.StorageItemId != storageItemId)
                {
                    throw new InvalidOperationException("Invalid parent comment.");
                }
            }

            var comment = new Comment
            {
                StorageItemId = storageItemId,
                UserId = userId,
                Text = text,
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Load navigation properties
            await _context.Entry(comment)
                .Reference(c => c.User)
                .LoadAsync();

            _logger.LogInformation("User {UserId} added comment {CommentId} to file {FileId}", 
                userId, comment.Id, storageItemId);

            return comment;
        }

        public async Task<bool> UpdateCommentAsync(int commentId, string userId, string text)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
            {
                return false;
            }

            // Only the comment author can edit
            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only edit your own comments.");
            }

            comment.Text = text;
            comment.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} updated comment {CommentId}", userId, commentId);

            return true;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, string userId)
        {
            var comment = await _context.Comments
                .Include(c => c.StorageItem)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
            {
                return false;
            }

            // User can delete if they are the comment author or file owner
            if (comment.UserId != userId && comment.StorageItem.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("You can only delete your own comments or comments on your files.");
            }

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted comment {CommentId}", userId, commentId);

            return true;
        }

        public async Task<IEnumerable<Comment>> GetFileCommentsAsync(int storageItemId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.User)
                .Where(c => c.StorageItemId == storageItemId && !c.IsDeleted && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment?> GetCommentByIdAsync(int commentId)
        {
            return await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        }

        public async Task<int> GetFileCommentCountAsync(int storageItemId)
        {
            return await _context.Comments
                .CountAsync(c => c.StorageItemId == storageItemId && !c.IsDeleted);
        }

        public async Task<bool> CanUserAccessCommentsAsync(int storageItemId, string userId)
        {
            return await _storageService.CanUserAccessItemAsync(storageItemId, userId);
        }
    }
}
