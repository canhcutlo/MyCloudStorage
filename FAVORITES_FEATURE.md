# Favorites Feature Documentation

## Overview
The Favorites feature allows users to bookmark files and folders for quick access. Users can mark any item they have access to (owned or shared) as a favorite and view all their favorited items in a dedicated Favorites page.

## Key Features

### 1. Toggle Favorite
- Users can add/remove items from favorites by clicking the star button
- Star button appears next to Share/Edit/Delete action buttons
- **Solid star (★)**: Item is in favorites
- **Outline star (☆)**: Item is not in favorites
- Tooltip shows "Add to favorites" or "Remove from favorites"

### 2. Favorites Page
- Accessible via navigation menu: **Storage → Favorites**
- Accessible via button in Storage Index page
- Shows all favorited files and folders
- Statistics displayed:
  - Total favorites count
  - Number of favorited files
  - Number of favorited folders

### 3. Permissions
- Users can favorite any item they have access to:
  - Items they own
  - Items shared with them (any permission level)
- Favorites are **per-user** - each user has their own favorites list
- Removing a favorite doesn't affect the actual file/folder

## Database Schema

### Favorites Table
```sql
CREATE TABLE [Favorites] (
    [Id] INT NOT NULL IDENTITY(1,1),
    [UserId] NVARCHAR(450) NOT NULL,
    [StorageItemId] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Favorites] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Favorites_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
        REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Favorites_StorageItems_StorageItemId] FOREIGN KEY ([StorageItemId]) 
        REFERENCES [StorageItems] ([Id]) ON DELETE NO ACTION
);

-- Unique index to prevent duplicate favorites
CREATE UNIQUE INDEX [IX_Favorites_UserId_StorageItemId] 
    ON [Favorites] ([UserId], [StorageItemId]);
```

## API Endpoints

### Toggle Favorite
**POST** `/Storage/ToggleFavorite`

**Parameters:**
- `id` (int): Storage item ID
- `currentFolderId` (int?, optional): Current folder ID for redirect

**Behavior:**
- If item is not favorited: Adds to favorites
- If item is already favorited: Removes from favorites
- Returns: Redirects back to the referring page

**Authorization:** Requires authenticated user with access to the item

### Get Favorites
**GET** `/Storage/Favorites`

**Returns:** View with all favorited items for the current user

**Authorization:** Requires authenticated user

## Service Methods

### IStorageService

```csharp
Task<bool> ToggleFavoriteAsync(int itemId, string userId);
// Adds/removes item from user's favorites
// Returns: true if operation succeeded, false if item doesn't exist or user lacks access

Task<bool> IsFavoriteAsync(int itemId, string userId);
// Checks if an item is favorited by a user
// Returns: true if favorited, false otherwise

Task<IEnumerable<StorageItem>> GetFavoritesAsync(string userId);
// Gets all favorited items for a user
// Returns: List of StorageItem objects ordered by type and name

Task<Dictionary<int, bool>> GetFavoriteStatusesAsync(IEnumerable<int> itemIds, string userId);
// Gets favorite status for multiple items at once
// Returns: Dictionary mapping item IDs to their favorite status
```

## UI Components

### Favorite Button (Index View)
```html
<form asp-action="ToggleFavorite" method="post" style="display: inline;">
    <input type="hidden" name="id" value="@item.Id" />
    <input type="hidden" name="currentFolderId" value="@currentFolderId" />
    <button type="submit" class="btn btn-sm @(isFavorite ? "btn-warning" : "btn-outline-warning")" 
            title="@(isFavorite ? "Remove from favorites" : "Add to favorites")">
        <i class="@(isFavorite ? "fas" : "far") fa-star"></i>
    </button>
</form>
```

### Navigation Link (Layout)
```html
<li class="nav-item">
    <a class="nav-link text-white" asp-controller="Storage" asp-action="Favorites">
        <i class="fas fa-star"></i> Favorites
    </a>
</li>
```

## Usage Examples

### Example 1: Adding a File to Favorites
1. Navigate to **My Files** or any folder
2. Find the file/folder you want to favorite
3. Click the **star icon (☆)** in the Actions column
4. The star becomes **solid (★)** and a success message appears

### Example 2: Viewing Favorites
1. Click **Favorites** in the navigation menu
2. See all your favorited items in one place
3. Statistics show total count, files, and folders
4. Click folder names to navigate into them
5. Use Preview/Download buttons for files

### Example 3: Removing from Favorites
1. In **Favorites** page or any listing
2. Click the **solid star (★)** on a favorited item
3. The star becomes **outline (☆)** and item is removed from favorites
4. Success message confirms removal

### Example 4: Favoriting Shared Items
1. Navigate to a folder shared with you
2. Click star icon on any file/folder
3. Item is added to your personal favorites
4. Other users' favorites are not affected

## Important Notes

1. **Cascade Deletion**
   - When a user is deleted: All their favorites are automatically deleted (CASCADE)
   - When a storage item is deleted: Favorites referencing it are NOT automatically deleted (NO ACTION)
   - This prevents cascade conflicts with the storage item ownership relationship

2. **Performance Optimization**
   - `GetFavoriteStatusesAsync` retrieves multiple statuses in one query
   - Used in list views to efficiently show star states for all items
   - Unique index ensures no duplicate favorites

3. **Access Control**
   - Users can only favorite items they have access to
   - Validation performed using `CanUserAccessItemAsync`
   - Attempting to favorite inaccessible items returns failure

4. **UI Consistency**
   - Star button appears in all views: Index, Favorites, Search, Shared Items
   - Button styling changes based on favorite status
   - Tooltip provides clear action indication

## Technical Implementation

### Model
```csharp
public class Favorite
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int StorageItemId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public virtual ApplicationUser User { get; set; }
    public virtual StorageItem StorageItem { get; set; }
}
```

### ViewModel Enhancement
```csharp
public class StorageViewModel
{
    // ... existing properties ...
    public Dictionary<int, bool> ItemFavoriteStatuses { get; set; } = new Dictionary<int, bool>();
}
```

### Controller Logic
```csharp
// In Index action
var itemIds = items.Select(i => i.Id).ToList();
var favoriteStatuses = await _storageService.GetFavoriteStatusesAsync(itemIds, userId);
viewModel.ItemFavoriteStatuses = favoriteStatuses;
```

## Future Enhancements

1. **Favorite Collections**: Group favorites into custom collections
2. **Sort Options**: Sort favorites by name, date added, type
3. **Filter Options**: Filter by file type, shared vs owned
4. **Quick Access**: Show recent favorites in dashboard
5. **Favorite Count Badge**: Display count in navigation menu
6. **Bulk Operations**: Select multiple items to favorite/unfavorite at once
7. **Keyboard Shortcuts**: Press 'F' to toggle favorite on selected item

## Troubleshooting

### Problem: "Failed to update favorite" message
**Solution:** Check that:
- User has access to the item
- Item still exists and is not permanently deleted
- Database connection is working

### Problem: Star button not appearing
**Solution:** Verify:
- `ItemFavoriteStatuses` dictionary is populated in view model
- User is authenticated
- View includes the favorite button code

### Problem: Favorites not persisting
**Solution:** Check:
- Database has Favorites table created
- Unique constraint on UserId + StorageItemId exists
- ApplicationDbContext includes `DbSet<Favorite>`

## Related Features
- **Sharing**: Can favorite shared items
- **Trash**: Deleted items are automatically unfavorited (due to cascade)
- **Search**: Search results can include favorite status
- **Permissions**: Respects user access rights
