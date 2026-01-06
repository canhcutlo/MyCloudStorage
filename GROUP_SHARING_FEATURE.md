# Group Sharing Feature Documentation

## Overview

The Group Sharing feature allows users to organize contacts into reusable groups for efficient bulk sharing of files and folders. Instead of sharing files with multiple people one by one, you can create groups and share with all members simultaneously.

## Key Features

- **Contact Management**: Store and organize email contacts for sharing
- **Sharing History Integration**: Automatically track contacts from your sharing history
- **Group-Based Bulk Sharing**: Share files/folders with multiple people at once
- **Manual Contact Addition**: Add contacts manually to groups
- **Quick Import**: Import all contacts from sharing history with one click

---

## How to Use

### 1. Creating a Group

1. Navigate to **Groups** in the main navigation menu
2. Click the **Create New Group** button
3. Enter the following information:
   - **Name** (required): A descriptive name for the group (max 100 characters)
   - **Description** (optional): Additional details about the group (max 500 characters)
4. Click **Create** to save the group

**Note**: After creating a group, you'll need to add members before you can use it for sharing.

### 2. Managing Group Members

#### Adding Members Manually

1. Go to **Groups** and click **Manage** on the desired group
2. In the left panel, use the **Add Member** form:
   - **Email Address** (required): Enter a valid email address
   - **Display Name** (optional): Enter a friendly name for the contact
3. Click **Add Member**

The system will automatically:
- Check if the email belongs to a registered user
- Link the member to their user account if they exist
- Mark them as a "Registered User" in the member list

#### Quick Add from Sharing History

The system automatically tracks all email addresses you've shared files with in the past.

1. In the **Manage Members** page, scroll to "Quick Add from Sharing History"
2. Click on any email address to populate the form
3. Submit to add them to the group

#### Import All from History

To quickly build a group from your sharing history:

1. Click the **Import All from History** button
2. Confirm the action
3. All contacts from your sharing history will be added to the group at once

**Note**: Duplicate emails are automatically prevented - you can't add the same email twice to a group.

### 3. Sharing Files/Folders with Groups

#### Step-by-Step Process

1. Navigate to your files in **Storage**
2. Click the **Share** button on any file or folder
3. In the share modal, select **By Group** (toggle between "By Email" and "By Group")
4. Choose a group from the dropdown menu
5. Configure sharing settings:
   - **Permission**: View Only or Edit
   - **Allow Download**: Enable/disable file downloads
   - **Expiration Date**: Optional expiration for the share
   - **Notify Recipients**: Send email notifications
   - **Message**: Optional message to include in notifications
6. Click **Share** to share with all group members

#### What Happens When You Share

When you share with a group:
- Each member receives an **individual share record** in the database
- Members with registered accounts can access the file in their "Shared with Me" section
- Members without accounts receive email invitations (if notifications are enabled)
- Each share has its own access token for security
- All members receive the same permissions you configured

#### Share Results

After sharing with a group, you'll see:
- **Success Count**: Number of members who received the share successfully
- **Failed Count**: Number of members where sharing failed (if any)
- **Failed Emails**: List of email addresses that couldn't receive the share

---

## Technical Details

### Database Schema

#### Groups Table
```sql
- Id (int, primary key)
- Name (nvarchar(100), required)
- Description (nvarchar(500), nullable)
- OwnerId (nvarchar(450), foreign key to AspNetUsers)
- CreatedAt (datetime2)
- ModifiedAt (datetime2, nullable)
```

**Indexes**:
- Unique index on `(OwnerId, Name)` - prevents duplicate group names per user

#### GroupMembers Table
```sql
- Id (int, primary key)
- GroupId (int, foreign key to Groups, cascade delete)
- Email (nvarchar(256), required)
- DisplayName (nvarchar(100), nullable)
- UserId (nvarchar(450), foreign key to AspNetUsers, nullable)
- AddedAt (datetime2)
- IsFromSharingHistory (bit)
```

**Indexes**:
- Unique index on `(GroupId, Email)` - prevents duplicate members per group
- Index on `Email` - for quick lookups

### Key Service Methods

#### GroupService

- `GetUserGroupsAsync(userId)` - Retrieves all groups owned by a user
- `CreateGroupAsync(name, description, ownerId)` - Creates a new group
- `UpdateGroupAsync(id, name, description, userId)` - Updates group details
- `DeleteGroupAsync(id, userId)` - Deletes a group and all its members
- `AddMemberAsync(groupId, email, displayName, userId)` - Adds a member to a group
- `RemoveMemberAsync(memberId, groupId, userId)` - Removes a member from a group
- `GetSharingHistoryContactsAsync(userId)` - Gets unique emails from sharing history
- `ImportContactsFromHistoryAsync(groupId, userId)` - Imports all history contacts to a group
- `GetGroupMemberEmailsAsync(groupId, userId)` - Gets list of all member emails in a group

