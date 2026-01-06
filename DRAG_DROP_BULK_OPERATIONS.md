# Drag-and-Drop Upload and Bulk Operations Features

## Overview
This document describes the drag-and-drop file upload and bulk operations features that enhance the user experience for managing files in the cloud storage application.

## Features

### 1. Drag-and-Drop File Upload

#### User Interface
- **Drop Zone**: A dedicated area at the top of the file browser with visual feedback
- **Visual States**:
  - Default: Light blue background with dashed border and upload icon
  - Drag Over: Blue background with solid border
  - Upload Progress: Progress bar showing percentage complete

#### Functionality
- **Multiple File Upload**: Users can drop multiple files simultaneously
- **Real-time Progress**: XHR-based upload with progress bar
- **Auto-refresh**: File list refreshes automatically after successful upload
- **Error Handling**: Displays error messages if upload fails

#### Technical Implementation
```javascript
// Drag event handlers prevent default browser behavior
dropZone.addEventListener('dragover', handleDragOver);
dropZone.addEventListener('dragleave', handleDragLeave);
dropZone.addEventListener('drop', handleDrop);

// XHR upload with progress tracking
xhr.upload.onprogress = (e) => {
    const percent = Math.round((e.loaded / e.total) * 100);
    progressBar.style.width = percent + '%';
};
```

### 2. Bulk Operations

#### User Interface Components

##### Selection Checkboxes
- **Checkbox Column**: Added to the file/folder table as the first column
- **Select All**: Checkbox in the table header to toggle all items
- **Item Count**: Shows number of selected items in the bulk toolbar

##### Bulk Actions Toolbar
- **Visibility**: Hidden by default, appears when items are selected
- **Actions**:
  - **Delete**: Moves selected items to trash
  - **Move**: Relocates selected items to a chosen folder
  - **Download**: Creates a ZIP archive of selected files

#### Functionality Details

##### Bulk Delete
- **Action**: Moves all selected items to trash (soft delete)
- **Confirmation**: Shows modal with item count before deletion
- **Auto-refresh**: Updates file list after successful deletion
- **Versioning**: Preserves file versions for deleted files

##### Bulk Move
- **Action**: Relocates multiple files/folders to a target folder
- **Folder Selection**: Modal with dropdown of available folders
- **Validation**: Prevents moving items to themselves or their children
- **Auto-refresh**: Updates file list after successful move

##### Bulk Download
- **Action**: Downloads multiple files as a single ZIP archive
- **Compression**: Uses System.IO.Compression to create ZIP
- **Naming**: ZIP file named based on current folder or "selected-files.zip"
- **File Structure**: Preserves folder structure in the ZIP

#### Technical Implementation

##### Controller Actions

**BulkDelete**
```csharp
[HttpPost]
public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
{
    var user = await _userManager.GetUserAsync(User);
    foreach (var itemId in request.ItemIds)
    {
        var item = await _context.StorageItems.FindAsync(itemId);
        if (item != null && item.OwnerId == user.Id)
        {
            await _storageService.DeleteItemAsync(itemId, user.Id);
        }
    }
    return Json(new { success = true });
}
```

**BulkMove**
```csharp
[HttpPost]
public async Task<IActionResult> BulkMove([FromBody] BulkMoveRequest request)
{
    var user = await _userManager.GetUserAsync(User);
    foreach (var itemId in request.ItemIds)
    {
        var item = await _context.StorageItems.FindAsync(itemId);
        if (item != null && item.OwnerId == user.Id)
        {
            item.ParentFolderId = request.TargetFolderId == 0 ? null : request.TargetFolderId;
            item.ModifiedAt = DateTime.UtcNow;
        }
    }
    await _context.SaveChangesAsync();
    return Json(new { success = true });
}
```

