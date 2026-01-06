# Phân Tích Chi Tiết Quá Trình Download (Detailed Download Process Analysis)

## Tổng Quan (Overview)

Hệ thống CloudStorage hỗ trợ nhiều phương thức tải xuống file với các mức độ bảo mật và quyền truy cập khác nhau. Tài liệu này phân tích chi tiết từng kịch bản download và quy trình xử lý.

## Các Kịch Bản Download (Download Scenarios)

### 1. Download File Thuộc Sở Hữu (Owned File Download)
**Endpoint**: `GET /Storage/Download/{id}`  
**Controller**: `StorageController.Download()`  
**Authentication**: Required (Authorize attribute)

#### Quy Trình Xử Lý (Processing Flow)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User Request: GET /Storage/Download/{id}                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Authentication Check                                          │
│    - Verify user is logged in (via [Authorize])                │
│    - Get userId from UserManager                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Get Storage Item                                              │
│    - Query: await _storageService.GetItemByIdAsync(id)          │
│    - Check: item != null && item.Type == File                   │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Access Permission Check                                       │
│    - await _storageService.CanUserAccessItemAsync(id, userId)   │
│    - Checks if user owns file OR file is shared with user       │
│    - Returns 403 Forbid if no access                            │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Retrieve Physical File                                        │
│    - await _fileStorageService.GetFileAsync(item.FilePath)      │
│    - Path: wwwroot/uploads/{userId}/{uniqueFileName}            │
│    - Reads file into byte array                                 │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Log Activity                                                  │
│    - Type: ActivityType.FileDownloaded                          │
│    - Captures: IP address, User-Agent                           │
│    - Description: "Downloaded file: {name}"                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Return File Response                                          │
│    - File(fileBytes, mimeType, fileName)                        │
│    - Sets Content-Disposition: attachment                       │
│    - Browser triggers download                                  │
└─────────────────────────────────────────────────────────────────┘
```

#### Code Implementation

```csharp
[HttpGet]
public async Task<IActionResult> Download(int id)
{
    var userId = _userManager.GetUserId(User)!;
    
    // Step 1: Get item by ID
    var item = await _storageService.GetItemByIdAsync(id);
    
    if (item == null || item.Type != StorageItemType.File)
    {
        return NotFound();
    }
    
    // Step 2: Check access permissions
    var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
    if (!hasAccess)
    {
        return Forbid();
    }

    try
    {
        // Step 3: Get physical file
        var fileBytes = await _fileStorageService.GetFileAsync(item.FilePath);
        
        // Step 4: Log activity
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        await _activityService.LogActivityAsync(
            item.Id,
            userId,
            ActivityType.FileDownloaded,
            $"Downloaded file: {item.Name}",
            ipAddress,
            userAgent
        );
        
        // Step 5: Return file
        return File(fileBytes, item.MimeType, item.Name);
    }
    catch (FileNotFoundException)
    {
        TempData["ErrorMessage"] = "File not found on storage.";
        return RedirectToAction("Index");
    }
}
```

#### Security Checks

1. **Authentication**: User phải đăng nhập (via [Authorize])
2. **Authorization**: Kiểm tra quyền truy cập file
   - Owner của file: `item.OwnerId == userId`
   - Hoặc được share: Kiểm tra bảng SharedItems
3. **File Type**: Chỉ cho phép download files, không cho folder
4. **Physical File**: Kiểm tra file tồn tại trên disk

---

### 2. Download Qua Link Chia Sẻ (Public Share Download)
**Endpoint**: `GET /Share/Download?token={token}`  
**Controller**: `ShareController.Download()`  
**Authentication**: Not required (public access)

#### Quy Trình Xử Lý (Processing Flow)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User Request: GET /Share/Download?token=xxx                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Validate Token                                                │
│    - Check token is not empty                                   │
│    - await _sharingService.GetSharedItemByTokenAsync(token)     │
│    - Returns null if token invalid/expired                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Check Share Permissions                                       │
│    - Permission level: Viewer, Editor, Owner                    │
│    - AllowDownload flag check                                   │
│    - If Viewer && !AllowDownload → Return 403 Forbid            │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Validate Item Type                                            │
│    - Must be StorageItemType.File                               │
│    - Folders cannot be downloaded directly                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Get Physical File                                             │
│    - await _fileStorageService.GetFileAsync(item.FilePath)      │
│    - Path: wwwroot/uploads/{ownerId}/{uniqueFileName}           │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Log Download (Anonymous)                                      │
│    - Log via _logger with file ID and token                     │
│    - No ActivityLog as user may be anonymous                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Return File Response                                          │
│    - File(fileBytes, mimeType, fileName)                        │
└─────────────────────────────────────────────────────────────────┘
```

