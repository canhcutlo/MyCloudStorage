# CloudStorage - ASP.NET MVC Cloud Storage Application

A modern cloud storage application built with ASP.NET MVC, similar to Google Drive, TeraBox, or FShare. This application provides secure file storage, sharing capabilities, and a responsive web interface.

## Features

### 🔐 User Management
- User registration and authentication
- Secure login with ASP.NET Core Identity
- User profile management
- Storage quota tracking (5GB default per user)

### 📁 File & Folder Management
- Upload files (up to 100MB per file)
- Create and organize folders
- Rename files and folders
- Delete files and folders (soft delete)
- Move items between folders
- File integrity checking with MD5 hash
- Support for various file types with MIME type detection

### 🔍 Search & Navigation
- **Semantic Search** with Vietnamese language support
  - Understands Vietnamese accents and diacritics
  - Matches files written without accents (e.g., "Báo cáo tài chính" → "Bao_cao_tai_chinh.pdf")
  - Recognizes common abbreviations (T10 → tháng 10, BC → báo cáo, TC → tài chính)
  - Token-based similarity matching with relevance scoring
  - Number-aware search for dates and quarters
- Breadcrumb navigation
- Filter by file type (Files/Folders)
- Responsive file browser interface
- Sort results by relevance

### 🤝 Sharing Capabilities
- Share files and folders with other users
- Create public share links
- Set share permissions (View, Download, Edit, Full Access)
- Set expiration dates for shares
- Manage shared items

### 📊 Storage Analytics
- Real-time storage usage tracking
- Visual storage quota indicators
- File and folder count statistics

## Technology Stack

- **Framework**: ASP.NET Core 9.0 MVC
- **Authentication**: ASP.NET Core Identity
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: Bootstrap 5, Font Awesome icons
- **File Storage**: Local file system (configurable)

## Prerequisites

- .NET 9.0 SDK or later
- SQL Server (LocalDB for development)
- Visual Studio Code or Visual Studio

## Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd MyCloudStorage
```

### 2. Setup Database
The application uses SQL Server LocalDB by default. The database will be created automatically when you first run the application.

To use a different SQL Server instance, update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Connection-String-Here"
  }
}
```

### 3. Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

The application will be available at `http://localhost:5000` or `https://localhost:5001`.

### 4. First Time Setup
1. Navigate to the application URL
2. Click "Register" to create a new account
3. Fill in your details and create an account
4. You'll be automatically logged in and redirected to your storage dashboard

## Project Structure

```
CloudStorage/
├── Controllers/           # MVC Controllers
│   ├── AccountController.cs    # Authentication & user management
│   ├── HomeController.cs       # Home page routing
│   ├── ShareController.cs      # Public sharing functionality
│   └── StorageController.cs    # Main storage operations
├── Data/                 # Database context and configuration
│   └── ApplicationDbContext.cs
├── Models/               # Data models and view models
│   ├── ApplicationUser.cs      # User entity
│   ├── StorageItem.cs          # File/folder entity
│   ├── SharedItem.cs           # Sharing entity
│   └── ViewModels/             # View models for UI
├── Services/             # Business logic services
│   ├── FileStorageService.cs   # File system operations
│   ├── SharingService.cs       # Sharing functionality
│   ├── StorageService.cs       # Database operations
│   ├── SemanticSearchService.cs # Intelligent search engine
│   └── GeminiAIService.cs      # AI integration
├── Views/                # Razor views
│   ├── Account/               # Authentication views
│   ├── Share/                 # Public sharing views
│   ├── Storage/               # Main application views
│   └── Shared/                # Layout and shared views
└── wwwroot/              # Static files and uploads
```

## Key Features Explained

### Storage Management
- Files are stored in the `wwwroot/uploads/{userId}` directory
- Each user has their own subdirectory for security
- File integrity is ensured using MD5 hash verification
- Soft delete functionality allows for data recovery

### Security Features
- All file operations require user authentication
- Users can only access their own files unless explicitly shared
- Public shares use secure tokens for access control
- File paths are abstracted to prevent direct access

### Sharing System
- Multiple sharing options: specific users or public links
- Granular permissions: View Only, Download, Edit, Full Access
- Time-limited shares with automatic expiration
- Share management dashboard for tracking active shares

### Database Design
- User data managed by ASP.NET Core Identity
- Storage items support hierarchical folder structure
- Sharing system with flexible permission model
- Optimized queries with proper indexing

