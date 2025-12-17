# Google Drive-like Sharing Features - Implementation Guide

## Overview
This document describes the enhanced sharing features implemented for the MyCloudStorage application, inspired by Google Drive's intuitive sharing system.

## Key Features Implemented

### 1. **Modern Share Modal UI**
- **Multi-tab Interface**: Three organized tabs for different sharing workflows
  - "Share with people": Add specific users by email
  - "Get link": Generate and copy shareable links
  - "Who has access": Manage existing permissions
  
- **Google Drive-inspired Design**:
  - Gradient headers (#667eea to #764ba2)
  - Clean, modern card-based layout
  - Responsive design for mobile and desktop
  - Smooth animations and transitions

### 2. **Enhanced Permission System**
Updated from basic 4-level system to Google Drive-like roles:
- **Viewer**: Can view only (replaces ViewOnly)
- **Commenter**: Can view and comment (new - future feature)
- **Editor**: Can view, comment, and edit (replaces Edit)
- **Owner**: Full access including delete (replaces FullAccess)

### 3. **Advanced Share Options**
- **Download Control**: Toggle whether viewers can download files
- **Email Notifications**: Automatically notify people when sharing
- **Custom Messages**: Add personal messages when sharing
- **Link Generation**: Automatic creation of shareable links
- **Expiration Dates**: Set time limits on shared access (existing feature)

### 4. **Access Analytics**
New tracking capabilities:
- **Last Accessed**: Track when shared items were last viewed
- **Access Count**: Monitor how many times items have been accessed
- **Notification Status**: Track if share notifications were sent

### 5. **Real-time Permission Management**
- Change permissions without page reload
- Remove access instantly
- Update download permissions on the fly
- View access history for each share

## Database Changes

### New Columns in SharedItems Table:
```sql
AllowDownload    BIT NOT NULL DEFAULT 1
Notify           BIT NOT NULL DEFAULT 1
NotificationSent BIT NOT NULL DEFAULT 0
LastAccessedAt   DATETIME2 NULL
AccessCount      INT NOT NULL DEFAULT 0
Message          NVARCHAR(MAX) NULL
```

### New Indexes:
- `IX_SharedItems_AccessToken`: Fast token lookups
- `IX_SharedItems_LastAccessedAt`: Analytics queries

## API Endpoints

### New ShareController Actions:
1. **GET /Share/GetSharesForItem?itemId={id}**
   - Returns all shares for a specific item
   - Requires: User owns the item
   - Response: JSON array of share objects

2. **POST /Share/ChangePermission**
   - Parameters: shareId, permission, allowDownload
   - Updates permission level and download settings
   - Response: Success/failure JSON

3. **POST /Share/RemoveAccess**
   - Parameter: shareId
   - Deactivates a share
   - Response: Success/failure JSON

4. **GET /Share/GetShareLink?itemId={id}**
   - Creates or retrieves public link for item
   - Returns: Full shareable URL
   - Response: JSON with link

5. **GET /Share/GetAccessInfo?shareId={id}**
   - Returns detailed analytics for a share
   - Includes: access count, last accessed, permissions
   - Response: JSON object

## Service Layer Updates

### ISharingService New Methods:
```csharp
Task<IEnumerable<SharedItem>> GetSharesForItemAsync(int itemId, string userId);
Task<bool> ChangePermissionAsync(int shareId, SharePermission permission, bool allowDownload, string userId);
Task<string> GetShareLinkAsync(int itemId, string userId);
Task<Dictionary<string, object>> GetAccessInfoAsync(int shareId, string userId);
Task SendShareNotificationAsync(SharedItem share);
Task TrackAccessAsync(string token);
```

### Email Notifications:
- Beautiful HTML email templates with gradient design
- Includes:
  - Sharer's name
  - File/folder name
  - Permission level explanation
  - Direct link to access
  - Optional personal message
  - Expiration date (if set)

## Frontend Components

### Files Created:
1. **wwwroot/js/share-modal.js** (475 lines)
   - Modal management functions
   - AJAX calls for sharing operations
   - Permission change handlers
   - Link copying functionality
   - Real-time share list updates

2. **wwwroot/css/share-modal.css** (358 lines)
   - Google Drive-inspired styling
   - Tab system with active states
   - Responsive breakpoints
   - Avatar and badge styles
   - Animation definitions

3. **Views/Shared/_ShareModal.cshtml** (110 lines)
   - Reusable modal component
   - Three-tab interface
   - Form inputs for sharing
   - Access management list

### UI Integration:
- Modal included in _Layout.cshtml for authenticated users
- Share buttons updated to trigger modal instead of page navigation
- Shared items display indicator icons
- Tooltips added to action buttons

## Usage

### For Users:
1. **Share a File/Folder**:
   - Click share button on any item
   - Modal opens with three tabs
   - Enter email or generate link
   - Set permission level
   - Optional: Add message, disable download
   - Click "Share" button

2. **Manage Shares**:
   - Open share modal on shared item
   - Go to "Who has access" tab
   - View list of all shares
   - Change permissions from dropdown
   - Remove access with one click

3. **Copy Link**:
   - Go to "Get link" tab
   - Link auto-generates if needed
   - Click "Copy link" button
   - Share via any method

### For Developers:
```javascript
// Open share modal programmatically
openShareModal(itemId, itemName);

// Share with specific person
shareWithPerson(); // Called from modal

// Change permissions
changePermission(shareId, newPermission);

// Remove access
removeAccess(shareId);
```

## Security Considerations

### Access Control:
- All share operations verify ownership
- Token-based access for public links
- User-specific shares require authentication
- Permissions enforced at service layer

### Token Security:
- Cryptographically secure random tokens (32 bytes)
- URL-safe Base64 encoding
- Unique per share
- Indexed for fast lookups

### Download Protection:
- AllowDownload flag enforced in ShareController
- Viewers can be prevented from downloading
- Useful for sensitive documents

## Migration Instructions

### Run the SQL Migration:
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -d CloudStorage -i "Migrations\AddShareFeatures.sql"
```

### Build and Run:
```bash
dotnet build
dotnet run
```

## Testing Checklist

- [x] Share file with specific email
- [x] Email notification sent successfully
- [x] Custom message included in email
- [x] Generate public link
- [x] Copy link to clipboard
- [x] View all shares for an item
- [x] Change permission level
- [x] Toggle download permission
- [x] Remove access
- [x] Access tracking (count increments)
- [x] Last accessed timestamp updates
- [x] Shared indicator shows on items
- [x] Modal opens/closes smoothly
- [x] Responsive on mobile

## Future Enhancements

### Planned Features:
1. **Commenter Role Implementation**:
   - Add comment functionality to files
   - Comment threads and replies
   - Comment notifications

2. **Folder Sharing**:
   - Share entire folders
   - Inherited permissions for child items
   - Bulk permission changes

3. **Advanced Expiration**:
   - Specific time (not just date)
   - Auto-renewal options
   - Expiration warnings

4. **Share Templates**:
   - Save common sharing configurations
   - Quick share with presets
   - Organization-level defaults

5. **Activity Dashboard**:
   - View all sharing activity
   - Analytics charts
   - Export share reports

6. **Advanced Security**:
   - Password-protected links
   - Two-factor for sensitive shares
   - Geographic restrictions
   - Device restrictions

## Browser Compatibility

### Tested Browsers:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

### Required APIs:
- Clipboard API (for link copying)
- Fetch API (for AJAX calls)
- Bootstrap 5 (for modal/UI)

## Performance Considerations

### Optimizations:
- Indexed database columns for fast queries
- Lazy loading of share lists
- Debounced search inputs
- Cached share links
- Efficient AJAX calls (no page reloads)

### Scalability:
- Share operations are async
- Database queries use proper indexes
- Email sending doesn't block responses
- Background tracking doesn't slow down access

## Support and Troubleshooting

### Common Issues:

1. **Modal doesn't open**:
   - Check JavaScript console for errors
   - Ensure share-modal.js is loaded
   - Verify Bootstrap is initialized

2. **Email not sent**:
   - Check Gmail SMTP settings in appsettings.json
   - Verify app password is correct
   - Check IEmailService registration in Program.cs

3. **Permission change fails**:
   - Verify anti-forgery token is present
   - Check user owns the item
   - Review server logs for errors

4. **Link not copying**:
   - Ensure HTTPS or localhost
   - Clipboard API requires secure context
   - Check browser permissions

## Credits

Designed and implemented with inspiration from:
- Google Drive's sharing system
- Modern Material Design principles
- ASP.NET Core best practices
- Bootstrap 5 components

## License

Part of MyCloudStorage - A secure cloud storage solution
© 2025 - All rights reserved
