# Activity Feed Feature Documentation

## Overview
The Activity Feed feature provides comprehensive tracking and visualization of user actions on files and folders within the cloud storage application. This feature helps users monitor file access, modifications, and collaboration activities.

## Features

### 1. Activity Tracking
The system automatically logs the following activities:
- **File Uploaded** - When a user uploads a new file
- **File Downloaded** - When a user downloads a file
- **File Viewed** - When a user previews/views a file
- **File Edited** - When a user modifies file content
- **File Deleted** - When a file is moved to trash
- **File Restored** - When a file is restored from trash
- **File Moved** - When a file is moved to a different folder
- **File Renamed** - When a file name is changed
- **File Shared** - When a file is shared with others
- **File Unshared** - When sharing is revoked
- **Folder Created** - When a new folder is created
- **Folder Deleted** - When a folder is deleted
- **Comment Added** - When a comment is added to a file
- **Comment Edited** - When a comment is modified
- **Comment Deleted** - When a comment is removed
- **Permission Changed** - When file/folder permissions are updated
- **File Added to Favorites** - When a file is favorited
- **File Removed from Favorites** - When a file is unfavorited

### 2. Activity Details Logged
Each activity record includes:
- **Activity Type** - The type of action performed
- **User** - Who performed the action (name, email)
- **File/Folder** - The target item
- **Timestamp** - When the action occurred
- **Description** - Human-readable description
- **IP Address** - User's IP address
- **User Agent** - Browser/device information
- **Additional Data** - Context-specific information

### 3. Activity Views

#### Recent Activity View (`/Activity/Recent`)
- Timeline-style display of recent activities
- Visual icons for each activity type
- "Time ago" format for recent actions
- Clean, card-based layout
- Default shows last 20 activities

#### Full Activity Feed (`/Activity/Index`)
- Comprehensive table view of all activities
- Advanced filtering options:
  - Date range (From/To dates)
  - Activity type filter
- Pagination support (50 items per page)
- Sortable columns
- User avatars and details
- File type indicators

#### File-Specific Activity (`/Activity/FileActivity/{id}`)
- Shows all activities for a specific file
- Useful for file audit trails
- Last 100 activities per file

### 4. Navigation
Activity feed is accessible from:
- **Navigation Menu** - "Activity" link in main navigation
- **API Endpoint** - `/Activity/GetRecentActivities` for AJAX calls

## Implementation Details

### Database Schema
**ActivityLogs Table:**
```sql
CREATE TABLE [ActivityLogs] (
    [Id] int IDENTITY(1,1) PRIMARY KEY,
    [StorageItemId] int NULL,
    [UserId] nvarchar(450) NOT NULL,
    [ActivityType] int NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [IpAddress] nvarchar(45) NULL,
    [UserAgent] nvarchar(500) NULL,
    [AdditionalData] nvarchar(1000) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]),
    FOREIGN KEY ([StorageItemId]) REFERENCES [StorageItems]([Id]) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX [IX_ActivityLogs_Timestamp] ON [ActivityLogs]([Timestamp]);
CREATE INDEX [IX_ActivityLogs_StorageItemId_Timestamp] ON [ActivityLogs]([StorageItemId], [Timestamp]);
CREATE INDEX [IX_ActivityLogs_UserId_Timestamp] ON [ActivityLogs]([UserId], [Timestamp]);
```

### Service Layer
**IActivityService Interface:**
```csharp
Task LogActivityAsync(int? storageItemId, string userId, ActivityType activityType, 
    string description, string? ipAddress = null, string? userAgent = null, 
    string? additionalData = null);
Task<List<ActivityLog>> GetRecentActivitiesAsync(string userId, int count = 50);
Task<List<ActivityLog>> GetFileActivitiesAsync(int storageItemId, int count = 100);
Task<List<ActivityLog>> GetAllActivitiesAsync(string userId, DateTime? fromDate = null, 
    DateTime? toDate = null, ActivityType? activityType = null, int pageNumber = 1, 
    int pageSize = 50);
Task<int> GetActivityCountAsync(string userId, DateTime? fromDate = null, 
    DateTime? toDate = null);
```

### Controllers Integration
Activity logging is integrated into:
- **StorageController** - Upload, Download, Delete actions
- **PreviewController** - Document view and Edit actions
- **ShareController** - Share/Unshare actions (can be added)

Example usage in controller:
```csharp
var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
await _activityService.LogActivityAsync(
    item.Id,
    userId,
    ActivityType.FileUploaded,
    $"Uploaded file: {item.Name}",
    ipAddress,
    userAgent
);
```

## Security & Privacy

