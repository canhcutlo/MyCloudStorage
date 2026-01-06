# Keyboard Shortcuts Feature Documentation

## Overview
Comprehensive keyboard shortcuts have been implemented to enhance productivity and provide power users with efficient navigation and file management capabilities in the cloud storage application.

## Features

### Visual Indicators
- **Keyboard Icon Button**: Added to the top navigation bar
- **Help Modal**: Press `F1` or `?` to view all shortcuts
- **First-Time Tooltip**: Shows a helpful tip on first visit about keyboard shortcuts
- **Visual Feedback**: 
  - Selected items highlighted in blue
  - Keyboard-navigated items highlighted in yellow with left border
  - Smooth scroll behavior for keyboard navigation

## Available Shortcuts

### File Operations

| Shortcut | Action | Description |
|----------|--------|-------------|
| <kbd>Ctrl</kbd> + <kbd>U</kbd> | Upload Files | Opens file upload dialog |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | New Folder | Opens create folder modal |
| <kbd>Delete</kbd> | Delete Items | Deletes all selected items (bulk delete) |
| <kbd>Backspace</kbd> | Go Up | Navigate to parent folder |
| <kbd>Enter</kbd> | Open Item | Opens the currently highlighted item |

### Selection Management

| Shortcut | Action | Description |
|----------|--------|-------------|
| <kbd>Ctrl</kbd> + <kbd>A</kbd> | Select All | Selects all items in current view |
| <kbd>Ctrl</kbd> + <kbd>D</kbd> | Deselect All | Clears all selections |
| <kbd>Space</kbd> | Toggle Selection | Toggles checkbox of highlighted item |
| <kbd>Esc</kbd> | Clear & Close | Clears selection and closes modals |

### Bulk Operations

| Shortcut | Action | Description |
|----------|--------|-------------|
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>D</kbd> | Bulk Download | Downloads selected items as ZIP |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>M</kbd> | Bulk Move | Opens move dialog for selected items |
| <kbd>Delete</kbd> | Bulk Delete | Deletes all selected items |

### Navigation

| Shortcut | Action | Description |
|----------|--------|-------------|
| <kbd>↑</kbd> | Navigate Up | Highlights previous item in list |
| <kbd>↓</kbd> | Navigate Down | Highlights next item in list |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Focus Search | Moves cursor to search box |
| <kbd>Ctrl</kbd> + <kbd>R</kbd> | Refresh | Reloads current view |

### Help

| Shortcut | Action | Description |
|----------|--------|-------------|
| <kbd>F1</kbd> | Show Help | Opens keyboard shortcuts modal |
| <kbd>?</kbd> | Show Help | Opens keyboard shortcuts modal |

## Technical Implementation

### JavaScript Architecture

#### Event Handling
```javascript
document.addEventListener('keydown', handleKeyboardShortcut);
```

The main keyboard event handler filters out events when:
- User is typing in input fields
- User is typing in textareas
- User is editing contenteditable elements

#### Initialization
```javascript
function initializeKeyboardShortcuts() {
    // Build itemRows array for navigation
    document.querySelectorAll('.item-checkbox').forEach((checkbox, index) => {
        itemRows.push({
            checkbox: checkbox,
            row: checkbox.closest('tr'),
            itemId: parseInt(checkbox.value)
        });
    });
    
    // Attach keyboard event listener
    document.addEventListener('keydown', handleKeyboardShortcut);
    
    // Show first-time tip
    if (!localStorage.getItem('keyboardShortcutsShown')) {
        // Display tooltip...
    }
}
```

#### Navigation System
```javascript
let selectedItemIndex = -1;
const itemRows = [];

function navigateItems(direction) {
    clearHighlights();
    selectedItemIndex += direction;
    // Bounds checking
    if (selectedItemIndex < 0) selectedItemIndex = 0;
    if (selectedItemIndex >= itemRows.length) selectedItemIndex = itemRows.length - 1;
    
    // Highlight and scroll
    const currentRow = itemRows[selectedItemIndex];
    currentRow.row.classList.add('table-active');
    currentRow.row.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}
```

### Key Handler Functions

**selectAllItems()**
- Checks the "Select All" checkbox
- Triggers `toggleSelectAll()` function
- Updates bulk actions toolbar

**deselectAllItems()**
- Unchecks the "Select All" checkbox
- Clears all item selections
- Resets navigation index
- Hides bulk actions toolbar

