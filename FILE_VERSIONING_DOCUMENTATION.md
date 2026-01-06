# File Versioning Feature Documentation

## Overview
The file versioning feature allows users to track and manage previous versions of their files. When a file is replaced, the system automatically creates a backup of the current version, allowing users to restore or download previous versions at any time.

## Features

### 1. Automatic Version Creation
- When you replace a file, the current version is automatically saved
- Each version is assigned a sequential version number (v1, v2, v3, etc.)
- The original file is copied to a secure versions folder

### 2. Version History
- View complete history of all file versions
- See who created each version and when
- View version size and file type information
- Read change descriptions (if provided)

### 3. Version Operations

#### View Version History
1. Navigate to your files in the Storage section
2. Click the three-dot menu next to any file
3. Select "Version History"
4. View all previous versions in chronological order

#### Replace a File
1. Click the three-dot menu next to the file
2. Select "Replace File"
3. Choose the new file to upload
4. Optionally add a change description
5. Click "Replace File"
6. The current version will be saved automatically

#### Download a Previous Version
1. Open the version history for a file
2. Find the version you want to download
3. Click the "Download" button
4. The file will be downloaded with "_v{number}" appended to its name

#### Restore a Previous Version
1. Open the version history for a file
2. Find the version you want to restore
3. Click the "Restore" button
4. Confirm the restoration
5. The selected version becomes the current file
6. The previous current version is saved automatically

#### Delete a Version
1. Open the version history for a file
2. Find the version you want to delete
3. Click the "Delete" button
4. Confirm the deletion
5. The version is soft-deleted (marked as deleted but not physically removed)

## Technical Details

### Database Schema
The `FileVersions` table stores version metadata:
- `Id`: Unique version identifier
- `StorageItemId`: Reference to the parent file
- `VersionNumber`: Sequential version number
- `FilePath`: Path to the versioned file in storage
- `FileHash`: MD5 hash for file integrity
- `Size`: File size in bytes
- `MimeType`: File content type
- `CreatedByUserId`: User who created this version
- `CreatedAt`: Timestamp of version creation
- `ChangeDescription`: Optional description of changes
- `IsDeleted`: Soft delete flag

### Storage Structure
```
wwwroot/
  uploads/
    {userId}/
      file.txt          (current version)
    versions/
      file_v1.txt       (version 1)
      file_v2.txt       (version 2)
      file_v3.txt       (version 3)
```

### Version Lifecycle

#### Creation
1. When a file is replaced via "Replace File" action
2. Current file is copied to versions folder
3. Version record is created in database
4. New file replaces the current file

#### Restoration
1. User selects a version to restore
2. Current file is automatically backed up as a new version
3. Selected version file replaces the current file
4. File metadata is updated to match the restored version

## Best Practices

### For Users
1. **Add Change Descriptions**: Always add meaningful descriptions when replacing files
2. **Regular Cleanup**: Delete old versions you no longer need to save storage space
3. **Review Before Restore**: Download and review a version before restoring it
4. **Version Control**: Use versions for important documents that change frequently

### For Administrators
1. **Storage Monitoring**: Monitor the versions folder size
2. **Cleanup Policy**: Implement automatic cleanup of old versions if needed
3. **Backup Strategy**: Include versions folder in backup plans
4. **Quota Management**: Versions count toward user storage quota

## Security Features

1. **Access Control**: Only file owners can view and manage versions
2. **Soft Delete**: Deleted versions are marked, not physically removed
3. **File Integrity**: MD5 hashes verify file integrity
4. **Audit Trail**: All version operations are logged in activity logs

## UI Components

### Version History Page
- Timeline view showing all versions
- Color-coded version badges
- User and timestamp information
- File size and type details
- Action buttons (Download, Restore, Delete)
- Confirmation modals for destructive actions

### File Actions Menu
- "Version History" - View all versions
- "Replace File" - Upload new version

### Replace File Page
- File upload control
- Change description text area
- Warning about automatic backup
- Cancel and submit buttons

## API Endpoints

### VersionController
- `GET /Version/History/{id}` - View version history
- `POST /Version/Restore/{id}` - Restore a version
- `GET /Version/Download/{id}` - Download a version
- `POST /Version/Delete/{id}` - Delete a version

### StorageController
- `GET /Storage/Replace/{id}` - Show replace file form
- `POST /Storage/Replace` - Replace a file (creates version)

## Service Layer

### IFileVersionService
```csharp
Task<FileVersion> CreateVersionAsync(StorageItem item, string userId, string changeDescription)
Task<List<FileVersion>> GetVersionHistoryAsync(int storageItemId)
Task<FileVersion?> GetVersionAsync(int versionId)
Task<bool> RestoreVersionAsync(int versionId, string userId)
Task<bool> DeleteVersionAsync(int versionId)
```

## Activity Logging
All version operations are logged with ActivityType:
- `FileModified` - When a file is replaced
- Activity logs include user, timestamp, IP address, and user agent

## Error Handling

### Common Errors
1. **File Not Found**: Version file doesn't exist in storage
2. **Permission Denied**: User doesn't own the file
3. **Storage Quota Exceeded**: Not enough space for new version
4. **Version Not Found**: Invalid version ID

### Error Messages
- Clear, user-friendly error messages
- Suggestions for resolution when possible
- Logging for debugging

## Performance Considerations

1. **Indexing**: Composite index on (StorageItemId, VersionNumber)
2. **Lazy Loading**: Versions are not loaded unless requested
3. **Pagination**: Consider adding pagination for files with many versions
4. **Cleanup**: Soft-deleted versions can be permanently deleted by background job

## Future Enhancements

### Potential Features
1. **Version Comparison**: Side-by-side diff view for text files
2. **Version Comments**: Allow comments on specific versions
3. **Automatic Versioning**: Auto-create versions on file edit
4. **Version Retention Policy**: Auto-delete versions older than X days
5. **Version Tagging**: Tag important versions (e.g., "Release 1.0")
6. **Version Branching**: Create branches from specific versions
7. **Bulk Operations**: Restore/delete multiple versions at once
8. **Version Export**: Export complete version history as archive

### Technical Improvements
1. **Compression**: Compress old versions to save space
2. **Delta Storage**: Store only differences between versions
3. **Cloud Storage**: Move old versions to cheaper cloud storage
4. **Version Limits**: Limit number of versions per file
5. **Size Optimization**: Automatically delete very old versions

## Troubleshooting

### Version Not Created
- Check file permissions on versions folder
- Verify storage quota is not exceeded
- Check application logs for errors

### Cannot Restore Version
- Verify version file exists in storage
- Check user has ownership of file
- Ensure sufficient storage quota

### Version Download Fails
- Verify file exists in versions folder
- Check file path in database is correct
- Review application logs

## Support
For issues or questions about file versioning, contact your system administrator.
