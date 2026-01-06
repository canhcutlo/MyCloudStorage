using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudStorage.Services
{
    public interface IActivityService
    {
        Task LogActivityAsync(int? storageItemId, string userId, ActivityType activityType, string description, string? ipAddress = null, string? userAgent = null, string? additionalData = null);
        Task<List<ActivityLog>> GetRecentActivitiesAsync(string userId, int count = 50);
        Task<List<ActivityLog>> GetFileActivitiesAsync(int storageItemId, int count = 100);
        Task<List<ActivityLog>> GetAllActivitiesAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, ActivityType? activityType = null, int pageNumber = 1, int pageSize = 50);
        Task<int> GetActivityCountAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
    }

    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;

        public ActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(int? storageItemId, string userId, ActivityType activityType, string description, string? ipAddress = null, string? userAgent = null, string? additionalData = null)
        {
            var activity = new ActivityLog
            {
                StorageItemId = storageItemId,
                UserId = userId,
                ActivityType = activityType,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                AdditionalData = additionalData
            };

            _context.ActivityLogs.Add(activity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetRecentActivitiesAsync(string userId, int count = 50)
        {
            // Get activities for files owned by the user or shared with them
            var ownedFileIds = await _context.StorageItems
                .Where(s => s.OwnerId == userId && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            var sharedFileIds = await _context.SharedItems
                .Where(s => s.SharedWithUserId == userId)
                .Select(s => s.StorageItemId)
                .ToListAsync();

            var fileIds = ownedFileIds.Union(sharedFileIds).ToList();

            return await _context.ActivityLogs
                .Include(a => a.User)
                .Include(a => a.StorageItem)
                .Where(a => a.StorageItemId.HasValue && fileIds.Contains(a.StorageItemId.Value))
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetFileActivitiesAsync(int storageItemId, int count = 100)
        {
            return await _context.ActivityLogs
                .Include(a => a.User)
                .Include(a => a.StorageItem)
                .Where(a => a.StorageItemId == storageItemId)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetAllActivitiesAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, ActivityType? activityType = null, int pageNumber = 1, int pageSize = 50)
        {
            // Get activities for files owned by the user or shared with them
            var ownedFileIds = await _context.StorageItems
                .Where(s => s.OwnerId == userId && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            var sharedFileIds = await _context.SharedItems
                .Where(s => s.SharedWithUserId == userId)
                .Select(s => s.StorageItemId)
                .ToListAsync();

            var fileIds = ownedFileIds.Union(sharedFileIds).ToList();

            var query = _context.ActivityLogs
                .Include(a => a.User)
                .Include(a => a.StorageItem)
                .Where(a => a.StorageItemId.HasValue && fileIds.Contains(a.StorageItemId.Value));

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            if (activityType.HasValue)
                query = query.Where(a => a.ActivityType == activityType.Value);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetActivityCountAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var ownedFileIds = await _context.StorageItems
                .Where(s => s.OwnerId == userId && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            var sharedFileIds = await _context.SharedItems
                .Where(s => s.SharedWithUserId == userId)
                .Select(s => s.StorageItemId)
                .ToListAsync();

            var fileIds = ownedFileIds.Union(sharedFileIds).ToList();

            var query = _context.ActivityLogs
                .Where(a => a.StorageItemId.HasValue && fileIds.Contains(a.StorageItemId.Value));

            if (fromDate.HasValue)
                query = query.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.Timestamp <= toDate.Value);

            return await query.CountAsync();
        }
    }
}