#### Code Implementation

```csharp
[HttpGet]
public async Task<IActionResult> Download(string token)
{
    if (string.IsNullOrEmpty(token))
    {
        return NotFound();
    }

    // Step 1: Get shared item by token
    var sharedItem = await _sharingService.GetSharedItemByTokenAsync(token);

    if (sharedItem == null)
    {
        return NotFound();
    }

    // Step 2: Check download permissions
    if (!sharedItem.AllowDownload && sharedItem.Permission == SharePermission.Viewer)
    {
        return Forbid("This share does not allow downloading.");
    }

    var item = sharedItem.StorageItem;
    if (item.Type != StorageItemType.File)
    {
        return BadRequest("Cannot download a folder.");
    }

    try
    {
        // Step 3: Get file bytes
        var fileBytes = await _fileStorageService.GetFileAsync(item.FilePath);
        
        // Step 4: Log download
        _logger.LogInformation("File {FileId} downloaded via share token {Token}", 
            item.Id, token);
        
        // Step 5: Return file
        return File(fileBytes, item.MimeType, item.Name);
    }
    catch (FileNotFoundException)
    {
        return NotFound("File not found.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error downloading shared file {FileId} with token {Token}", 
            item.Id, token);
        return StatusCode(500, "An error occurred while downloading the file.");
    }
}
```

#### Security Checks

1. **Token Validation**: 
   - Token phải hợp lệ và không hết hạn
   - Check qua SharedItems table
2. **Permission Check**:
   - `AllowDownload` flag
   - Permission level (Viewer with AllowDownload=false → Forbid)
3. **Expiration Check**: Token có thể có ExpiresAt
4. **No User Authentication**: Public link không cần đăng nhập

#### Share Permission Levels

| Permission | AllowDownload | Can Download? |
|------------|---------------|---------------|
| Viewer     | false         | ❌ No          |
| Viewer     | true          | ✅ Yes         |
| Editor     | Any           | ✅ Yes         |
| Owner      | Any           | ✅ Yes         |

---

### 3. Download File Từ Thư Mục Được Chia Sẻ (Shared Folder File Download)
**Endpoint**: `GET /Share/DownloadFromFolder?token={token}&itemId={itemId}`  
**Controller**: `ShareController.DownloadFromFolder()`  
**Authentication**: Not required

#### Quy Trình Xử Lý (Processing Flow)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User Request: GET /Share/DownloadFromFolder                  │
│    Parameters: token, itemId                                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Validate Share Token                                          │
│    - await _sharingService.GetSharedItemByTokenAsync(token)     │
│    - Check token valid and not expired                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Check AllowDownload Permission                                │
│    - If !sharedItem.AllowDownload → Return 403                  │
│    - This applies to all permission levels                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Get Requested File Item                                       │
│    - await _storageService.GetItemByIdAsync(itemId)             │
│    - Verify item exists and is a File type                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Verify Folder Hierarchy                                       │
│    - await IsItemWithinSharedFolderAsync(itemId, sharedFolderId)│
│    - Walk up parent folders to verify file is within shared     │
│    - Prevents unauthorized access to files outside share        │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Get Physical File                                             │
│    - await _fileStorageService.GetFileAsync(fileItem.FilePath)  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Log Download                                                  │
│    - Log file ID and token                                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 8. Return File Response                                          │
└─────────────────────────────────────────────────────────────────┘
```

#### Code Implementation

```csharp
[HttpGet]
public async Task<IActionResult> DownloadFromFolder(string token, int itemId)
{
    if (string.IsNullOrEmpty(token))
    {
        return NotFound();
    }

    // Step 1: Get shared item
    var sharedItem = await _sharingService.GetSharedItemByTokenAsync(token);

    if (sharedItem == null)
    {
        return NotFound();
    }

    // Step 2: Check download permission
    if (!sharedItem.AllowDownload)
    {
        return Forbid("This share does not allow downloading.");
    }

    // Step 3: Get the file item
    var fileItem = await _storageService.GetItemByIdAsync(itemId);
    
    if (fileItem == null || fileItem.Type != StorageItemType.File)
    {
        return NotFound("File not found.");
    }

    // Step 4: Verify file is within shared folder
    var isWithinShared = await IsItemWithinSharedFolderAsync(
        itemId, sharedItem.StorageItem.Id);
    if (!isWithinShared)
    {
        return Forbid("This file is not part of the shared folder.");
    }

    try
    {
        var fileBytes = await _fileStorageService.GetFileAsync(fileItem.FilePath);
        
        _logger.LogInformation(
            "File {FileId} downloaded from shared folder via token {Token}", 
            itemId, token);
        
        return File(fileBytes, fileItem.MimeType, fileItem.Name);
    }
    catch (FileNotFoundException)
    {
        return NotFound("File not found.");
    }
}