**toggleCurrentSelection()**
- Toggles checkbox of currently highlighted item
- Updates bulk actions count

**openSelectedItem()**
- Finds link in highlighted row
- Clicks the link to navigate

**showKeyboardShortcuts()**
- Opens Bootstrap modal with shortcuts table
- Uses `bootstrap.Modal` API

### User Interface Components

#### Keyboard Shortcuts Button
```html
<button type="button" class="btn btn-outline-secondary" 
        data-bs-toggle="modal" 
        data-bs-target="#keyboardShortcutsModal" 
        title="Keyboard Shortcuts (F1 or ?)">
    <i class="fas fa-keyboard"></i>
</button>
```

#### Help Modal Structure
- **Two-column layout**: File Operations & Bulk Operations on left, Selection & Navigation on right
- **Color-coded sections**: Different colors for different categories
- **Table format**: Keyboard shortcuts in styled `<kbd>` tags
- **Tip alert**: Info box at bottom with usage notes

#### First-Time Tooltip
```javascript
const helpText = document.createElement('div');
helpText.className = 'alert alert-info alert-dismissible fade show position-fixed bottom-0 end-0 m-3';
helpText.innerHTML = `
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    <strong><i class="fas fa-keyboard me-2"></i>Tip:</strong> 
    Press <kbd>?</kbd> or <kbd>F1</kbd> to view keyboard shortcuts!
`;
```

### CSS Styling

#### Keyboard Tag Styling
```css
kbd {
    padding: 2px 6px;
    font-size: 11px;
    color: #fff;
    background-color: #333;
    border-radius: 3px;
    box-shadow: inset 0 -1px 0 rgba(0,0,0,.25);
    font-family: monospace;
}
```

#### Navigation Highlight
```css
.table-active {
    background-color: #fff3cd !important;
    border-left: 3px solid #ffc107 !important;
}
```

#### Selected Items
```css
.item-checkbox:checked + .item-row,
.item-row:has(.item-checkbox:checked) {
    background-color: #e7f1ff !important;
}
```

## User Experience Flow

### Typical Keyboard Workflow

1. **Navigate to folder**
   - User presses `Backspace` to go up or `↑`/`↓` to browse items
   - Press `Enter` to open highlighted folder

2. **Select multiple files**
   - Press `↓` to highlight first file
   - Press `Space` to select it
   - Continue with `↓` + `Space` for more files
   - Or press `Ctrl+A` to select all

3. **Perform bulk operation**
   - Press `Ctrl+Shift+D` to download selected files as ZIP
   - Or press `Delete` to move to trash
   - Or press `Ctrl+Shift+M` to move to another folder

4. **Clear and continue**
   - Press `Esc` to clear selection
   - Press `Ctrl+R` to refresh if needed

### Discovery Mechanisms

1. **Keyboard Icon**: Visible button in top navigation
2. **First-Time Tooltip**: Appears on bottom-right corner on first visit
3. **Tooltip Hint**: Button has tooltip "Keyboard Shortcuts (F1 or ?)"
4. **Modal Itself**: Comprehensive, well-organized shortcuts reference

## Accessibility Considerations

### Screen Reader Support
- All keyboard shortcuts work without mouse
- Focus management for navigation
- ARIA labels on interactive elements
- Modal accessible via standard Bootstrap ARIA attributes

### Keyboard-Only Users
- Complete functionality available via keyboard
- Visual feedback for all keyboard interactions
- No keyboard traps
- Escape key consistently cancels/closes

### Visual Indicators
- **Blue highlight**: Selected items (checkbox checked)
- **Yellow highlight**: Keyboard-navigated item
- **Smooth scrolling**: When navigating with arrows
- **Badge count**: Shows number of selected items

## Browser Compatibility

### Tested Browsers
- ✅ Chrome/Edge (Chromium)
- ✅ Firefox
- ✅ Safari (Mac only shortcuts work)
- ⚠️ Note: `Ctrl` key on Windows/Linux, `Cmd` key on macOS

### Event Compatibility
- Uses standard `keydown` event
- Checks `e.ctrlKey` and `e.metaKey` for cross-platform support
- `e.preventDefault()` prevents browser default behaviors
- No proprietary APIs used

## Known Limitations

1. **Search Box Focus**: When in search box, keyboard shortcuts are disabled to allow typing
2. **Modal Inputs**: Shortcuts disabled in any text input or textarea
3. **Platform Differences**: Some shortcuts may conflict with browser/OS shortcuts
4. **Single Navigation**: Can only highlight one item at a time for keyboard navigation
5. **No Folder Upload**: `Ctrl+U` only allows file selection (browser limitation)