### Semantic Search Engine
- **Vietnamese Language Support**: Comprehensive accent normalization covering 60+ characters
- **Abbreviation Recognition**: Understands 25+ common business abbreviations (T10, BC, TC, HD, etc.)
- **Token-Based Matching**: Intelligent word segmentation and comparison
- **Similarity Scoring**: Jaccard coefficient with substring and number bonuses
- **Flexible Separators**: Handles underscores, hyphens, dots, and mixed formats
- **Example**: Search "Báo cáo tài chính tháng 10" finds "Bao_cao_tai_chinh_T10.pdf"

See [SEMANTIC_SEARCH.md](SEMANTIC_SEARCH.md) for detailed documentation.

## Documentation

### Project Documentation Files
- **[VIEWS_DIRECTORY_DOCUMENTATION.md](VIEWS_DIRECTORY_DOCUMENTATION.md)** - Detailed explanation of the Views directory structure (Vietnamese)
- **[SEMANTIC_SEARCH.md](SEMANTIC_SEARCH.md)** - Semantic search implementation and features
- **[DARK_MODE_DOCUMENTATION.md](DARK_MODE_DOCUMENTATION.md)** - Dark mode feature documentation
- **[FILE_VERSIONING_DOCUMENTATION.md](FILE_VERSIONING_DOCUMENTATION.md)** - File versioning system
- **[GROUP_SHARING_FEATURE.md](GROUP_SHARING_FEATURE.md)** - Group sharing functionality
- **[KEYBOARD_SHORTCUTS_DOCUMENTATION.md](KEYBOARD_SHORTCUTS_DOCUMENTATION.md)** - Keyboard shortcuts guide
- **[TRASH_FEATURE.md](TRASH_FEATURE.md)** - Trash/recycle bin feature
- And more feature-specific documentation files...

## Configuration

### File Upload Limits
Modify in `Program.cs`:
```csharp
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});
```

### Storage Quotas
Update in `ApplicationUser.cs`:
```csharp
public long StorageQuota { get; set; } = 5_000_000_000; // 5GB in bytes
```

### Database Provider
The application uses SQL Server by default. To use a different provider:
1. Install the appropriate NuGet package
2. Update the connection string in `appsettings.json`
3. Modify the context configuration in `Program.cs`

## Development

### Running in Development Mode
```bash
dotnet run --environment Development
```

### Database Migrations
If you make changes to the models:
```bash
# Add migration
dotnet ef migrations add YourMigrationName

# Update database
dotnet ef database update
```

### Building for Production
```bash
dotnet publish -c Release -o ./publish
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support, please open an issue in the GitHub repository or contact the development team.

---

## Quick Start Commands

```bash
# Clone and setup
git clone <repo-url>
cd MyCloudStorage

# Build and run
dotnet build
dotnet run

# Access application
# Navigate to http://localhost:5000
```

Enjoy using CloudStorage! 🚀
\n+## 🤖 AI Features (Gemini Integration)

### Overview
The application integrates Google Gemini (`gemini-2.5-flash`) to provide:
- Prompt-based folder & file generation
- Automatic file classification on upload (Images, Documents, Audio, Video, Archives, Code, Other)

### Configuration
Do NOT hard-code your API key. Use User Secrets or environment variables.

Using .NET User Secrets (development only):
```bash
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "YOUR_REAL_API_KEY"
```
Ensure `appsettings.Development.json` contains the placeholder section:
```json
"Gemini": { "ApiKey": "YOUR_GEMINI_API_KEY" }
```
In production, set an environment variable:
```powershell
$Env:Gemini__ApiKey="YOUR_REAL_API_KEY"
```

### Endpoints / Usage
| Feature | URL | Description |
|---------|-----|-------------|
| AI Folder Generation | `/Storage/AICreateFolder` | Enter a prompt (e.g., "tạo một folder trong đó có chứa 10 file text được đặt tên từ 1 - 10") to auto-create a folder and files. |
| Auto Classification | Upload any file at root | AI classifies and moves it into category folder automatically (created if missing). |

### Folder Generation Prompt Format
You can request structured folder contents, e.g.:
```
tạo một folder đặt tên "Tutorial" trong đó có 3 file text: intro.txt, steps.txt, summary.txt
```
AI will output a JSON plan; the server converts it into physical folder + files.

### Error Handling & Fallbacks
- If Gemini response parsing fails, the system falls back to minimal defaults.
- Classification defaults to `Other` if AI call fails.

### Security Notes
- Keep the API key secret and never commit it.
- Rate limit / caching can be added around AI calls for efficiency.

### Extending
- Enhance classification with custom taxonomy.
- Support more structured generation (Markdown, code snippets, etc.).
- Add more AI-powered features for file organization.

---