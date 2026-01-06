using CloudStorage.Data;
using CloudStorage.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudStorage.Services
{
    public class GroupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GroupService> _logger;

        public GroupService(ApplicationDbContext context, ILogger<GroupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get all groups for a user
        public async Task<List<Group>> GetUserGroupsAsync(string userId)
        {
            return await _context.Groups
                .Include(g => g.Members)
                .Where(g => g.OwnerId == userId)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        // Get a specific group by ID
        public async Task<Group?> GetGroupByIdAsync(int groupId, string userId)
        {
            return await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == userId);
        }

        // Create a new group
        public async Task<(bool Success, string Message, Group? Group)> CreateGroupAsync(string name, string? description, string userId)
        {
            try
            {
                // Check if group name already exists for this user
                var exists = await _context.Groups
                    .AnyAsync(g => g.OwnerId == userId && g.Name == name);

                if (exists)
                {
                    return (false, "A group with this name already exists.", null);
                }

                var group = new Group
                {
                    Name = name,
                    Description = description,
                    OwnerId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created group {GroupId}: {GroupName}", userId, group.Id, group.Name);

                return (true, "Group created successfully.", group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group for user {UserId}", userId);
                return (false, "An error occurred while creating the group.", null);
            }
        }

        // Update group details
        public async Task<(bool Success, string Message)> UpdateGroupAsync(int groupId, string name, string? description, string userId)
        {
            try
            {
                var group = await _context.Groups
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == userId);

                if (group == null)
                {
                    return (false, "Group not found.");
                }

                // Check if new name conflicts with another group
                var nameExists = await _context.Groups
                    .AnyAsync(g => g.OwnerId == userId && g.Name == name && g.Id != groupId);

                if (nameExists)
                {
                    return (false, "A group with this name already exists.");
                }

                group.Name = name;
                group.Description = description;
                group.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated group {GroupId}", userId, groupId);

                return (true, "Group updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId} for user {UserId}", groupId, userId);
                return (false, "An error occurred while updating the group.");
            }
        }

        // Delete a group
        public async Task<(bool Success, string Message)> DeleteGroupAsync(int groupId, string userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == userId);

                if (group == null)
                {
                    return (false, "Group not found.");
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted group {GroupId}: {GroupName}", userId, groupId, group.Name);

                return (true, "Group deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId} for user {UserId}", groupId, userId);
                return (false, "An error occurred while deleting the group.");
            }
        }

        // Add a member to a group
        public async Task<(bool Success, string Message)> AddMemberAsync(int groupId, string email, string? displayName, string userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == userId);

                if (group == null)
                {
                    return (false, "Group not found.");
                }

                // Check if member already exists
                if (group.Members.Any(m => m.Email.ToLower() == email.ToLower()))
                {
                    return (false, "This email is already in the group.");
                }

                // Check if email belongs to a registered user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                var member = new GroupMember
                {
                    GroupId = groupId,
                    Email = email,
                    DisplayName = displayName ?? email,
                    UserId = user?.Id,
                    AddedAt = DateTime.UtcNow,
                    IsFromSharingHistory = false
                };

                _context.GroupMembers.Add(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} added member {Email} to group {GroupId}", userId, email, groupId);

                return (true, "Member added successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member {Email} to group {GroupId}", email, groupId);
                return (false, "An error occurred while adding the member.");
            }
        }

        // Remove a member from a group
        public async Task<(bool Success, string Message)> RemoveMemberAsync(int memberId, string userId)
        {
            try
            {
                var member = await _context.GroupMembers
                    .Include(m => m.Group)
                    .FirstOrDefaultAsync(m => m.Id == memberId && m.Group.OwnerId == userId);

                if (member == null)
                {
                    return (false, "Member not found.");
                }

                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} removed member {MemberId} from group {GroupId}", userId, memberId, member.GroupId);

                return (true, "Member removed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member {MemberId}", memberId);
                return (false, "An error occurred while removing the member.");
            }
        }

        // Get all unique contacts from sharing history for a user
        public async Task<List<string>> GetSharingHistoryContactsAsync(string userId)
        {
            // Get all unique emails the user has shared with
            var sharedWithEmails = await _context.SharedItems
                .Where(s => s.SharedByUserId == userId && !string.IsNullOrEmpty(s.SharedWithEmail))
                .Select(s => s.SharedWithEmail!)
                .Distinct()
                .ToListAsync();

            return sharedWithEmails;
        }

        // Auto-add contacts from sharing history to a group
        public async Task<(bool Success, string Message, int AddedCount)> ImportContactsFromHistoryAsync(int groupId, string userId)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == userId);

                if (group == null)
                {
                    return (false, "Group not found.", 0);
                }

                var contacts = await GetSharingHistoryContactsAsync(userId);
                var existingEmails = group.Members.Select(m => m.Email.ToLower()).ToHashSet();

                int addedCount = 0;

                foreach (var email in contacts)
                {
                    if (!existingEmails.Contains(email.ToLower()))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                        var member = new GroupMember
                        {
                            GroupId = groupId,
                            Email = email,
                            DisplayName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : email,
                            UserId = user?.Id,
                            AddedAt = DateTime.UtcNow,
                            IsFromSharingHistory = true
                        };

                        _context.GroupMembers.Add(member);
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("User {UserId} imported {Count} contacts from sharing history to group {GroupId}", userId, addedCount, groupId);
                }

                return (true, $"{addedCount} contact(s) imported successfully.", addedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing contacts to group {GroupId}", groupId);
                return (false, "An error occurred while importing contacts.", 0);
            }
        }

        // Get group members' emails for sharing purposes
        public async Task<List<string>> GetGroupMemberEmailsAsync(int groupId, string userId)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.OwnerId == userId);

            if (group == null)
            {
                return new List<string>();
            }

            return group.Members.Select(m => m.Email).ToList();
        }
    }
}
