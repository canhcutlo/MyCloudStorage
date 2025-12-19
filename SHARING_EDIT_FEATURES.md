# Sharing Edit Features Documentation

## Overview
This document describes the editing capabilities added to the sharing feature, allowing users with Editor permissions to modify shared folders and files.

## Features Added

### 1. Permission-Based Editing
Users with **Editor** or **Owner** permissions can now:
- Upload files to shared folders
- Create subfolders in shared folders
- Rename files and folders in shared locations
- Delete files and folders from shared locations

### 2. Permission Checking System

#### New Service Methods (StorageService)

**`CanUserEditItemAsync(int itemId, string userId)`**
- Checks if a user has edit permission on a specific item
- Returns true if:
  - User owns the item
  - Item is shared with user with Editor/Owner permission
  - Parent folder is shared with edit permission

**`CanUserEditFolderAsync(int? folderId, string userId)`**
- Checks if a user can edit within a folder
- Recursively checks parent folders for shared permissions

**`ItemExistsInSharedFolderAsync(string name, int? parentFolderId)`**
- Checks for duplicate file/folder names in shared folders
- Does not require owner match (for shared folder validation)

### 3. Updated Controller Actions

#### Upload Action
- Determines the owner of the target folder
- Validates edit permissions for shared folders
- Uses folder owner's storage quota (not uploader's)
- Files uploaded to shared folders are owned by the folder owner

#### CreateFolder Action
- Checks edit permissions before creating folders
- New folders in shared locations are owned by the parent folder owner

#### Delete Action
- Validates edit permissions before deletion
- Allows editors to delete items from shared folders
- Items are moved to the owner's trash (not the editor's)

#### Rename Action
- Validates edit permissions before renaming
- Allows editors to rename items in shared folders

### 4. UI Updates

#### Storage Index View
- Upload/Create buttons only shown if user can edit current folder
- Edit/Delete buttons only shown for items user can edit
- Visual indicators for shared folders in breadcrumbs
- Permission-level notification (Editor vs Viewer)

#### Permission Display
```csharp
public class StorageViewModel
{
    // ... other properties ...
    public Dictionary<int, bool> ItemEditPermissions { get; set; }
    public bool CanEditCurrentFolder { get; set; }
}
```

### 5. Security Considerations

#### Permission Validation
- All edit operations validate permissions before execution
- Both item-level and folder-level permissions are checked
- Recursive permission checking for nested folders

#### Storage Quota Management
- Files uploaded to shared folders count toward folder owner's quota
- Deleted files reduce folder owner's usage (not editor's)

#### Owner Preservation
- All items maintain their original owner
- Editors cannot change ownership
- Files created in shared folders are owned by folder owner

## Usage Examples

### Sharing a Folder with Edit Permission

1. **Share the folder:**
   ```csharp
   await _sharingService.ShareItemAsync(
       folderId, 
       ownerId, 
       "editor@example.com", 
       SharePermission.Editor,
       expiresAt: DateTime.UtcNow.AddDays(30)
   );
   ```

2. **Editor can now:**
   - Navigate to the shared folder
   - Upload files (stored in owner's space)
   - Create subfolders
   - Rename/delete items
   - Share the folder with others (if permission allows)

### Permission Levels

| Permission | View | Download | Comment | Upload | Delete | Rename |
|------------|------|----------|---------|--------|--------|--------|
| Viewer     | ✓    | ✓        | ✗       | ✗      | ✗      | ✗      |
| Commenter  | ✓    | ✓        | ✓*      | ✗      | ✗      | ✗      |
| Editor     | ✓    | ✓        | ✓*      | ✓      | ✓      | ✓      |
| Owner      | ✓    | ✓        | ✓*      | ✓      | ✓      | ✓      |

*Comment feature not yet implemented

## Testing Checklist

### As Editor:
- [ ] Can upload files to shared folder
- [ ] Can create subfolders in shared folder
- [ ] Can rename files in shared folder
- [ ] Can delete files from shared folder
- [ ] Cannot see edit buttons for non-shared items
- [ ] Files use folder owner's storage quota
- [ ] Deleted items go to owner's trash

### As Viewer:
- [ ] Can view shared folder contents
- [ ] Can download files
- [ ] Cannot see upload/create buttons
- [ ] Cannot see edit/delete buttons
- [ ] See "Read-only" indicator

### As Owner:
- [ ] Can see all items with full permissions
- [ ] Can manage share permissions
- [ ] Can see who has access
- [ ] Storage quota reflects all files (including editor uploads)

## Migration Notes

### Database
No database changes required. Uses existing SharePermission enum values.

### Existing Shares
All existing shares continue to work. Permissions are respected based on SharePermission value:
- `SharePermission.Editor` (value: 3) - Full edit access
- `SharePermission.Viewer` (value: 1) - Read-only access

## Future Enhancements

1. **Activity Log**: Track who edited what and when
2. **Comment System**: Implement Commenter permission functionality
3. **Conflict Resolution**: Handle simultaneous edits by multiple users
4. **Version History**: Track file versions when edited by multiple users
5. **Notification System**: Notify owners when editors make changes
6. **Bulk Operations**: Allow editors to perform bulk uploads/deletes
7. **Access Analytics**: Show folder owner who accessed/edited what

## API Endpoints

No new API endpoints required. All functionality uses existing controller actions with enhanced permission checking.

## Configuration

No additional configuration required. Feature is enabled by default for all shares with Editor permission.

## Support

For issues or questions:
1. Check permission level in share settings
2. Verify user is logged in with correct account
3. Check storage quota hasn't been exceeded
4. Review application logs for permission-related errors