### Access Control
- Users can only see activities for:
  - Files they own
  - Files shared with them
- Activity logs respect file permissions
- Deleted files' activities are removed (CASCADE)

### Data Retention
- Activities are stored indefinitely by default
- Consider implementing a cleanup policy for old activities
- IP addresses and user agents are stored for security auditing

## Performance Considerations

### Database Indexes
Three indexes are created for optimal query performance:
1. **Timestamp Index** - For time-based queries
2. **StorageItemId + Timestamp** - For file-specific activity queries
3. **UserId + Timestamp** - For user activity feed queries

### Query Optimization
- Pagination limits data transfer
- Eager loading (Include) for related entities
- Filtering at database level before loading

### Caching Opportunities
Consider caching:
- Recent activities (5-15 minutes)
- Activity counts
- User-specific activity feeds

## Usage Examples

### View Recent Activities
1. Click "Activity" in the navigation menu
2. See timeline of recent file operations
3. Click "View All Activity" for comprehensive feed

### Filter Activities
1. Navigate to `/Activity/Index`
2. Click "Filters" button
3. Select date range and/or activity type
4. Click "Filter" to apply

### View File Activity History
1. Go to file details or preview
2. Access file-specific activity log
3. See complete audit trail

### API Integration
Fetch recent activities via AJAX:
```javascript
fetch('/Activity/GetRecentActivities?count=10')
    .then(response => response.json())
    .then(data => {
        data.forEach(activity => {
            console.log(`${activity.userName} ${activity.activityType} ${activity.fileName} ${activity.timeAgo}`);
        });
    });
```

## Future Enhancements

### Potential Features
1. **Real-time Updates** - WebSocket/SignalR for live activity feed
2. **Activity Notifications** - Email/push notifications for important activities
3. **Activity Export** - Export activity logs to CSV/PDF
4. **Advanced Analytics** - Charts and graphs for activity trends
5. **Activity Webhooks** - Trigger external systems on specific activities
6. **Custom Activity Types** - Allow plugins to register custom activities
7. **Activity Rollback** - Undo certain activities
8. **Team Activity Dashboard** - Aggregated view for team admins
9. **Activity Search** - Full-text search across descriptions
10. **Compliance Reporting** - Generate audit reports for compliance

### Integration Ideas
- Integrate with file versioning to track version changes
- Link activities to comment threads
- Show activities in file preview sidebar
- Activity-based recommendations ("You might be interested in...")

## Troubleshooting

### No Activities Showing
- **Issue**: Activity feed is empty
- **Solution**: 
  - Perform some file operations (upload, download, view)
  - Check database for ActivityLogs table
  - Verify user has access to files

### Activities Not Logging
- **Issue**: Activities not being saved
- **Solution**:
  - Check IActivityService is registered in Program.cs
  - Verify database connection
  - Check for exceptions in logs
  - Ensure controllers have IActivityService injected

### Performance Issues
- **Issue**: Activity feed loads slowly
- **Solution**:
  - Reduce page size (default 50, try 20)
  - Add date range filter to limit results
  - Check database indexes exist
  - Consider implementing caching

### Missing User/File Information
- **Issue**: User or file names show as null
- **Solution**:
  - Ensure eager loading (Include) is used in queries
  - Check foreign key relationships
  - Verify soft-deleted files are handled

## API Reference

### Endpoints

#### GET /Activity/Index
Shows paginated activity feed with filtering
- **Parameters**: 
  - `fromDate` (DateTime, optional)
  - `toDate` (DateTime, optional)
  - `activityType` (ActivityType enum, optional)
  - `page` (int, default 1)
- **Returns**: View with list of ActivityLog

#### GET /Activity/Recent
Shows recent activities in timeline format
- **Parameters**: 
  - `count` (int, default 20)
- **Returns**: View with list of ActivityLog

#### GET /Activity/FileActivity/{id}
Shows activities for specific file
- **Parameters**: 
  - `id` (int, file ID)
- **Returns**: View with list of ActivityLog

#### GET /Activity/GetRecentActivities (API)
Returns JSON array of recent activities
- **Parameters**: 
  - `count` (int, default 10)
- **Returns**: JSON array
```json
[
  {
    "id": 1,
    "activityType": "FileUploaded",
    "description": "Uploaded file: document.pdf",
    "fileName": "document.pdf",
    "userName": "john@example.com",
    "timestamp": "2026-01-05T15:30:00Z",
    "timeAgo": "2h ago"
  }
]
```

## Conclusion
The Activity Feed feature provides comprehensive tracking and transparency for file operations, enhancing security, collaboration, and user awareness within the cloud storage application.
