# GIẢI THÍCH CHI TIẾT THư MỤC VIEWS (Views Directory Documentation)

## Tổng Quan (Overview)

Thư mục `Views` là một phần quan trọng trong kiến trúc ASP.NET MVC, chứa tất cả các file Razor (`.cshtml`) dùng để render giao diện người dùng. Đây là nơi xác định cách dữ liệu từ Controllers được hiển thị cho người dùng cuối.

### Vị Trí
```
MyCloudStorage/
└── Views/
    ├── Account/              # Các view cho xác thực và quản lý tài khoản
    ├── Activity/             # Các view cho hoạt động và lịch sử
    ├── Groups/               # Các view cho quản lý nhóm
    ├── Home/                 # Các view cho trang chủ
    ├── Preview/              # Các view cho xem trước file
    ├── Share/                # Các view cho chia sẻ công khai
    ├── Shared/               # Các view được chia sẻ (Layout, Partial Views)
    ├── Storage/              # Các view cho quản lý lưu trữ file
    ├── Version/              # Các view cho lịch sử phiên bản file
    ├── _ViewImports.cshtml   # Import các namespace và Tag Helpers
    └── _ViewStart.cshtml     # Cấu hình layout mặc định
```

---

## 1. Thư Mục Account (Authentication & User Management)

**Mục đích**: Quản lý tất cả các giao diện liên quan đến xác thực người dùng và quản lý tài khoản.

### Các File View:

#### 1.1. `Login.cshtml`
- **Chức năng**: Trang đăng nhập
- **Model**: `CloudStorage.Models.ViewModels.LoginViewModel`
- **Tính năng**:
  - Form đăng nhập với email và mật khẩu
  - Toggle hiển thị/ẩn mật khẩu
  - Checkbox "Remember me"
  - Link quên mật khẩu
  - Giao diện phân chia hai panel (trái: giới thiệu, phải: form đăng nhập)
- **Thiết kế đặc biệt**: 
  - Panel trái hiển thị logo, slogan và các tính năng nổi bật
  - Panel phải chứa form đăng nhập
  - Giao diện hiện đại với gradient và animation

#### 1.2. `Register.cshtml`
- **Chức năng**: Trang đăng ký tài khoản mới
- **Model**: `CloudStorage.Models.ViewModels.RegisterViewModel`
- **Tính năng**:
  - Form đăng ký với các trường: Email, Username, Password, Confirm Password
  - Validation dữ liệu nhập
  - Toggle hiển thị/ẩn mật khẩu
  - Link đến trang đăng nhập
- **Đặc điểm**: Giao diện tương tự Login.cshtml với bố cục hai panel

#### 1.3. `ForgotPassword.cshtml`
- **Chức năng**: Trang yêu cầu đặt lại mật khẩu
- **Tính năng**:
  - Form nhập email để nhận link đặt lại mật khẩu
  - Gửi email xác nhận đến người dùng
  - Validation email

#### 1.4. `ForgotPasswordConfirmation.cshtml`
- **Chức năng**: Trang xác nhận đã gửi email đặt lại mật khẩu
- **Nội dung**: Thông báo cho người dùng kiểm tra email

#### 1.5. `ResetPassword.cshtml`
- **Chức năng**: Trang đặt lại mật khẩu mới
- **Tính năng**:
  - Form nhập mật khẩu mới và xác nhận mật khẩu
  - Token xác thực từ email
  - Validation mật khẩu mạnh

#### 1.6. `ResetPasswordConfirmation.cshtml`
- **Chức năng**: Trang xác nhận mật khẩu đã được đặt lại thành công
- **Nội dung**: Thông báo thành công và link đến trang đăng nhập

#### 1.7. `ResetPasswordError.cshtml`
- **Chức năng**: Trang hiển thị lỗi khi đặt lại mật khẩu
- **Nội dung**: Thông báo lỗi và hướng dẫn thử lại

---

## 2. Thư Mục Activity (Activity Feed & History)

**Mục đích**: Hiển thị các hoạt động và lịch sử thao tác của người dùng.

### Các File View:

#### 2.1. `Index.cshtml`
- **Chức năng**: Trang chính hiển thị tất cả hoạt động
- **Tính năng**:
  - Timeline hiển thị các hoạt động (upload, download, share, delete, etc.)
  - Filter theo loại hoạt động
  - Phân trang
  - Hiển thị thời gian và chi tiết hoạt động
  - Icon phân biệt các loại hoạt động khác nhau

#### 2.2. `Recent.cshtml`
- **Chức năng**: Hiển thị các hoạt động gần đây
- **Tính năng**:
  - Danh sách các hoạt động mới nhất
  - Sắp xếp theo thời gian giảm dần
  - Quick actions từ activity list
  - Real-time updates (nếu được cấu hình)

---

## 3. Thư Mục Groups (Group Management)

**Mục đích**: Quản lý các nhóm người dùng để chia sẻ file và cộng tác.

### Các File View:

#### 3.1. `Index.cshtml`
- **Chức năng**: Trang danh sách tất cả các nhóm
- **Tính năng**:
  - Hiển thị các nhóm mà người dùng là thành viên
  - Nút tạo nhóm mới
  - Hiển thị số thành viên trong mỗi nhóm
  - Các actions: View, Edit, Manage Members, Delete

#### 3.2. `Create.cshtml`
- **Chức năng**: Trang tạo nhóm mới
- **Tính năng**:
  - Form nhập tên nhóm và mô tả
  - Chọn thành viên ban đầu
  - Validation dữ liệu

#### 3.3. `Edit.cshtml`
- **Chức năng**: Trang chỉnh sửa thông tin nhóm
- **Tính năng**:
  - Cập nhật tên và mô tả nhóm
  - Chỉ admin nhóm mới có quyền chỉnh sửa

#### 3.4. `ManageMembers.cshtml`
- **Chức năng**: Quản lý thành viên trong nhóm
- **Tính năng**:
  - Danh sách thành viên hiện tại
  - Thêm thành viên mới
  - Xóa thành viên
  - Phân quyền thành viên (Admin, Member)
  - Search người dùng để thêm vào nhóm

---

## 4. Thư Mục Home (Home Pages)

**Mục đích**: Các trang chính của ứng dụng.

### Các File View:

#### 4.1. `Index.cshtml`
- **Chức năng**: Trang chủ của ứng dụng
- **Nội dung**:
  - Welcome message
  - Giới thiệu tính năng
  - Link đến tài liệu hướng dẫn
- **Đặc điểm**: Trang đơn giản, thường được thay thế bởi Storage/Index khi người dùng đã đăng nhập

#### 4.2. `Privacy.cshtml`
- **Chức năng**: Trang chính sách bảo mật
- **Nội dung**: Thông tin về quyền riêng tư và chính sách sử dụng dữ liệu

---

## 5. Thư Mục Preview (File Preview)

**Mục đích**: Xem trước nội dung file trực tiếp trên trình duyệt.

### Các File View:

#### 5.1. `Document.cshtml`
- **Chức năng**: Xem trước tài liệu (PDF, Word, Excel, PowerPoint)
- **Tính năng**:
  - Hiển thị document trong iframe hoặc viewer
  - Hỗ trợ nhiều định dạng: PDF, DOCX, XLSX, PPTX
  - Zoom in/out
  - Download document
  - Share và comment trực tiếp từ preview
  - Navigation giữa các trang (cho PDF)

#### 5.2. `Edit.cshtml`
- **Chức năng**: Chỉnh sửa file text trực tiếp trên trình duyệt
- **Tính năng**:
  - Text editor với syntax highlighting
  - Hỗ trợ các file: TXT, MD, JSON, XML, HTML, CSS, JS, etc.
  - Save changes
  - Line numbers
  - Search and replace
  - Auto-save draft

#### 5.3. `Video.cshtml`
- **Chức năng**: Xem trước video
- **Tính năng**:
  - Video player tích hợp
  - Play/pause controls
  - Volume control
  - Fullscreen mode
  - Timeline seeking
  - Hỗ trợ: MP4, WebM, AVI, MOV

---