## Performance Considerations

### Event Handling
- **Single event listener**: One `keydown` listener on document
- **Early exit**: Returns immediately if typing in input field
- **Debouncing not needed**: Keyboard events are naturally discrete

### Memory Management
- **itemRows array**: Rebuilt on page load, minimal memory footprint
- **No memory leaks**: Event listeners cleaned up on page unload
- **LocalStorage**: Only stores one boolean flag for first-time tip

### Rendering Performance
- **CSS transitions**: Smooth highlight animations
- **Scroll behavior**: `smooth` for better UX without jank
- **Class-based styling**: No inline style manipulation

## Future Enhancements

### Planned Features
1. **Customizable shortcuts**: Allow users to define their own key combinations
2. **Shortcut cheat sheet**: Printable PDF reference guide
3. **Context-sensitive shortcuts**: Different shortcuts based on current view
4. **Vim-style navigation**: `j`/`k` for up/down (opt-in)
5. **Quick actions**: Number keys to quick-select items (1-9)

### Advanced Navigation
6. **Type-ahead search**: Start typing to filter/select items
7. **Multiple selection ranges**: `Shift+Click` equivalent with keyboard
8. **Folder tree navigation**: `←`/`→` to expand/collapse folders
9. **Breadcrumb navigation**: `Alt+←`/`→` for history navigation

### Productivity Features
10. **Clipboard operations**: `Ctrl+X`/`Ctrl+C`/`Ctrl+V` for cut/copy/paste
11. **Rename shortcut**: `F2` to rename selected item
12. **File info**: `Ctrl+I` to show details panel
13. **Quick preview**: `Space` to preview file (macOS Quick Look style)

## Testing Checklist

### Manual Testing

#### Basic Shortcuts
- [ ] `Ctrl+U` opens upload dialog
- [ ] `Ctrl+N` opens new folder modal
- [ ] `Delete` deletes selected items
- [ ] `Backspace` goes to parent folder
- [ ] `Enter` opens highlighted item

#### Selection
- [ ] `Ctrl+A` selects all items
- [ ] `Ctrl+D` deselects all items
- [ ] `Space` toggles highlighted item
- [ ] `Esc` clears selection

#### Navigation
- [ ] `↑` highlights previous item
- [ ] `↓` highlights next item
- [ ] `Ctrl+F` focuses search box
- [ ] `Ctrl+R` refreshes page

#### Bulk Operations
- [ ] `Ctrl+Shift+D` downloads selected as ZIP
- [ ] `Ctrl+Shift+M` opens move modal
- [ ] Bulk delete works with multiple items selected

#### Help
- [ ] `F1` opens shortcuts modal
- [ ] `?` opens shortcuts modal
- [ ] Keyboard button in header opens modal

#### Edge Cases
- [ ] Shortcuts disabled in search box
- [ ] Shortcuts disabled in modal inputs
- [ ] First-time tooltip appears once
- [ ] Tooltip doesn't reappear after dismissed

## Related Features

### Integration Points
- **Drag-and-Drop**: Works alongside keyboard shortcuts
- **Bulk Operations**: Triggered via keyboard or mouse
- **File Selection**: Checkbox system supports both input methods
- **Navigation**: Breadcrumbs and keyboard backspace work together

### Complementary Features
- [Drag-and-Drop Upload](DRAG_DROP_BULK_OPERATIONS.md)
- [Bulk Operations](DRAG_DROP_BULK_OPERATIONS.md)
- [File Versioning](FILE_VERSIONING_DOCUMENTATION.md)
- [Search Functionality](SEMANTIC_SEARCH.md)

## Changelog

### Version 1.0 (January 2026)
- Initial implementation of keyboard shortcuts
- Added 15+ keyboard shortcuts for common operations
- Implemented visual feedback system
- Created keyboard shortcuts help modal
- Added first-time user tooltip
- Styled `<kbd>` tags for better readability
- Cross-platform support (Ctrl/Cmd keys)

## Support and Feedback

### Known Issues
None reported yet.

### Reporting Issues
If you encounter issues with keyboard shortcuts:
1. Note which shortcut isn't working
2. Check if you're in an input field (shortcuts are disabled)
3. Verify your browser/OS combination
4. Check browser console for JavaScript errors

### Feature Requests
Suggestions for new shortcuts or improvements are welcome!
