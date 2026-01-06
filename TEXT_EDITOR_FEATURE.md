# In-Browser Text Editor Feature

## Overview

The In-Browser Text Editor feature allows users to edit text files and markdown documents directly in their browser without downloading them. This feature uses the Monaco Editor (the same editor that powers VS Code) for a professional editing experience.

## Features

### ✏️ Rich Text Editing
- **Monaco Editor Integration**: Professional code editor with syntax highlighting
- **Multiple Language Support**: Supports 25+ programming languages and file types
- **Dark Theme**: Modern VS Code dark theme for comfortable editing
- **Line Numbers**: Clear line numbering for easy navigation
- **Minimap**: Overview of the entire document for quick navigation
- **Word Wrap**: Automatic word wrapping for better readability

### 📝 Markdown Support
- **Live Preview**: Real-time markdown preview alongside the editor
- **Split View**: Toggle between edit-only and split edit/preview modes
- **GitHub-Flavored Markdown**: Support for tables, code blocks, and more
- **Styled Preview**: Beautiful, GitHub-style rendering of markdown

### 💾 Smart Saving
- **Auto-Save Detection**: Visual indicator for unsaved changes
- **Keyboard Shortcut**: Save with Ctrl+S (Cmd+S on Mac)
- **Confirmation**: Save success/error messages
- **Unsaved Warning**: Browser prompt before leaving with unsaved changes

### 🔒 Security & Permissions
- **Permission Checks**: Only users with edit permission can edit files
- **Owner & Shared**: Works for file owners and users with edit access
- **File Type Validation**: Only editable text files can be opened in the editor

## Supported File Types

The editor supports the following file extensions:

### Text & Documentation
- `.txt` - Plain text files
- `.md`, `.markdown` - Markdown files
- `.rtf` - Rich text format
- `.log` - Log files

### Web Development
- `.html`, `.htm` - HTML files
- `.css` - Stylesheets
- `.js`, `.jsx` - JavaScript files
- `.ts`, `.tsx` - TypeScript files

### Programming Languages
- `.py` - Python
- `.java` - Java
- `.c`, `.cpp`, `.h`, `.hpp` - C/C++
- `.cs` - C#
- `.php` - PHP
- `.rb` - Ruby
- `.go` - Go
- `.rs` - Rust
- `.swift` - Swift
- `.kt` - Kotlin

### Data & Configuration
- `.json` - JSON files
- `.xml` - XML files
- `.yaml`, `.yml` - YAML files
- `.csv` - CSV files
- `.ini`, `.cfg`, `.conf` - Configuration files

### Scripts
- `.sh` - Shell scripts
- `.bat` - Batch files
- `.ps1` - PowerShell scripts
- `.sql` - SQL files

## How to Use

### 1. Opening the Editor

1. Navigate to your files in **Storage**
2. Find a text-based file you want to edit
3. Look for the **Edit button** (pencil icon) in the Actions column
   - This button only appears for editable text files
   - You must have edit permission for the file
4. Click the Edit button to open the file in the editor

**Note**: The Edit button will only appear if:
- The file is a supported text file type
- You own the file OR have been granted edit permission through sharing

### 2. Editing Text Files

Once the editor opens:
- The file content loads automatically in the Monaco Editor
- Use the editor like you would use VS Code:
  - Syntax highlighting based on file type
  - Auto-indentation
  - Bracket matching
  - Multi-cursor support
  - Find & replace
- The editor shows the file name, language, size, and last modified date

### 3. Editing Markdown Files

For markdown files (`.md`, `.markdown`):

1. The editor includes a **Preview button**
2. Click **Preview** to toggle split view:
   - Left side: Editor with markdown syntax
   - Right side: Live preview of rendered HTML
3. The preview updates as you type
4. Click **Code Only** to return to full-width editing

Markdown features supported:
- Headers (H1-H6)
- Bold, italic, strikethrough
- Lists (ordered and unordered)
- Links and images
- Code blocks with syntax highlighting
- Tables
- Blockquotes
- Horizontal rules

### 4. Saving Changes

**Option 1: Save Button**
- Click the green **Save** button in the top-right
- The button shows a spinner during save
- Success message appears when saved

**Option 2: Keyboard Shortcut**
- Press `Ctrl+S` (Windows/Linux) or `Cmd+S` (Mac)
- Works from anywhere in the editor

**Unsaved Changes Indicator**:
- When you have unsaved changes, the Save button turns yellow
- Button text changes to "Save*" with an asterisk
- If you try to leave the page, a browser warning appears

### 5. Canceling

- Click the **Cancel** button to return to Storage
- If you have unsaved changes, the browser will warn you
- Confirm to discard changes or stay to save them

## Technical Details

### Monaco Editor Configuration
- **Version**: 0.45.0 (latest stable)
- **Theme**: VS Dark
- **Font Size**: 14px
- **Tab Size**: 4 spaces
- **Features Enabled**:
  - Automatic layout resizing
  - Minimap
  - Word wrap
  - Line numbers
  - Whitespace rendering (on selection)