## 6. Thư Mục Share (Public Sharing)

**Mục đích**: Các trang để người dùng không đăng nhập truy cập nội dung được chia sẻ công khai.

### Các File View:

#### 6.1. `PublicFile.cshtml`
- **Chức năng**: Xem và tải file được chia sẻ công khai
- **Tính năng**:
  - Hiển thị thông tin file (tên, kích thước, loại file)
  - Preview file (nếu supported)
  - Download button
  - Kiểm tra expiration date
  - Không yêu cầu đăng nhập

#### 6.2. `PublicFolder.cshtml`
- **Chức năng**: Duyệt thư mục được chia sẻ công khai
- **Tính năng**:
  - Danh sách file và folder trong thư mục shared
  - Navigation giữa các folder con
  - Download individual files
  - Bulk download (ZIP)
  - Breadcrumb navigation

#### 6.3. `ShareError.cshtml`
- **Chức năng**: Hiển thị lỗi khi truy cập share link
- **Các trường hợp lỗi**:
  - Link đã hết hạn
  - Link không tồn tại
  - Link đã bị vô hiệu hóa
  - Không có quyền truy cập

---

## 7. Thư Mục Shared (Shared Views & Layouts)

**Mục đích**: Chứa các view được sử dụng chung trên toàn bộ ứng dụng.

### Các File View:

#### 7.1. `_Layout.cshtml` (Layout Chính)
- **Chức năng**: Template chính cho tất cả các trang
- **Cấu trúc**:
  ```html
  <!DOCTYPE html>
  <html>
  <head>
      <!-- Meta tags, CSS, Scripts -->
  </head>
  <body>
      <!-- Navigation Bar -->
      <header>
          <nav class="navbar">
              <!-- Logo, Menu items, User dropdown -->
          </nav>
      </header>
      
      <!-- Main Content Area -->
      <div class="container">
          <main>
              @RenderBody() <!-- Nội dung từ các view con -->
          </main>
      </div>
      
      <!-- Footer -->
      <footer>
          <!-- Copyright, Links -->
      </footer>
      
      <!-- Modals (Share Modal) -->
      @await Html.PartialAsync("_ShareModal")
      
      <!-- Scripts -->
      <script src="..."></script>
      @await RenderSectionAsync("Scripts", required: false)
  </body>
  </html>
  ```

- **Tính năng Navigation Bar**:
  - Logo và brand name
  - Menu items: My Files, Favorites, Shared, Groups, Activity, Trash
  - AI dropdown menu
  - Theme toggle (Dark/Light mode)
  - User dropdown: Settings, Logout
  - Responsive design (collapse menu on mobile)

- **Conditional Rendering**:
  - Hiển thị navigation bar khác nhau cho authenticated/non-authenticated users
  - Ẩn navbar hoàn toàn cho các trang authentication (Login, Register)

#### 7.2. `_Layout.cshtml.css`
- **Chức năng**: CSS riêng cho layout (scoped CSS)
- **Nội dung**: Styles đặc thù cho layout component

#### 7.3. `_ShareModal.cshtml` (Partial View)
- **Chức năng**: Modal popup để chia sẻ file/folder
- **Tính năng**:
  - Form chia sẻ với người dùng cụ thể hoặc tạo public link
  - Chọn permissions: View, Download, Edit, Full Access
  - Set expiration date
  - Copy share link
  - Manage existing shares
  - Email notification option

#### 7.4. `_CommentSection.cshtml` (Partial View)
- **Chức năng**: Component hiển thị comments cho file/folder
- **Tính năng**:
  - Danh sách comments
  - Form thêm comment mới
  - Reply to comments (threaded comments)
  - Edit/Delete own comments
  - Real-time comment updates
  - User avatars và timestamps

#### 7.5. `_ValidationScriptsPartial.cshtml`
- **Chức năng**: Scripts cho client-side validation
- **Nội dung**:
  ```cshtml
  <script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
  <script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
  ```

#### 7.6. `Error.cshtml`
- **Chức năng**: Trang hiển thị lỗi chung
- **Tính năng**:
  - Hiển thị error message
  - Request ID để trace lỗi
  - Link quay về trang chủ
  - Developer exception page (development mode)