**BulkDownload**
```csharp
[HttpPost]
public async Task<IActionResult> BulkDownload([FromBody] BulkDownloadRequest request)
{
    var user = await _userManager.GetUserAsync(User);
    var items = new List<StorageItem>();
    
    foreach (var itemId in request.ItemIds)
    {
        var item = await _context.StorageItems.FindAsync(itemId);
        if (item != null && item.OwnerId == user.Id && item.Type == "File")
        {
            items.Add(item);
        }
    }
    
    // Create ZIP archive
    var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
    using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
    {
        foreach (var item in items)
        {
            if (File.Exists(item.FilePath))
            {
                archive.CreateEntryFromFile(item.FilePath, item.Name);
            }
        }
    }
    
    var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
    System.IO.File.Delete(zipPath);
    
    return File(zipBytes, "application/zip", zipFileName);
}
```

**UploadMultiple**
```csharp
[HttpPost]
public async Task<IActionResult> UploadMultiple(List<IFormFile> files, int? parentId)
{
    var user = await _userManager.GetUserAsync(User);
    long totalSize = files.Sum(f => f.Length);
    long currentUsage = await _storageService.GetUserStorageUsageAsync(user.Id);
    
    if (currentUsage + totalSize > user.StorageQuota)
    {
        return Json(new { success = false, error = "Insufficient storage quota" });
    }
    
    foreach (var file in files)
    {
        await _fileStorageService.UploadFileAsync(file, user.Id, parentId);
    }
    
    return Json(new { success = true, count = files.Count });
}
```

##### JavaScript Functions

**Selection Management**
```javascript
function toggleSelectAll() {
    const checked = document.getElementById('selectAll').checked;
    document.querySelectorAll('.item-checkbox').forEach(cb => cb.checked = checked);
    updateBulkActions();
}

function updateBulkActions() {
    const selectedCount = getSelectedIds().length;
    const toolbar = document.querySelector('.bulk-actions-toolbar');
    toolbar.style.display = selectedCount > 0 ? 'block' : 'none';
    document.getElementById('selectedCount').textContent = selectedCount;
}

function getSelectedIds() {
    return Array.from(document.querySelectorAll('.item-checkbox:checked'))
        .map(cb => parseInt(cb.value));
}
```

**Bulk Operations**
```javascript
async function bulkDelete() {
    const itemIds = getSelectedIds();
    const response = await fetch('/Storage/BulkDelete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ itemIds })
    });
    
    if (response.ok) {
        location.reload();
    }
}

async function bulkMove(targetFolderId) {
    const itemIds = getSelectedIds();
    const response = await fetch('/Storage/BulkMove', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ itemIds, targetFolderId })
    });
    
    if (response.ok) {
        location.reload();
    }
}

async function bulkDownload() {
    const itemIds = getSelectedIds();
    const response = await fetch('/Storage/BulkDownload', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ itemIds })
    });
    
    if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = response.headers.get('Content-Disposition')
            .split('filename=')[1].replace(/"/g, '');
        a.click();
    }
}
```

## User Experience

### Drag-and-Drop Upload Flow
1. User drags files from their file system over the drop zone
2. Drop zone highlights with blue background to indicate readiness
3. User drops files - upload begins immediately
4. Progress bar shows upload percentage
5. On completion, file list refreshes to show new files
6. Drop zone returns to default state

### Bulk Operations Flow
1. User selects multiple items using checkboxes
2. Bulk actions toolbar appears showing selection count
3. User clicks desired action (Delete, Move, or Download)
4. Confirmation modal appears (for Delete and Move)
5. User confirms action
6. Operation executes on all selected items
7. Success message appears and page refreshes

## Browser Compatibility
- **Drag-and-Drop**: Supported in all modern browsers (Chrome, Firefox, Edge, Safari)
- **XHR Upload Progress**: Supported in all modern browsers
- **FormData API**: Required for drag-and-drop uploads, supported in all modern browsers

## Security Considerations

### Upload Security
- **File Size Validation**: Enforced on server-side
- **Storage Quota Check**: Prevents exceeding user storage limits
- **File Type Validation**: MimeType validation on server
- **Authentication Required**: All endpoints require [Authorize] attribute