// Helper method to verify folder hierarchy
private async Task<bool> IsItemWithinSharedFolderAsync(int itemId, int sharedFolderId)
{
    var item = await _storageService.GetItemByIdAsync(itemId);
    
    while (item != null)
    {
        if (item.Id == sharedFolderId)
            return true;
        
        if (!item.ParentFolderId.HasValue)
            return false;
        
        item = await _storageService.GetItemByIdAsync(item.ParentFolderId.Value);
    }
    
    return false;
}
```

#### Security Checks

1. **Token Validation**: Share token must be valid (Share token hợp lệ)
2. **Download Permission**: AllowDownload flag must be true (AllowDownload flag phải là true)
3. **Folder Hierarchy Verification**: 
   - File must be within the shared folder tree (File phải nằm trong cây thư mục được share)
   - Prevents downloading files outside share scope (Ngăn chặn download file ngoài phạm vi share)
4. **File Type Check**: Only allows downloading files (Chỉ cho phép download files)

#### Hierarchy Verification Algorithm

```
Example Folder Structure:
Root (ID: 1)
└── Documents (ID: 2) [SHARED]
    ├── Report.pdf (ID: 3)
    └── SubFolder (ID: 4)
        └── Data.xlsx (ID: 5)

Shared: Documents (ID: 2)

Download Request: itemId = 5
1. Get item 5 → Data.xlsx, ParentFolderId = 4
2. Get item 4 → SubFolder, ParentFolderId = 2
3. Get item 2 → Documents, ID = 2 = sharedFolderId ✅ ALLOW

Download Request: itemId = 99 (outside folder)
1. Get item 99 → OtherFile.txt, ParentFolderId = 10
2. Get item 10 → OtherFolder, ParentFolderId = 1
3. Get item 1 → Root, ParentFolderId = null ❌ DENY
```

---

### 4. Download Phiên Bản Cũ (Version Download)
**Endpoint**: `GET /Version/Download/{id}`  
**Controller**: `VersionController.Download()`  
**Authentication**: Required

#### Quy Trình Xử Lý (Processing Flow)

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User Request: GET /Version/Download/{versionId}              │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. Get File Version                                              │
│    - await _versionService.GetVersionAsync(id)                  │
│    - Include StorageItem navigation property                    │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. Ownership Check                                               │
│    - version.StorageItem.OwnerId == userId                      │
│    - Only file owner can download old versions                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. Check Physical File Exists                                    │
│    - Path: wwwroot/uploads/versions/{version.FilePath}          │
│    - File.Exists(filePath)                                      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. Read File to MemoryStream                                     │
│    - FileStream with Read access                                │
│    - Copy to MemoryStream for response                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Generate Version Filename                                     │
│    - Format: {originalName}_v{versionNumber}.{ext}              │
│    - Example: Report_v2.pdf                                     │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. Return File Response                                          │
│    - File(memoryStream, mimeType, versionedFileName)            │
└─────────────────────────────────────────────────────────────────┘
```

#### Code Implementation

