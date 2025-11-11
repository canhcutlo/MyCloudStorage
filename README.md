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
- Full-text search across files and folders
- Breadcrumb navigation
- Filter by file type
- Responsive file browser interface

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
│   └── StorageService.cs       # Database operations
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