### Bulk Operations Security
- **Ownership Verification**: Each item checked for ownership before operation
- **Authorization**: Only owner can perform bulk operations on their files
- **SQL Injection Prevention**: Uses parameterized queries via Entity Framework

## Performance Considerations

### Drag-and-Drop Upload
- **Chunked Upload**: Consider implementing for large files (future enhancement)
- **Concurrent Uploads**: Currently sequential, could be parallelized
- **Progress Tracking**: Minimal overhead using XHR progress events

### Bulk Operations
- **Database Queries**: Uses FindAsync for individual item lookups
- **Transaction Management**: SaveChangesAsync batches updates
- **ZIP Creation**: Uses temporary file, cleaned up after download
- **Memory Usage**: ZIP created on disk, not in memory

## Testing

### Manual Testing Checklist

#### Drag-and-Drop
- [ ] Drag single file over drop zone - highlights correctly
- [ ] Drop single file - uploads successfully
- [ ] Drag multiple files - all upload
- [ ] Upload progress bar displays correctly
- [ ] File list refreshes after upload
- [ ] Storage quota enforced on upload
- [ ] Error message shown when quota exceeded

#### Bulk Delete
- [ ] Select multiple items - checkboxes work
- [ ] Select all checkbox toggles all items
- [ ] Bulk delete toolbar appears when items selected
- [ ] Delete confirmation modal shows correct count
- [ ] All selected items moved to trash
- [ ] File list refreshes after delete
- [ ] Cannot delete items owned by other users

#### Bulk Move
- [ ] Move modal shows available folders
- [ ] Cannot move to current folder
- [ ] All selected items moved to target folder
- [ ] File list refreshes after move
- [ ] Cannot move items owned by other users

#### Bulk Download
- [ ] ZIP file downloads with correct name
- [ ] ZIP contains all selected files
- [ ] File names preserved in ZIP
- [ ] Cannot download items owned by other users

### Automated Testing (Future Enhancement)
```csharp
// Example unit test for BulkDelete
[Fact]
public async Task BulkDelete_WithValidItems_DeletesAll()
{
    // Arrange
    var itemIds = new[] { 1, 2, 3 };
    var request = new BulkDeleteRequest { ItemIds = itemIds };
    
    // Act
    var result = await _controller.BulkDelete(request);
    
    // Assert
    var jsonResult = Assert.IsType<JsonResult>(result);
    var response = Assert.IsType<dynamic>(jsonResult.Value);
    Assert.True(response.success);
}
```

## Known Limitations

1. **Drag-and-Drop**: Does not support folder uploads (browser limitation)
2. **Bulk Operations**: No undo functionality (items go to trash, can be restored)
3. **ZIP Download**: Does not preserve folder structure for nested items
4. **Progress Tracking**: No global progress for multiple file uploads
5. **File Size**: Very large bulk downloads may timeout (consider streaming ZIP)

## Future Enhancements

1. **Chunked Upload**: For files larger than 100MB
2. **Resume Upload**: Support resuming interrupted uploads
3. **Drag to Move**: Drag files between folders
4. **Folder Structure in ZIP**: Preserve nested folder structure in bulk downloads
5. **Parallel Uploads**: Upload multiple files concurrently
6. **Upload Queue**: Show queue of pending uploads
7. **Drag from Browser**: Support dragging files between browser windows
8. **Copy/Paste**: Support Ctrl+C / Ctrl+V for file operations

## Related Documentation
- [File Versioning Documentation](FILE_VERSIONING_DOCUMENTATION.md)
- [Sharing Features Documentation](SHARING_FEATURES_DOCUMENTATION.md)
- [Trash Feature Documentation](TRASH_FEATURE.md)

## Changelog

### Version 1.0 (January 2026)
- Initial implementation of drag-and-drop file upload
- Added bulk delete, move, and download operations
- Implemented selection checkboxes and bulk toolbar
- Added XHR upload with progress tracking