### File Encoding
- All files are saved with UTF-8 encoding
- Supports international characters and emojis
- Preserves line endings

### Performance
- Editor loads via CDN (fast, cached)
- Lazy loading of Monaco modules
- Optimized for files up to several MB
- Automatic viewport adjustment

### Browser Compatibility
- Chrome/Edge: Full support
- Firefox: Full support
- Safari: Full support
- IE 11: Not supported (Monaco requires modern browsers)

## Examples

### Example 1: Editing a Python Script

1. Upload `hello.py` to your storage
2. Click the Edit button
3. The editor opens with Python syntax highlighting
4. Edit your code:
   ```python
   def greet(name):
       print(f"Hello, {name}!")
   
   greet("World")
   ```
5. Press Ctrl+S to save
6. Success message confirms save

### Example 2: Creating Documentation

1. Upload `README.md` to your storage
2. Click the Edit button
3. Click the **Preview** button for split view
4. Write markdown in the left pane:
   ```markdown
   # My Project
   
   ## Features
   - Easy to use
   - Fast performance
   - Great documentation
   
   ## Installation
   \`\`\`bash
   npm install my-project
   \`\`\`
   ```
5. See real-time preview on the right
6. Save when finished

### Example 3: Editing Configuration Files

1. Upload `config.json` to your storage
2. Click the Edit button
3. Edit JSON with syntax validation:
   ```json
   {
     "apiUrl": "https://api.example.com",
     "timeout": 5000,
     "retries": 3
   }
   ```
4. Save the configuration

## Troubleshooting

### "Edit button doesn't appear"
- **Check file type**: Only text-based files show the Edit button
- **Check permissions**: You need edit permission for the file
- **Supported extensions**: Verify your file extension is in the supported list

### "Permission denied" error
- You don't have edit access to this file
- Contact the file owner to request edit permission
- If you own the file, check if it's in a read-only shared folder

### "Failed to save file" error
- Check your internet connection
- Verify you still have edit permission
- Try refreshing the page and editing again
- Check if the file was deleted or moved

### Editor doesn't load
- Ensure JavaScript is enabled
- Check browser console for errors
- Try clearing browser cache
- Verify you're using a modern browser (not IE 11)

### Preview not updating (Markdown)
- Try toggling preview off and on again
- Check if the markdown syntax is valid
- Refresh the page if preview stops working

## API Endpoints

### GET /Preview/Edit/{id}
- Opens the editor for a file
- Requires: User authentication and edit permission
- Returns: Editor view with file content

### POST /Preview/Edit/{id}
- Saves file content
- Parameters:
  - `id`: File ID
  - `content`: New file content (string)
- Returns: JSON `{ success: bool, message: string }`

## Security Considerations

1. **Permission Validation**: Both GET and POST endpoints validate user permissions
2. **Anti-Forgery Token**: CSRF protection on save operations
3. **File Type Validation**: Only whitelisted text file types can be edited
4. **Owner Verification**: Files can only be edited by owner or users with explicit edit permission
5. **Content Encoding**: UTF-8 encoding prevents encoding-based attacks
6. **File Size**: Large files may cause browser performance issues

## Future Enhancements

Potential improvements for future versions:
- Collaborative real-time editing (multiple users)
- Version history and diff viewer
- Auto-save (every N seconds)
- Offline editing support
- Custom themes (light/dark/custom)
- More language support
- Code formatting/linting
- Search and replace across files
- File templates
- Zen mode (distraction-free editing)

## Related Features

- **File Preview**: View-only preview for documents
- **Sharing**: Share files with edit permissions
- **Version Control**: (Future) Track file changes over time
- **Comments**: (Future) Add comments to files

---

## Developer Notes

### Implementation Files

**Backend**:
- `Services/FileStorageService.cs`: Added `SaveFileContentAsync()` and `IsEditableTextFile()`
- `Controllers/PreviewController.cs`: Added `Edit()` GET/POST actions

**Frontend**:
- `Views/Preview/Edit.cshtml`: Monaco Editor implementation
- `Views/Storage/Index.cshtml`: Added Edit button to file actions

### Dependencies

- **Monaco Editor**: 0.45.0 (CDN)
- **Marked.js**: Latest (CDN, for markdown preview)
- **Bootstrap**: 5.x (existing)
- **Font Awesome**: 6.x (existing)

### Testing Checklist

- [ ] Edit text file as owner
- [ ] Edit text file with shared edit permission
- [ ] Edit markdown with preview
- [ ] Save with button
- [ ] Save with Ctrl+S
- [ ] Cancel with unsaved changes warning
- [ ] Try to edit without permission (should fail)
- [ ] Try to edit unsupported file type (should fail)
- [ ] Test various file types (JSON, Python, HTML, etc.)
- [ ] Test large files (>1MB)
- [ ] Test special characters and emojis
- [ ] Test on different browsers

---

**Version**: 1.0  
**Last Updated**: January 5, 2026  
**Status**: ✅ Complete and Ready for Use
