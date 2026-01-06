using CloudStorage.Models;
using CloudStorage.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CloudStorage.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly GroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(
            GroupService groupService,
            UserManager<ApplicationUser> userManager,
            ILogger<GroupsController> logger)
        {
            _groupService = groupService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Groups/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var groups = await _groupService.GetUserGroupsAsync(userId);
            return View(groups);
        }

        // GET: Groups/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var (success, message, group) = await _groupService.CreateGroupAsync(model.Name, model.Description, userId);

            if (success && group != null)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(ManageMembers), new { id = group.Id });
            }

            ModelState.AddModelError("", message);
            return View(model);
        }

        // GET: Groups/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var group = await _groupService.GetGroupByIdAsync(id, userId);
            if (group == null)
            {
                return NotFound();
            }

            var model = new EditGroupViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description
            };

            return View(model);
        }

        // POST: Groups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditGroupViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var (success, message) = await _groupService.UpdateGroupAsync(id, model.Name, model.Description, userId);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", message);
            return View(model);
        }

        // POST: Groups/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var (success, message) = await _groupService.DeleteGroupAsync(id, userId);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Groups/ManageMembers/5
        public async Task<IActionResult> ManageMembers(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var group = await _groupService.GetGroupByIdAsync(id, userId);
            if (group == null)
            {
                return NotFound();
            }

            var contacts = await _groupService.GetSharingHistoryContactsAsync(userId);

            var model = new ManageMembersViewModel
            {
                Group = group,
                SharingHistoryContacts = contacts
            };

            return View(model);
        }

        // POST: Groups/AddMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(AddMemberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var (success, message) = await _groupService.AddMemberAsync(
                model.GroupId,
                model.Email,
                model.DisplayName,
                userId);

            return Json(new { success, message });
        }

        // POST: Groups/RemoveMember
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int memberId, int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var (success, message) = await _groupService.RemoveMemberAsync(memberId, userId);

            return Json(new { success, message });
        }

        // POST: Groups/ImportContacts
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportContacts(int groupId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var (success, message, addedCount) = await _groupService.ImportContactsFromHistoryAsync(groupId, userId);

            return Json(new { success, message, addedCount });
        }

        // GET: Groups/GetGroupMembers/5 - For AJAX calls from share modal
        [HttpGet]
        public async Task<IActionResult> GetGroupMembers(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var emails = await _groupService.GetGroupMemberEmailsAsync(id, userId);

            return Json(new { success = true, emails });
        }

        // GET: Groups/GetUserGroups - Return groups as JSON for share modal
        [HttpGet]
        public async Task<IActionResult> GetUserGroups()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var groups = await _groupService.GetUserGroupsAsync(userId);
            
            var groupList = groups.Select(g => new
            {
                id = g.Id,
                name = g.Name,
                memberCount = g.Members.Count
            }).ToList();

            return Json(new { success = true, groups = groupList });
        }
    }

    // View Models
    public class CreateGroupViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }

    public class EditGroupViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }
    }

    public class AddMemberViewModel
    {
        public int GroupId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
    }

    public class ManageMembersViewModel
    {
        public Group Group { get; set; } = null!;
        public List<string> SharingHistoryContacts { get; set; } = new();
    }
}