---

## 8. Thư Mục Storage (Main Storage Management)

**Mục đích**: Các view chính để quản lý file và folder - core functionality của ứng dụng.

### Các File View:

#### 8.1. `Index.cshtml` (Main Storage View)
- **Chức năng**: Trang chính hiển thị file và folder
- **Model**: `CloudStorage.Models.ViewModels.StorageViewModel`
- **Tính năng Chính**:

  **A. Storage Usage Dashboard**:
  - Hiển thị dung lượng đã sử dụng / tổng quota
  - Số lượng files và folders
  - Progress bar trực quan

  **B. Breadcrumb Navigation**:
  - Điều hướng qua các folder level
  - Icon cho shared folders
  - Link "Home" về root

  **C. Search và Sorting**:
  - Search box với semantic search
  - Sort by: Name, Date, Size (ascending/descending)
  - Filter options

  **D. Drag & Drop Upload Zone**:
  - Kéo thả file để upload
  - Multiple file upload
  - Progress bar cho upload
  - Visual feedback (drag-over effect)

  **E. File/Folder Table**:
  - Checkbox cho bulk selection
  - Icons phân biệt file types và folders
  - Shared indicator icon
  - Size và modification date
  - Actions dropdown menu

  **F. Actions Dropdown**:
  - **For Files**: Preview, Download, Version History, Replace, Edit, Share, Comments, Rename, Delete
  - **For Folders**: Open, Share, Comments, Rename, Delete
  - Conditional actions based on permissions

  **G. Bulk Operations Toolbar**:
  - Hiển thị khi có items được select
  - Bulk Download (ZIP)
  - Bulk Move
  - Bulk Delete
  - Clear selection

  **H. Keyboard Shortcuts**:
  - Ctrl+A: Select all
  - Ctrl+U: Upload
  - Delete: Delete selected
  - Arrow keys: Navigate
  - Space: Toggle selection
  - Enter: Open item
  - F1 hoặc ?: Show shortcuts help
  - Ctrl+F: Focus search
  - Backspace: Parent folder
  - Và nhiều shortcuts khác...

  **I. Modals**:
  - Comments Modal
  - Keyboard Shortcuts Help Modal

- **JavaScript Functions** (embedded in view):
  - `applySorting()`: Apply sort preferences
  - `openCommentsModal()`: Open comments for item
  - `uploadFiles()`: Handle drag-drop upload
  - `bulkDelete()`, `bulkDownload()`, `bulkMove()`: Bulk operations
  - `initializeKeyboardShortcuts()`: Setup keyboard navigation
  - `toggleSelectAll()`, `updateBulkActions()`: Manage selection

#### 8.2. `Upload.cshtml`
- **Chức năng**: Trang upload file (alternative to drag-drop)
- **Tính năng**:
  - File input với multiple selection
  - Target folder selection
  - Upload progress
  - File size validation
  - MIME type validation

#### 8.3. `CreateFolder.cshtml`
- **Chức năng**: Trang tạo folder mới
- **Tính năng**:
  - Form nhập tên folder
  - Parent folder selection
  - Validation tên folder (không trùng, không ký tự đặc biệt)

#### 8.4. `AICreateFolder.cshtml`
- **Chức năng**: Tạo folder với AI (Gemini)
- **Tính năng**:
  - Text input cho prompt (tiếng Việt hoặc tiếng Anh)
  - AI tự động tạo folder structure và files
  - Preview generated structure
  - Example prompts
  - **Ví dụ prompt**: "tạo một folder chứa 10 file text từ 1-10"

#### 8.5. `Rename.cshtml`
- **Chức năng**: Đổi tên file/folder
- **Tính năng**:
  - Form nhập tên mới
  - Validation tên (không trùng, không ký tự đặc biệt)
  - Preview tên mới
  - Giữ nguyên file extension

#### 8.6. `Replace.cshtml`
- **Chức năng**: Thay thế file hiện tại bằng version mới
- **Tính năng**:
  - Upload file mới
  - Giữ metadata cũ (shares, comments, etc.)
  - Tạo version history
  - Size và format validation