```csharp
[HttpGet]
public async Task<IActionResult> Download(int id)
{
    var userId = _userManager.GetUserId(User);
    var version = await _versionService.GetVersionAsync(id);

    if (version == null || version.StorageItem == null)
    {
        return NotFound();
    }

    // Ownership check
    if (version.StorageItem.OwnerId != userId)
    {
        return Forbid();
    }

    var filePath = Path.Combine(
        _environment.WebRootPath, 
        "uploads", 
        version.FilePath);

    if (!System.IO.File.Exists(filePath))
    {
        _logger.LogError("Version file not found: {FilePath}", filePath);
        return NotFound("Version file not found.");
    }

    // Read to memory stream
    var memory = new MemoryStream();
    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
    {
        await stream.CopyToAsync(memory);
    }
    memory.Position = 0;

    // Generate versioned filename
    var fileName = $"{Path.GetFileNameWithoutExtension(version.StorageItem.Name)}" +
                   $"_v{version.VersionNumber}" +
                   $"{Path.GetExtension(version.StorageItem.Name)}";
                   
    return File(memory, version.MimeType, fileName);
}
```

#### Security Checks

1. **Authentication**: User phải đăng nhập
2. **Ownership**: Chỉ owner của file gốc mới download được versions
3. **Version Exists**: Kiểm tra version record trong database
4. **Physical File**: Kiểm tra file version tồn tại

#### Version Storage

- **Location**: `wwwroot/uploads/versions/{userId}/{uniqueFileName}`
- **Created**: Khi user thay thế file (Replace action)
- **Naming**: Versioned filename với số phiên bản
- **Metadata**: VersionNumber, FileHash, Size, CreatedAt, ChangedBy

---

## Service Layer Analysis

### FileStorageService.GetFileAsync()

```csharp
public async Task<byte[]> GetFileAsync(string filePath)
{
    try
    {
        // Construct full path: wwwroot/uploads/{filePath}
        var fullPath = Path.Combine(_uploadsPath, filePath);
        
        // Security check: file must exist
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        // Read entire file into memory
        return await File.ReadAllBytesAsync(fullPath);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error reading file {FilePath}", filePath);
        throw;
    }
}
```

**Performance Considerations**:
- File được đọc toàn bộ vào memory
- Phù hợp với file nhỏ (<100MB theo giới hạn upload)
- Với file lớn hơn, nên sử dụng Stream-based approach

---

## Activity Logging

### Purpose
- Theo dõi lịch sử download
- Audit trail cho security
- Analytics về file usage

### Implementation

```csharp
await _activityService.LogActivityAsync(
    item.Id,                              // File being downloaded
    userId,                               // User performing action
    ActivityType.FileDownloaded,          // Activity type enum
    $"Downloaded file: {item.Name}",      // Human-readable description
    ipAddress,                            // Client IP
    userAgent                             // Browser/client info
);
```

### Stored Information

| Field | Example | Purpose |
|-------|---------|---------|
| StorageItemId | 123 | Link to file |
| UserId | "abc123..." | Who downloaded |
| ActivityType | FileDownloaded | Action type |
| Timestamp | 2026-01-06 10:30:00 | When |
| IpAddress | 192.168.1.100 | Where from |
| UserAgent | Mozilla/5.0... | Client info |

---

## Error Handling

### Common Error Scenarios

#### 1. File Not Found (404)
**Cause**: File deleted from disk but record exists in DB

```csharp
catch (FileNotFoundException)
{
    TempData["ErrorMessage"] = "File not found on storage.";
    return RedirectToAction("Index");
}
```

**Resolution**: 
- Check file integrity
- Restore from backup
- Update database record

#### 2. Access Forbidden (403)
**Cause**: User không có quyền truy cập

```csharp
var hasAccess = await _storageService.CanUserAccessItemAsync(id, userId);
if (!hasAccess)
{
    return Forbid();
}
```

**Reasons**:
- File không thuộc sở hữu
- Không được share
- Share đã expire
- Permission không cho phép download

#### 3. Invalid Token (404)
**Cause**: Token share không hợp lệ hoặc hết hạn

```csharp
var sharedItem = await _sharingService.GetSharedItemByTokenAsync(token);
if (sharedItem == null)
{
    return NotFound();
}
```

