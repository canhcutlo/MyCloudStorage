using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudStorage.Models;
using CloudStorage.Services;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivityController(IActivityService activityService, UserManager<ApplicationUser> userManager)
        {
            _activityService = activityService;
            _userManager = userManager;
        }

        // GET: /Activity/Index
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, ActivityType? activityType, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var pageSize = 50;
            var activities = await _activityService.GetAllActivitiesAsync(
                user.Id, 
                fromDate, 
                toDate, 
                activityType, 
                page, 
                pageSize
            );

            var totalCount = await _activityService.GetActivityCountAsync(user.Id, fromDate, toDate);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.ActivityType = activityType;
            ViewBag.ActivityTypes = Enum.GetValues(typeof(ActivityType));

            return View(activities);
        }

        // GET: /Activity/Recent
        public async Task<IActionResult> Recent(int count = 20)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var activities = await _activityService.GetRecentActivitiesAsync(user.Id, count);
            return View(activities);
        }

        // GET: /Activity/FileActivity/{id}
        public async Task<IActionResult> FileActivity(int id)
        {
            var activities = await _activityService.GetFileActivitiesAsync(id, 100);
            return View(activities);
        }

        // API: GET /Activity/GetRecentActivities
        [HttpGet]
        public async Task<IActionResult> GetRecentActivities(int count = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var activities = await _activityService.GetRecentActivitiesAsync(user.Id, count);
            
            var result = activities.Select(a => new
            {
                id = a.Id,
                activityType = a.ActivityType.ToString(),
                description = a.Description,
                fileName = a.StorageItem?.Name,
                userName = a.User?.UserName,
                timestamp = a.Timestamp,
                timeAgo = GetTimeAgo(a.Timestamp)
            });

            return Json(result);
        }

        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow - timestamp;

            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            
            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }
    }
}
