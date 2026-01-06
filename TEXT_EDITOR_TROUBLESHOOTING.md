# Text Editor Troubleshooting Guide

## Issue: Blue Screen After Clicking Edit Button

### Problem
When clicking the Edit button for a text file, the page shows only the navigation bar and a blue background, but the editor doesn't appear.

### Root Cause
The Edit view had CSS that conflicted with the main layout:
- `html, body { height: 100%; overflow: hidden; }` prevented scrolling
- `container-fluid { height: 100vh; display: flex; }` created layout conflicts
- Fixed height calculations didn't work well with the layout wrapper

### Solution Applied
Fixed the Edit.cshtml view by:

1. **Removed conflicting CSS**:
   - Removed `overflow: hidden` from body
   - Removed flexbox height calculations
   - Used fixed pixel height for the editor (600px)

2. **Simplified layout**:
   - Changed from `calc(100vh - 200px)` to fixed `600px` height
   - Removed flex-grow-1 class
   - Used standard Bootstrap grid without custom height constraints

3. **Improved editor initialization**:
   - Editor now has explicit height: 600px
   - Proper layout() call with timeout for preview toggle
   - Better responsive behavior

### Files Modified
- `Views/Preview/Edit.cshtml` - Fixed layout and CSS issues

### How to Test

1. **Create a test file**:
   - Go to Storage
   - Upload or create a `.txt` file
   - Click the Edit button (pencil icon)

2. **Expected Result**:
   - Page loads with header showing "Edit: filename.txt"
   - Monaco Editor appears with dark theme
   - Save, Preview (for markdown), and Cancel buttons visible
   - Editor displays file content
   - Syntax highlighting works

3. **Verify Editor Features**:
   - Type in the editor - changes should be tracked
   - Press Ctrl+S - file should save
   - Click Save button - should show success message
   - For .md files - Preview button should show split view

### Common Issues and Solutions

#### Issue: Editor still doesn't appear
**Solutions**:
1. Clear browser cache (Ctrl+Shift+Del)
2. Check browser console for errors (F12)
3. Verify Monaco CDN is accessible: https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/loader.min.js
4. Check if JavaScript is enabled

#### Issue: "Item not found" error
**Solutions**:
1. Verify the file exists in the database
2. Check user has permission to edit the file
3. Ensure file ID is valid

#### Issue: Save button doesn't work
**Solutions**:
1. Check browser console for errors
2. Verify anti-forgery token is present
3. Check network tab for failed POST requests
4. Ensure user still has edit permission

#### Issue: Monaco Editor loads but is blank
**Solutions**:
1. Check if file content is being passed correctly
2. Verify `ViewBag.Content` has data
3. Check browser console for serialization errors
4. Try with a small text file first

#### Issue: Height is too small/large
**Solutions**:
You can adjust the editor height in Edit.cshtml:
```html
<!-- Change this line -->
<div id="editor" style="height: 600px; ..."></div>
<!-- To your preferred height, e.g., 800px -->
<div id="editor" style="height: 800px; ..."></div>
```

### Browser Compatibility

**Supported**:
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

**Not Supported**:
- Internet Explorer 11 (Monaco requires modern browsers)

### Performance Tips

1. **Large Files**: Files over 1MB may load slowly
2. **Syntax Highlighting**: Complex files may cause lag
3. **Auto-save**: Currently not implemented (manual save only)

### Debugging Steps

If the editor doesn't load, check these in order:

1. **Open Browser DevTools (F12)**
   - Check Console tab for errors
   - Look for 404 errors loading Monaco
   - Check for JavaScript errors

2. **Verify Request**
   - Network tab should show GET to `/Preview/Edit/{id}`
   - Response should be 200 OK
   - Response should contain HTML with Monaco script tags

3. **Check Monaco Loading**
   - Look for requests to cdnjs.cloudflare.com
   - Verify Monaco loader.min.js loads successfully
   - Check if monaco.editor is defined in console

4. **Verify Content**
   - In console, check: `typeof editor`
   - Should return "object" after page loads
   - Check: `editor.getValue()` to see file content

### Testing Checklist

- [ ] Edit button appears for .txt files
- [ ] Edit button appears for .md files
- [ ] Edit button appears for .js files
- [ ] Edit button does NOT appear for .pdf files
- [ ] Edit button does NOT appear for .jpg files
- [ ] Clicking Edit loads the editor page
- [ ] Editor displays with dark theme
- [ ] File content loads in editor
- [ ] Syntax highlighting works for the file type
- [ ] Save button works
- [ ] Ctrl+S keyboard shortcut works
- [ ] Success message appears after save
- [ ] Cancel button returns to Storage
- [ ] Unsaved changes warning works
- [ ] For markdown: Preview button shows split view
- [ ] For markdown: Preview updates as you type
- [ ] For markdown: Toggle back to code-only works

### Quick Fix Commands

If you need to restart the application:
```powershell
# Stop the app
dotnet build /t:Clean

# Rebuild
dotnet build

# Run again
dotnet run
```

Then navigate to: http://localhost:5000 or https://localhost:5001

### Contact Support

If issues persist:
1. Check [TEXT_EDITOR_FEATURE.md](TEXT_EDITOR_FEATURE.md) for complete documentation
2. Review Controller code in [Controllers/PreviewController.cs](Controllers/PreviewController.cs)
3. Check Service implementation in [Services/FileStorageService.cs](Services/FileStorageService.cs)

---

**Last Updated**: January 5, 2026  
**Status**: ✅ Fixed - Blue screen issue resolved