#### 4. Server Error (500)
**Cause**: Lỗi không mong đợi (disk failure, permission issues)

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error downloading file {FileId}", id);
    return StatusCode(500, "An error occurred while downloading the file.");
}
```

---

## Performance Optimization

### Current Implementation
✅ **Strengths**:
- Simple và straightforward
- Sử dụng async/await hiệu quả
- Proper error handling
- Activity logging

❌ **Limitations**:
- Toàn bộ file load vào memory
- Không có caching
- Mỗi request đều query database

### Potential Improvements

#### 1. Stream-based Download (Large Files)
```csharp
public async Task<IActionResult> Download(int id)
{
    // ... validation code ...
    
    var fullPath = Path.Combine(_uploadsPath, item.FilePath);
    var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    
    return File(stream, item.MimeType, item.Name, enableRangeProcessing: true);
}
```

**Benefits**:
- Không load toàn bộ file vào memory
- Hỗ trợ resume download (Range requests)
- Better cho large files

#### 2. Response Caching
```csharp
[ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "id" })]
public async Task<IActionResult> Download(int id)
{
    // ... existing code ...
}
```

#### 3. CDN Integration
- Upload files to Azure Blob Storage / AWS S3
- Generate signed URLs
- Redirect user to CDN URL

#### 4. Download Throttling
```csharp
// Rate limiting per user
var downloadCount = await GetRecentDownloadCount(userId);
if (downloadCount > MAX_DOWNLOADS_PER_HOUR)
{
    return StatusCode(429, "Too many download requests");
}
```

---

## Security Best Practices

### ✅ Currently Implemented

1. **Authentication & Authorization**
   - User authentication via ASP.NET Identity
   - Ownership verification
   - Share permission checks

2. **Path Traversal Prevention**
   - Files stored in user-specific directories
   - No direct path exposure to client
   - All access via database IDs

3. **Token-based Sharing**
   - Secure random tokens
   - Expiration support
   - Permission granularity

4. **Activity Logging**
   - IP address tracking
   - User agent logging
   - Audit trail

### 🔒 Additional Recommendations

1. **Rate Limiting**
   - Limit downloads per user per time period
   - Prevent abuse of public shares

2. **Download Link Expiration**
   - Generate temporary signed URLs
   - URLs expire after X minutes

3. **Virus Scanning**
   - Scan files before download
   - Integrate with antivirus service

4. **Watermarking**
   - Add watermarks to sensitive documents
   - Identify source of leaks

5. **DLP (Data Loss Prevention)**
   - Monitor download patterns
   - Alert on bulk downloads
   - Block download of sensitive files

---

## Database Schema

### StorageItems Table
```sql
CREATE TABLE StorageItems (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(255) NOT NULL,
    Type INT NOT NULL, -- 0: File, 1: Folder
    FilePath NVARCHAR(500), -- Relative path
    Size BIGINT,
    MimeType NVARCHAR(100),
    FileHash VARCHAR(32), -- MD5 hash
    OwnerId NVARCHAR(450) NOT NULL,
    ParentFolderId INT NULL,
    IsPublic BIT DEFAULT 0,
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    ModifiedAt DATETIME2 NOT NULL,
    FOREIGN KEY (OwnerId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (ParentFolderId) REFERENCES StorageItems(Id)
);
```

### SharedItems Table
```sql
CREATE TABLE SharedItems (
    Id INT PRIMARY KEY IDENTITY,
    StorageItemId INT NOT NULL,
    SharedByUserId NVARCHAR(450) NOT NULL,
    SharedWithUserId NVARCHAR(450) NULL,
    SharedWithEmail NVARCHAR(256) NULL,
    Permission INT NOT NULL, -- Viewer, Editor, Owner
    AllowDownload BIT DEFAULT 1,
    AccessToken VARCHAR(100) UNIQUE,
    ExpiresAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL,
    AccessCount INT DEFAULT 0,
    LastAccessedAt DATETIME2 NULL,
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (StorageItemId) REFERENCES StorageItems(Id),
    FOREIGN KEY (SharedByUserId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (SharedWithUserId) REFERENCES AspNetUsers(Id)
);
```

### ActivityLogs Table
```sql
CREATE TABLE ActivityLogs (
    Id INT PRIMARY KEY IDENTITY,
    StorageItemId INT NULL,
    UserId NVARCHAR(450) NOT NULL,
    ActivityType INT NOT NULL, -- Enum
    Description NVARCHAR(500),
    Timestamp DATETIME2 NOT NULL,
    IpAddress VARCHAR(45),
    UserAgent NVARCHAR(500),
    AdditionalData NVARCHAR(1000),
    FOREIGN KEY (StorageItemId) REFERENCES StorageItems(Id),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
);
```

---

## API Endpoints Summary

| Endpoint | Method | Auth | Purpose | Permission |
|----------|--------|------|---------|------------|
| `/Storage/Download/{id}` | GET | Required | Download owned/shared file | Owner or has share |
| `/Share/Download?token={token}` | GET | Optional | Download via public link | Valid token + AllowDownload |
| `/Share/DownloadFromFolder?token={token}&itemId={id}` | GET | Optional | Download file from shared folder | Valid token + file in hierarchy |
| `/Version/Download/{id}` | GET | Required | Download file version | File owner only |
| `/Preview/Download/{id}` | GET | Required | Download after preview | Owner or has share |

---

## Testing Scenarios

### Test Case 1: Owned File Download
```
1. Login as User A
2. Upload file "test.pdf"
3. Navigate to file list
4. Click Download button
5. Verify file downloads successfully
6. Check ActivityLog for FileDownloaded entry
```

### Test Case 2: Shared File Download (With Permission)
```
1. Login as User A
2. Share file with User B (Permission: Viewer, AllowDownload: true)
3. Login as User B
4. Navigate to "Shared with Me"
5. Click Download on shared file
6. Verify download succeeds
```

### Test Case 3: Shared File Download (Without Permission)
```
1. Login as User A
2. Share file with User B (Permission: Viewer, AllowDownload: false)
3. Login as User B
4. Try to download file
5. Verify 403 Forbidden response
```

### Test Case 4: Public Link Download
```
1. Login as User A
2. Create public share link
3. Logout
4. Open share link in incognito browser
5. Click Download button
6. Verify file downloads without login
```

### Test Case 5: Expired Share
```
1. Login as User A
2. Create share with expiration: yesterday
3. Try to access share link
4. Verify 404 Not Found (share expired)
```

### Test Case 6: File Not Found
```
1. Login as User A
2. Upload file
3. Manually delete physical file from uploads folder
4. Try to download file
5. Verify error message: "File not found on storage"
```

### Test Case 7: Version Download
```
1. Login as User A
2. Upload "report.pdf"
3. Replace with new version
4. Navigate to Version History
5. Download previous version
6. Verify filename: "report_v1.pdf"
```

---

## Monitoring & Analytics

### Key Metrics to Track

1. **Download Volume**
   - Total downloads per day/week/month
   - Downloads per user
   - Downloads per file

2. **Performance**
   - Average download time
   - Failed download rate
   - Server response time

3. **Security**
   - Failed access attempts
   - Downloads from unusual IPs
   - Bulk download patterns

4. **Popular Files**
   - Most downloaded files
   - Most shared files
   - File type distribution

### Query Examples

```sql
-- Most downloaded files in last 30 days
SELECT 
    si.Name,
    si.MimeType,
    COUNT(*) as DownloadCount
FROM ActivityLogs al
JOIN StorageItems si ON al.StorageItemId = si.Id
WHERE al.ActivityType = 1 -- FileDownloaded
  AND al.Timestamp >= DATEADD(day, -30, GETUTCDATE())
GROUP BY si.Id, si.Name, si.MimeType
ORDER BY DownloadCount DESC;

-- Download activity by hour
SELECT 
    DATEPART(hour, Timestamp) as Hour,
    COUNT(*) as Downloads
FROM ActivityLogs
WHERE ActivityType = 1
  AND Timestamp >= DATEADD(day, -7, GETUTCDATE())
GROUP BY DATEPART(hour, Timestamp)
ORDER BY Hour;

-- Users with most downloads
SELECT 
    u.Email,
    COUNT(*) as DownloadCount
FROM ActivityLogs al
JOIN AspNetUsers u ON al.UserId = u.Id
WHERE al.ActivityType = 1
  AND al.Timestamp >= DATEADD(day, -30, GETUTCDATE())
GROUP BY u.Id, u.Email
ORDER BY DownloadCount DESC;
```

---

## Conclusion

Hệ thống download của CloudStorage được thiết kế với:

✅ **Security First Approach**
- Multi-layer permission checks
- Token-based public access
- Activity logging and audit trail

✅ **Flexibility**
- Multiple download scenarios
- Granular permission control
- Share expiration support

✅ **Reliability**
- Proper error handling
- File integrity checks
- Comprehensive logging

### Future Enhancements
1. Stream-based downloads for large files
2. Download resume support (HTTP Range)
3. CDN integration
4. Advanced analytics dashboard
5. Download quotas and throttling
6. Virus scanning integration

---

**Document Version**: 1.0  
**Last Updated**: 2026-01-06  
**Author**: CloudStorage Development Team
