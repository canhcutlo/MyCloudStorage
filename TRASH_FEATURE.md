# Trash/Recycle Bin Feature

## Overview
Implemented a comprehensive Trash/Recycle Bin feature for the CloudStorage application, similar to Windows Recycle Bin or macOS Trash.

## Features

### 1. **Soft Delete**
- When users delete files or folders, they are moved to trash (soft delete)
- Physical files remain on disk for 15 days
- Database records are marked with `IsDeleted=true` and `DeletedAt` timestamp
- Storage quota is NOT affected until permanent deletion

### 2. **Trash View** (`/Storage/Trash`)
- Displays all deleted items
- Shows item name, type (file/folder), size, deleted date
- Calculates days remaining before auto-deletion
- Visual warning for items expiring within 3 days (yellow highlight)
- Empty state message when no items in trash

### 3. **Restore Functionality**
- Users can restore individual items from trash
- Restoring a folder also restores all its contents
- Storage quota is restored when items are recovered
- Restored items return to their original location

### 4. **Permanent Delete**
- Users can permanently delete items from trash
- Physical files are deleted from disk
- Database records are removed
- Storage quota is reduced accordingly
- Confirmation dialog prevents accidental deletion

### 5. **Empty Trash**
- Button to permanently delete all items in trash at once
- Confirmation dialog required
- Shows count of deleted items

### 6. **Automatic Cleanup**
- Background service runs every 24 hours
- Automatically deletes items older than 15 days
- Physical files and database records are removed
- Storage quota is updated
- Logged for audit purposes

## Implementation Details

### Files Modified/Created

1. **Services/StorageService.cs**
   - Added `GetDeletedItemsAsync()` - retrieves all deleted items for a user
   - Added `RestoreItemAsync()` - restores item and updates storage quota
   - Added `PermanentlyDeleteItemAsync()` - permanently removes item from database
   - Added `CleanupOldDeletedItemsAsync()` - removes items older than 15 days
   - Modified `DeleteItemAsync()` - removed storage quota reduction (now only on permanent delete)

2. **Services/TrashCleanupService.cs** (NEW)
   - Background hosted service
   - Runs every 24 hours
   - Calls `CleanupOldDeletedItemsAsync()` to remove old items
   - Logs cleanup activities

3. **Controllers/StorageController.cs**
   - Modified `Delete` action - removed physical file deletion
   - Added `Trash` action - displays trash view
   - Added `Restore` action - restores items from trash
   - Added `PermanentDelete` action - permanently deletes single item
   - Added `EmptyTrash` action - permanently deletes all trash items

4. **Views/Storage/Trash.cshtml** (NEW)
   - Responsive table view of deleted items
   - Color-coded warnings for items expiring soon
   - Restore and Delete Forever buttons
   - Empty Trash button
   - Info messages about 15-day retention

5. **Views/Shared/_Layout.cshtml**
   - Added "Trash" navigation link in header
   - Added Bootstrap Icons CSS for trash icons

6. **Program.cs**
   - Registered `TrashCleanupService` as hosted service
   - Added `CloudStorage.BackgroundServices` namespace

## User Experience

### Deleting Items
1. User clicks delete button on a file/folder
2. Item is soft-deleted (marked as IsDeleted=true)
3. Success message: "File/Folder moved to trash!"
4. Storage quota remains unchanged

### Viewing Trash
1. User clicks "Trash" in navigation menu
2. See all deleted items with details
3. Items expiring within 3 days highlighted in yellow
4. Info banner explains 15-day retention policy

### Restoring Items
1. User clicks "Restore" button on an item
2. Item and all contents restored to original location
3. Storage quota updated
4. Success message: "Item restored successfully!"

### Permanent Deletion
1. User clicks "Delete Forever" button
2. Confirmation dialog appears
3. Physical files deleted from disk
4. Database records removed
5. Storage quota reduced

### Empty Trash
1. User clicks "Empty Trash" button
2. Confirmation dialog for all items
3. All items permanently deleted
4. Success message shows count

## Technical Improvements

### Storage Quota Management
- **Before**: Storage quota reduced immediately on delete
- **After**: Storage quota unchanged until permanent delete
- **Benefit**: Users can recover files without quota penalties

### Physical File Retention
- **Before**: Physical files deleted immediately
- **After**: Physical files kept for 15 days in trash
- **Benefit**: Users can restore files with actual data

### Recursive Operations
- Folder deletion: All sub-items soft-deleted recursively
- Folder restoration: All sub-items restored recursively
- Folder permanent delete: All sub-items removed recursively
- Size calculation: Accurately calculates folder sizes including all files

### Background Cleanup
- Automatic maintenance without user intervention
- Runs daily to clean up old trash items
- Reduces database size and disk usage
- Logged for monitoring and debugging

## Configuration

### Cleanup Interval
Located in `Services/TrashCleanupService.cs`:
```csharp
private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);
```
Change this value to adjust how often cleanup runs.

### Retention Period
Located in `Services/StorageService.cs`, `CleanupOldDeletedItemsAsync()`:
```csharp
var fifteenDaysAgo = DateTime.UtcNow.AddDays(-15);
```
Change `-15` to adjust retention period (e.g., `-30` for 30 days).

## Database Schema
No changes required - uses existing `StorageItem` fields:
- `IsDeleted` (bool)
- `DeletedAt` (DateTime?)

## Testing Checklist

✅ Delete file/folder moves to trash
✅ Trash view shows deleted items
✅ Days remaining calculated correctly
✅ Items expiring soon highlighted
✅ Restore file/folder works
✅ Restore updates storage quota
✅ Permanent delete removes from trash
✅ Permanent delete reduces storage quota
✅ Empty trash removes all items
✅ Background service runs daily
✅ Auto-cleanup after 15 days
✅ Navigation link appears in header
✅ Confirmation dialogs prevent accidents
✅ Recursive folder operations work

## Future Enhancements
- Search/filter in trash view
- Bulk select and restore
- Trash size indicator
- Configurable retention period per user
- Email notifications before auto-delete
- Trash statistics dashboard