#### 8.7. `Share.cshtml`
- **Chức năng**: Trang chia sẻ file/folder (full page)
- **Tính năng**:
  - Danh sách users để share
  - Permission selection
  - Expiration date picker
  - Generate public link
  - Email notifications

#### 8.8. `EditShare.cshtml`
- **Chức năng**: Chỉnh sửa share settings
- **Tính năng**:
  - Update permissions
  - Update expiration
  - Revoke share
  - View share history

#### 8.9. `SharedItems.cshtml`
- **Chức năng**: Danh sách các items được share với tôi
- **Tính năng**:
  - Files/folders shared by others
  - Filter by sharer
  - Permission indicator
  - Quick access actions
  - Group shared items

#### 8.10. `Favorites.cshtml`
- **Chức năng**: Danh sách các items được đánh dấu yêu thích
- **Tính năng**:
  - Star/unstar items
  - Quick access to favorite items
  - Sort và filter
  - Same actions as Index view

#### 8.11. `Trash.cshtml`
- **Chức năng**: Thùng rác (soft-deleted items)
- **Tính năng**:
  - Danh sách items đã xóa
  - Restore items
  - Permanently delete
  - Auto-delete after 30 days
  - Empty trash button
  - Filter by date deleted

#### 8.12. `Search.cshtml`
- **Chức năng**: Trang kết quả tìm kiếm
- **Tính năng**:
  - Semantic search results
  - Relevance score
  - Highlight matched terms
  - Filter results by type
  - Sort by relevance/date/name
  - Search trong tên file, content, tags

---

## 9. Thư Mục Version (File Versioning)

**Mục đích**: Quản lý lịch sử phiên bản của file.

### Các File View:

#### 9.1. `History.cshtml`
- **Chức năng**: Hiển thị lịch sử các phiên bản của file
- **Tính năng**:
  - Timeline các versions
  - File size cho mỗi version
  - Upload date và user
  - Download specific version
  - Restore previous version
  - Compare versions (nếu supported)
  - Delete old versions

---

## 10. Các File Đặc Biệt (Special Configuration Files)

### 10.1. `_ViewStart.cshtml`
- **Vị trí**: `Views/_ViewStart.cshtml`
- **Chức năng**: Thiết lập cấu hình mặc định cho tất cả views
- **Nội dung**:
  ```cshtml
  @{
      Layout = "_Layout";
  }
  ```
- **Giải thích**: 
  - File này chạy trước mỗi view
  - Set layout mặc định là `_Layout.cshtml`
  - Các view con có thể override bằng cách set `Layout = null` hoặc layout khác

### 10.2. `_ViewImports.cshtml`
- **Vị trí**: `Views/_ViewImports.cshtml`
- **Chức năng**: Import các namespace và Tag Helpers cho tất cả views
- **Nội dung**:
  ```cshtml
  @using CloudStorage
  @using CloudStorage.Models
  @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
  ```
- **Giải thích**:
  - `@using`: Import namespaces để không phải viết đầy đủ trong views
  - `@addTagHelper`: Enable Tag Helpers (asp-controller, asp-action, etc.)
  - File này được apply cho tất cả views trong thư mục và subfolders

---

## Kiến Trúc MVC và Vai Trò của Views

### MVC Pattern trong ASP.NET Core:

```
User Request
     ↓
Controller (Xử lý logic, chuẩn bị dữ liệu)
     ↓
Model (Dữ liệu được truyền vào View)
     ↓
View (Render HTML với dữ liệu từ Model)
     ↓
HTML Response → User
```

### Cách Views Hoạt Động:

1. **Controller Action** được gọi (ví dụ: `StorageController.Index()`)
2. Controller chuẩn bị **Model** (ví dụ: `StorageViewModel`)
3. Controller return view: `return View(model);`
4. **Razor Engine** tìm view tương ứng:
   - Controller là `StorageController`, Action là `Index`
   - Tìm file: `Views/Storage/Index.cshtml`
