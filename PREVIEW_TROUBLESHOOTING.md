# Document Preview - Troubleshooting Steps

## Issue: File type shows "Loading..." and preview shows "File not found"

### Steps to Debug:

#### 1. Stop the running application
Press `Ctrl+C` in the terminal where the app is running, or:
```powershell
Stop-Process -Name "CloudStorage" -Force
```

#### 2. Rebuild and run
```powershell
dotnet build
dotnet run
```

#### 3. Open browser Developer Tools
- Press `F12` in your browser
- Go to the **Console** tab
- Keep it open while testing

#### 4. Try to preview a file
- Click the eye icon (👁️) next to a file
- Watch the console for messages like:
  - `Loading preview for item ID: X`
  - `Preview response:` or `AJAX error:`

#### 5. Check the Network tab
- In Developer Tools, go to **Network** tab
- Click on the preview eye icon
- Look for the request to `/Preview/GetPreview?id=X`
- Click on it and check:
  - **Status**: Should be 200, not 404 or 500
  - **Response**: See what error message is returned

#### 6. Check Application Logs
Look at the terminal where `dotnet run` is executing. You should see logs like:
```
info: CloudStorage.Controllers.PreviewController[0]
      GetPreview: Found item 123, Name=test.pdf, Type=File, FilePath=C:\Project\MyCloudStorage\wwwroot\uploads\...
```

### Common Issues and Fixes:

#### Issue A: FilePath is empty
**Symptom**: Log shows `FilePath=`

**Cause**: Files uploaded before the preview feature was added may have empty FilePath.

**Fix**: Re-upload the file or manually set the FilePath in the database.

#### Issue B: File not found at path
**Symptom**: Log shows `File does not exist at {FilePath}`

**Cause**: The file was moved or deleted from the filesystem.

**Fix**: 
1. Check if the file exists at the path shown in logs
2. Re-upload the file
3. Check the uploads directory: `wwwroot/uploads/{userId}/`

#### Issue C: Permission denied
**Symptom**: 403 error or "Access denied"

**Cause**: Trying to preview someone else's file.

**Fix**: Make sure you're logged in as the file owner.

#### Issue D: Wrong content type
**Symptom**: Controller logs show item Type is not "File"

**Cause**: Trying to preview a folder.

**Fix**: Only files can be previewed, not folders.

### Manual Database Check:

If files were uploaded before the preview feature:

```sql
-- Check StorageItem records
SELECT Id, Name, Type, FilePath, Size, OwnerId 
FROM StorageItems 
WHERE Type = 1 AND IsDeleted = 0
ORDER BY CreatedAt DESC;

-- If FilePath is empty, you may need to reconstruct it
-- Pattern is: wwwroot/uploads/{OwnerId}/{FileName}
```

### Quick Test with Known Working File:

1. Upload a NEW test file (after rebuilding)
2. The upload process should now set FilePath correctly
3. Try to preview this new file
4. If this works, the issue is with old files

### What the Logs Should Show (Normal Operation):

```
[Info] GetPreview: Found item 5, Name=test.docx, Type=File, FilePath=C:\Project\MyCloudStorage\wwwroot\uploads\abc-123\test.docx
[Info] GetPreviewAsync called with filePath: C:\Project\MyCloudStorage\wwwroot\uploads\abc-123\test.docx
[Info] GetPreviewAsync: File exists at C:\Project\MyCloudStorage\wwwroot\uploads\abc-123\test.docx
[Info] GetPreviewAsync: File type is docx
```

### Browser Console Should Show:

```
Loading preview for item ID: 5
Preview response: {success: true, fileName: "test.docx", fileType: "docx", ...}
Displaying preview data: {success: true, ...}
```

### If You Still Have Issues:

1. **Check the actual file path**:
   ```powershell
   Get-ChildItem -Path "wwwroot/uploads" -Recurse -File | Select-Object FullName
   ```

2. **Verify file permissions**: Make sure the app can read files in wwwroot/uploads

3. **Check for path separators**: Windows uses backslashes `\`, ensure paths are consistent

4. **Test with different file types**: Try PDF, Word, Excel, Image, and Text files

5. **Check if the file was actually uploaded**: Look in `wwwroot/uploads/{your-user-id}/`

### Getting More Help:

Please provide:
1. Browser console output (from F12 > Console)
2. Network tab details (from F12 > Network)
3. Application logs (from terminal where `dotnet run` is executing)
4. Result of: `SELECT Id, Name, Type, FilePath FROM StorageItems WHERE Id = X` (replace X with your file ID)
