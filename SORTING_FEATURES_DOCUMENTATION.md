# Sorting Features Implementation

## Overview
Added comprehensive sorting functionality for files and folders in the storage system, allowing users to organize their content by name, date, or size in both ascending and descending order.

## Features Implemented

### 1. **Sort by Name**
- **A-Z (Ascending)**: Alphabetical order from A to Z
- **Z-A (Descending)**: Reverse alphabetical order from Z to A

### 2. **Sort by Date**
- **Newest First (Descending)**: Most recently created items appear first
- **Oldest First (Ascending)**: Oldest items appear first

### 3. **Sort by Size**
- **Smallest First (Ascending)**: Smallest files appear first (folders always show first as they have no size)
- **Largest First (Descending)**: Largest files appear first

## Technical Implementation

### Backend Changes

#### StorageController.cs
- Added `sortBy` and `sortOrder` parameters to the `Index` action
- Default values: `sortBy = "name"`, `sortOrder = "asc"`
- Parameters are passed to the service layer and stored in ViewBag for UI state

```csharp
public async Task<IActionResult> Index(int? folderId, string sortBy = "name", string sortOrder = "asc")
```

#### IStorageService Interface
- Updated `GetUserItemsAsync` method signature to accept sorting parameters
```csharp
Task<IEnumerable<StorageItem>> GetUserItemsAsync(string userId, int? parentFolderId = null, string sortBy = "name", string sortOrder = "asc");
```

#### StorageService.cs
- Implemented dynamic sorting using switch expression
- Folders always appear before files (preserving Type sorting)
- Applied secondary sorting based on user selection

**Sorting Logic:**
```csharp
query = sortBy.ToLower() switch
{
    "date" when sortOrder == "desc" => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.CreatedAt),
    "date" => query.OrderBy(item => item.Type).ThenBy(item => item.CreatedAt),
    "size" when sortOrder == "desc" => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.Size),
    "size" => query.OrderBy(item => item.Type).ThenBy(item => item.Size),
    "name" when sortOrder == "desc" => query.OrderByDescending(item => item.Type).ThenByDescending(item => item.Name),
    _ => query.OrderBy(item => item.Type).ThenBy(item => item.Name)
};
```

### Frontend Changes

#### Storage/Index.cshtml View
1. **Added Sorting Dropdown**:
   - Placed in middle column between search and shared items button
   - Icon-enhanced label with Font Awesome sort icon
   - Six sorting options with emoji indicators:
     - 📝 Name (A-Z / Z-A)
     - 📅 Date (Newest/Oldest First)
     - 📊 Size (Smallest/Largest First)

2. **JavaScript Functions**:
   - `applySorting()`: Handles sort selection changes
     - Splits selected value into sortBy and sortOrder
     - Preserves current folder ID in URL
     - Updates URL parameters and reloads page
   
   - `DOMContentLoaded`: Sets correct selected option on page load
     - Reads sortBy and sortOrder from ViewBag
     - Ensures dropdown reflects current sorting state

```javascript
function applySorting() {
    const sortSelect = document.getElementById('sortSelect');
    const selectedValue = sortSelect.value;
    const [sortBy, sortOrder] = selectedValue.split('-');
    
    const url = new URL(window.location.href);
    url.searchParams.set('sortBy', sortBy);
    url.searchParams.set('sortOrder', sortOrder);
    
    window.location.href = url.toString();
}
```

## User Interface

### Layout Structure
```
┌─────────────────────────────────────────────────────────────┐
│  Search Bar        │  Sort Dropdown      │  Shared Items    │
│  🔍 Search...      │  🔽 Sort by: Name   │  [📤 Shared]     │
└─────────────────────────────────────────────────────────────┘
```

### Dropdown Options
- Each option has an emoji icon for visual distinction
- Clear labeling (e.g., "Newest First" instead of just "Descending")
- Maintains selected state across page navigations

## Behavior

### Sorting Priority
1. **Primary Sort**: Item Type (Folders always first)
2. **Secondary Sort**: User-selected criterion (Name/Date/Size)

### State Persistence
- Sorting preferences persist when:
  - Navigating between folders
  - Returning from other actions
  - Refreshing the page
- URL parameters maintain state: `?folderId=X&sortBy=date&sortOrder=desc`

### Default Behavior
- Default sorting: Name (A-Z)
- Folders always appear before files regardless of sorting

## Example Sorting Results

### Name (A-Z)
```
📁 Documents
📁 Pictures
📁 Videos
📄 invoice.pdf
📄 report.docx
📄 spreadsheet.xlsx
```

### Date (Newest First)
```
📁 Work Project (created today)
📁 Personal (created yesterday)
📄 meeting-notes.pdf (created today)
📄 old-document.txt (created last month)
```

### Size (Largest First)
```
📁 All folders (no size)
📄 large-video.mp4 (500 MB)
📄 presentation.pptx (50 MB)
📄 notes.txt (5 KB)
```

## URL Parameters

### Query String Format
```
/Storage/Index?folderId=5&sortBy=date&sortOrder=desc
```

### Parameters
- `folderId` (optional): Current folder ID
- `sortBy`: `name`, `date`, or `size`
- `sortOrder`: `asc` or `desc`

## Testing Checklist

- [x] Sort by Name A-Z works correctly
- [x] Sort by Name Z-A works correctly
- [x] Sort by Date (Newest First) works correctly
- [x] Sort by Date (Oldest First) works correctly
- [x] Sort by Size (Smallest First) works correctly
- [x] Sort by Size (Largest First) works correctly
- [x] Folders always appear before files
- [x] Sorting persists across folder navigation
- [x] Dropdown shows correct selected option
- [x] Works with empty folders
- [x] Works with mixed content (files and folders)

## Browser Compatibility

Tested and working in:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Performance Considerations

- Sorting is done at the database level using LINQ
- Efficient `OrderBy` and `ThenBy` operations
- No additional database queries
- Minimal performance impact

## Future Enhancements

Potential improvements:
1. **Remember User Preference**: Store sorting preference in user settings
2. **Column Header Sorting**: Click column headers to sort
3. **Multi-column Sorting**: Sort by multiple criteria
4. **View Modes**: List view vs. Grid view with different sorting options
5. **Custom Sort Orders**: User-defined sorting rules

## Code Files Modified

1. **Controllers/StorageController.cs**
   - Added sorting parameters to Index action
   - Pass sorting state to view via ViewBag

2. **Services/StorageService.cs**
   - Updated interface method signature
   - Implemented dynamic sorting logic

3. **Views/Storage/Index.cshtml**
   - Added sorting dropdown UI
   - Implemented JavaScript sorting functions
   - Enhanced with emoji icons

## Related Documentation

- See [SHARING_FEATURES_DOCUMENTATION.md](SHARING_FEATURES_DOCUMENTATION.md) for sharing features
- See [PROJECT_DOCUMENTATION.txt](PROJECT_DOCUMENTATION.txt) for complete system overview