5. Razor xử lý:
   - Kết hợp layout từ `_Layout.cshtml`
   - Execute Razor syntax (`@`, `@{...}`, `@model`, etc.)
   - Bind dữ liệu từ Model vào HTML
6. Tạo ra **HTML final** và gửi về client

### View Discovery Rules:

ASP.NET Core tìm views theo thứ tự:
1. `Views/[ControllerName]/[ActionName].cshtml`
2. `Views/Shared/[ActionName].cshtml`

Ví dụ với `AccountController.Login()`:
- Tìm: `Views/Account/Login.cshtml` ✓
- Nếu không có, tìm: `Views/Shared/Login.cshtml`
- Nếu không có, throw exception

---

## Razor Syntax - Cú Pháp Quan Trọng

### 1. Model Declaration
```cshtml
@model CloudStorage.Models.ViewModels.StorageViewModel
```
Khai báo kiểu dữ liệu của Model được truyền vào view.

### 2. Accessing Model Properties
```cshtml
<h2>@Model.TotalFiles files</h2>
<p>@Model.TotalUsedStorage bytes used</p>
```

### 3. Code Blocks
```cshtml
@{
    var userName = User.Identity.Name;
    var isAdmin = User.IsInRole("Admin");
}
```

### 4. Control Structures

**If-Else**:
```cshtml
@if (Model.Items.Any())
{
    <table>...</table>
}
else
{
    <p>No items found</p>
}
```

**Foreach Loop**:
```cshtml
@foreach (var item in Model.Items)
{
    <tr>
        <td>@item.Name</td>
        <td>@item.Size</td>
    </tr>
}
```

### 5. Tag Helpers
```cshtml
<a asp-controller="Storage" asp-action="Index" asp-route-id="@item.Id">
    View
</a>
```
Generates: `<a href="/Storage/Index/123">View</a>`

### 6. Partial Views
```cshtml
@await Html.PartialAsync("_ShareModal")
```

### 7. Sections
```cshtml
@section Scripts {
    <script src="~/js/custom.js"></script>
}
```

### 8. ViewData & ViewBag
```cshtml
<title>@ViewData["Title"] - CloudStorage</title>
<h1>@ViewBag.Message</h1>
```

---

## Best Practices cho Views

### 1. **Separation of Concerns**
- Views chỉ nên chứa presentation logic
- Business logic phải ở Controllers hoặc Services
- Sử dụng ViewModels thay vì truyền Entity Models trực tiếp

### 2. **DRY (Don't Repeat Yourself)**
- Sử dụng Partial Views cho các component được reuse
- Tạo Layout chung thay vì duplicate HTML
- Use Tag Helpers thay vì viết HTML thủ công

### 3. **Security**
- Luôn encode output: `@Model.Name` (auto-encoded)
- Dùng `@Html.Raw()` cẩn thận, chỉ với trusted content
- CSRF protection với `asp-antiforgery="true"`

### 4. **Performance**
- Minimize logic trong views
- Sử dụng asynchronous rendering: `@await`
- Lazy load JavaScript khi có thể
- Optimize images và assets

### 5. **Accessibility**
- Semantic HTML tags
- ARIA labels
- Keyboard navigation
- Screen reader support

### 6. **Responsive Design**
- Mobile-first approach
- Bootstrap grid system
- Media queries trong CSS
- Test trên nhiều devices

---

## Cấu Trúc File View Mẫu

```cshtml
@* 1. Model Declaration *@
@model CloudStorage.Models.ViewModels.ExampleViewModel

@* 2. ViewData Configuration *@
@{
    ViewData["Title"] = "Example Page";
    var currentUser = User.Identity.Name;
}

@* 3. Main Content *@
<div class="container">
    <h1>@ViewData["Title"]</h1>
    
    @* 4. Conditional Rendering *@
    @if (Model.Items.Any())
    {
        @* 5. Loop Through Items *@
        <table class="table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model.Items)
                {
                    <tr>
                        <td>@item.Name</td>
                        <td>
                            @* 6. Tag Helpers *@
                            <a asp-action="Details" asp-route-id="@item.Id" 
                               class="btn btn-primary">
                                View
                            </a>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
    else
    {
        <p class="text-muted">No items found.</p>
    }
    
    @* 7. Partial View *@
    @await Html.PartialAsync("_SomePartial", Model.SomeData)
</div>

@* 8. Scripts Section *@
@section Scripts {
    <script>
        // Page-specific JavaScript
        console.log('Page loaded');
    </script>
}
```