#### SharingService

- `ShareWithGroupAsync(itemId, sharedByUserId, groupId, permission, expiresAt, allowDownload, notify, message)` - Shares an item with all members of a group

### Security Features

1. **Ownership Validation**: 
   - Users can only manage their own groups
   - Users can only share their own files/folders with groups

2. **Duplicate Prevention**: 
   - Database constraints prevent duplicate group names per user
   - Unique email addresses per group enforced

3. **Cascade Deletion**: 
   - Deleting a group automatically removes all its members
   - Deleting a user's group maintains data integrity

4. **Access Control**: 
   - Each share creates individual access tokens
   - Registered users are automatically linked when added to groups

---

## Use Cases

### 1. Team Collaboration
Create groups for different teams (e.g., "Marketing Team", "Development Team") and share project files with the entire team at once.

### 2. Client Management
Organize clients into groups and share deliverables, reports, or updates with specific client groups.

### 3. Department Communication
Create department-wide groups for sharing company documents, policies, or announcements.

### 4. Project-Based Sharing
Group all stakeholders of a specific project together for easy file distribution.

### 5. Recurring Sharing
For files you share regularly with the same set of people, groups eliminate repetitive email entry.

---

## Benefits

✅ **Time Saving**: Share with multiple people in a single action  
✅ **Organization**: Keep contacts organized in logical groups  
✅ **History Tracking**: Automatically builds contact list from your sharing activity  
✅ **Flexibility**: Mix registered users and external email addresses in the same group  
✅ **Reusability**: Create once, use repeatedly for different files  
✅ **Individual Control**: Each member gets their own share record with unique access  
✅ **Easy Management**: Add or remove members as your team changes  

---

## Limitations and Notes

- Group names must be unique within your account (case-insensitive)
- Email addresses must be unique within each group
- Maximum group name length: 100 characters
- Maximum description length: 500 characters
- Maximum email length: 256 characters
- Members are added immediately; they cannot be bulk-added except via import
- Sharing with a group creates individual shares (not a group-level share)
- Deleting a group does NOT revoke existing shares made to that group's members
- Only the group owner can manage members and use the group for sharing

---

## API Endpoints

### Groups Management
- `GET /Groups/Index` - View all your groups
- `GET /Groups/Create` - Display create group form
- `POST /Groups/Create` - Create a new group
- `GET /Groups/Edit/{id}` - Display edit group form
- `POST /Groups/Edit/{id}` - Update group details
- `POST /Groups/Delete/{id}` - Delete a group

### Member Management
- `GET /Groups/ManageMembers/{id}` - View and manage group members
- `POST /Groups/AddMember` - Add a member to a group (AJAX)
- `POST /Groups/RemoveMember` - Remove a member from a group (AJAX)
- `POST /Groups/ImportContacts` - Import all sharing history contacts (AJAX)

### API Endpoints for JavaScript
- `GET /Groups/GetUserGroups` - Returns JSON list of user's groups with member counts
- `GET /Groups/GetGroupMembers/{groupId}` - Returns JSON array of member emails

### Sharing
- `POST /Storage/ShareWithGroup` - Share a file/folder with a group

---

## Troubleshooting

### "Invalid data" error when adding members
- Ensure email address is in valid format
- Check that the group ID is correct
- Verify you're the owner of the group

### "Member already exists" error
- The email address is already in this group
- Check the member list before adding

### Group not appearing in share dropdown
- Refresh the page to reload groups
- Ensure the group has at least one member
- Verify you're logged in as the group owner

### Members not receiving notifications
- Check that "Notify Recipients" is enabled
- Verify email service is configured properly
- Confirm recipient email addresses are correct

### Share fails for some group members
- Check the failed emails list in the share result
- Verify those email addresses are valid
- Ensure you have permission to share the file

---

## Future Enhancements (Potential)

- Group-level permissions instead of individual shares
- Nested groups or group hierarchies
- Group templates for quick setup
- Bulk member import from CSV
- Group sharing analytics
- Member roles within groups
- Shared group ownership
- Group activity logs

---

## Related Documentation

- [SHARING_FEATURES_DOCUMENTATION.md](SHARING_FEATURES_DOCUMENTATION.md) - General sharing features
- [SHARING_EDIT_FEATURES.md](SHARING_EDIT_FEATURES.md) - Edit permission details
- [PROJECT_DOCUMENTATION.txt](PROJECT_DOCUMENTATION.txt) - Overall project documentation