---

## Common View Patterns trong Project

### 1. CRUD Views Pattern
Mỗi entity thường có 5 views:
- **Index**: List all items
- **Details**: View single item
- **Create**: Form tạo mới
- **Edit**: Form chỉnh sửa
- **Delete**: Confirmation page

### 2. Master-Detail Pattern
Ví dụ: Storage/Index.cshtml
- Master: List of folders
- Detail: Files trong folder được chọn

### 3. Modal Pattern
Ví dụ: _ShareModal.cshtml
- Popup overlay
- Form submission via AJAX
- Close và reload parent

### 4. Wizard Pattern
Ví dụ: Multi-step registration
- Step 1: Basic info
- Step 2: Preferences
- Step 3: Confirmation

---

## Integration với JavaScript

### Client-Side Interactions

Views thường include JavaScript để:
1. **Form Validation**
   - jQuery Validation
   - Custom validators
   - Real-time feedback

2. **AJAX Calls**
   ```javascript
   fetch('@Url.Action("GetData", "Storage")')
       .then(response => response.json())
       .then(data => {
           // Update UI
       });
   ```

3. **Dynamic Content**
   - Drag and drop
   - Real-time updates
   - Infinite scroll

4. **UI Enhancements**
   - Modals
   - Tooltips
   - Animations
   - Progress bars

---

## Debugging Views

### Common Issues:

1. **View Not Found**
   - Kiểm tra tên Controller và Action
   - Kiểm tra path: `Views/[Controller]/[Action].cshtml`
   - Case-sensitive trên Linux

2. **Model Binding Issues**
   - Kiểm tra `@model` declaration
   - Kiểm tra properties của Model
   - Null reference exceptions

3. **Layout Issues**
   - Kiểm tra `_ViewStart.cshtml`
   - Kiểm tra RenderBody() trong layout
   - Kiểm tra RenderSection()

4. **CSS/JS Not Loading**
   - Kiểm tra paths trong layout
   - Kiểm tra wwwroot folder
   - Cache browser

### Debugging Tips:

```cshtml
@* Output Debug Info *@
<pre>@System.Text.Json.JsonSerializer.Serialize(Model)</pre>

@* Check User Info *@
<p>User: @User.Identity.Name</p>
<p>Authenticated: @User.Identity.IsAuthenticated</p>

@* ViewData Inspection *@
@foreach (var key in ViewData.Keys)
{
    <p>@key: @ViewData[key]</p>
}
```

---

## Tổng Kết

Thư mục **Views** là trung tâm của presentation layer trong ứng dụng CloudStorage MVC. Hiểu rõ cấu trúc và vai trò của từng view giúp:

✅ **Dễ dàng maintain và extend** ứng dụng  
✅ **Tách biệt concerns** giữa presentation và business logic  
✅ **Reuse components** thông qua Partial Views và Layouts  
✅ **Tạo UI consistent** trên toàn bộ ứng dụng  
✅ **Optimize performance** với proper rendering strategies  

### Thống Kê Views:
- **Tổng số views**: 40+ files
- **Layouts**: 1 main layout
- **Partial views**: 3 (ShareModal, CommentSection, ValidationScripts)
- **View categories**: 9 functional areas
- **Configuration files**: 2 (_ViewStart, _ViewImports)

---

## Tài Liệu Tham Khảo

- [ASP.NET Core MVC Views](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview)
- [Razor Syntax Reference](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor)
- [Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro)
- [Layout trong ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/layout)

---

**Tài liệu này được tạo để giải thích chi tiết cấu trúc và chức năng của thư mục Views trong CloudStorage Application.**

**Phiên bản**: 1.0  
**Ngày tạo**: 2025-01-09  
**Tác giả**: CloudStorage Development Team
