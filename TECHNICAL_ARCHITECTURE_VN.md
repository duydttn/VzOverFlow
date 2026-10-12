# Kiến Trúc Kỹ Thuật VzOverFlow

> Tài liệu kỹ thuật chi tiết cho nền tảng Hỏi & Đáp VzOverFlow.

---

## 1. Tổng Quan Dự Án

**VzOverFlow** là một **bản clone của StackOverflow** được xây dựng bằng **ASP.NET Core 9.0 MVC**. Đây là ứng dụng web truyền thống (server-rendered) cung cấp nền tảng Hỏi & Đáp với các tính năng:

- **Câu hỏi & Câu trả lời** hỗ trợ Markdown
- **Bình chọn, Bình luận, và Gắn thẻ (Tag)**
- **Điểm uy tín & Gamification** (XP, Huy hiệu, Nhiệm vụ hàng ngày)
- **Xác thực hai yếu tố (2FA)** qua app Authenticator và OTP Email
- **Tích hợp AI** sử dụng Google Gemini API
- **Hệ thống báo cáo & kiểm duyệt nội dung**

---

## 2. Kiến Trúc & Luồng Xử Lý

### 2.1. Pattern: MVC với Service Layer

Dự án sử dụng **Service Layer Pattern** trên nền **ASP.NET Core MVC**. Dự án **không** dùng Repository Pattern chính thức. `AppDbContext` (EF Core) đóng vai trò là cả Unit of Work lẫn Repository.

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   Controller    │─────▶│     Service     │─────▶│  AppDbContext   │───▶ SQLite
│  (Nhận request) │      │  (Logic nghiệp  │      │     (ORM)       │
│                 │      │      vụ)        │      │                 │
└─────────────────┘      └─────────────────┘      └─────────────────┘
        │                                                  ▲
        │                                                  │
        └──▶ ViewModels ───────────────────────────────────┘
              (Định hình dữ liệu cho View)
```

**Vai trò của từng tầng:**
| Tầng | Trách nhiệm |
|:---|:---|
| **Controller** | Nhận HTTP request, validate input, gọi Service, trả về View hoặc JSON. |
| **Service** | Chứa logic nghiệp vụ, điều phối truy xuất dữ liệu, map Entity sang ViewModel. |
| **AppDbContext** | Entity Framework Core context. Truy cập database trực tiếp qua LINQ. |

### 2.2. Tại Sao Dùng ViewModel Tùy Chỉnh?

Dự án sử dụng rộng rãi các ViewModel trong `Models/ViewModels/`. Đây là **quyết định kiến trúc có chủ đích** vì:

| Lý Do | Giải Thích |
|:---|:---|
| **🔒 Bảo mật (Chống Over-Posting)** | Nếu bind trực tiếp vào Entity như `User`, client có thể gửi thêm các trường nhạy cảm (`Reputation`, `TwoFactorEnabled`) và thay đổi chúng. ViewModel chỉ expose **đúng** các field cần thiết. |
| **🔗 Tách biệt View khỏi Domain** | Entity `Question` có nhiều navigation properties (`Answers`, `Comments`, `Tags`). View danh sách câu hỏi không cần hết. `QuestionListViewModel` chỉ cung cấp dữ liệu cần thiết. |
| **✅ Validate Input** | ViewModel như `RegisterViewModel` chứa validation attributes (`[Required]`, `[MinLength]`) đặc thù cho input, không phải cho schema database. |
| **📦 Gom dữ liệu từ nhiều nguồn** | ViewModel như `UserProfileViewModel` tổng hợp dữ liệu từ nhiều entity (`User`, `Questions`, `Answers`, `Badges`) vào một object. |

### 2.3. Ví Dụ Luồng Request

**Kịch bản:** Người dùng xem chi tiết câu hỏi (`GET /questions/details/5`)

1. **HTTP Request** → `QuestionsController.Details(5)`
2. **Controller** gọi `IQuestionService.GetQuestionDetailsAsync(5, currentUserId)`
3. **Service**:
   - Query `AppDbContext.Questions` với `.Include()` để load các entity liên quan.
   - Tăng `ViewCount`.
   - Kiểm tra user hiện tại đã vote chưa.
   - Map `Question` entity → `QuestionDetailViewModel`.
4. **Controller** trả về `View(viewModel)`.
5. **Razor View** (`Details.cshtml`) render HTML dựa trên ViewModel.

---

## 3. Tầng Dữ Liệu (Data Layer)

### 3.1. Chiến Lược Database

| Khía Cạnh | Chi Tiết |
|:---|:---|
| **ORM** | Entity Framework Core 9.0 |
| **Database Provider** | **SQLite** (`VzOverFlow.db`) |
| **Connection String** | Cấu hình trong `appsettings.json` → `DefaultConnection` |
| **Chiến lược khởi tạo** | **Code-First Migrations** |
| **Thực thi khi Startup** | `await context.Database.MigrateAsync();` trong `Program.cs` |
| **Seed Data** | `SeedData.InitializeAsync(services)` tạo admin mặc định và các tag. |

### 3.2. Danh Sách Entity (Domain Models)

| Tên Entity | Primary Key | Quan Hệ Chính |
|:---|:---:|:---|
| `User` | `Id` (int) | `1:N` Questions, Answers, Comments, Votes, Activities, Badges. `N:M` Followers/Following. |
| `Question` | `Id` (int) | `N:1` User. `1:N` Answers, Comments, Votes. `N:M` Tags. `1:1` AcceptedAnswer (nullable). |
| `Answer` | `Id` (int) | `N:1` Question, User. `1:N` Comments, Votes. |
| `Comment` | `Id` (int) | `N:1` User, Question (hoặc Answer). |
| `Tag` | `Id` (int) | `N:M` Questions. |
| `Vote` | `Id` (int) | `N:1` User, Question (hoặc Answer). Giá trị: `+1` hoặc `-1`. |
| `OneTimeCode` | `Id` (int) | `N:1` User. Lưu OTP cho 2FA/Email verification. |
| `Report` | `Id` (int) | `N:1` Reporter, Question/Answer, ResolvedBy. Dùng cho kiểm duyệt. |
| `UserActivity` | `Id` (int) | `N:1` User. Log các hành động (hỏi, trả lời, vote) cho gamification. |
| `UserBadge` | `Id` (int) | `N:1` User. Lưu các huy hiệu đã đạt. |
| `DailyMission` | `Id` (int) | `N:1` User. Theo dõi nhiệm vụ hàng ngày. |
| `UserFollow` | `FollowerId, FollowedUserId` | Composite PK. Self-referencing `N:M` trên `User`. |
| `Notification` | `Id` (int) | `N:1` User. |

---

## 4. Dependency Injection & Packages

### 4.1. Các NuGet Package Chính (`.csproj`)

| Package | Mục Đích |
|:---|:---|
| `Microsoft.EntityFrameworkCore.Sqlite` | **ORM**. Provider cho SQLite. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | **Xác thực**. Quản lý user, hash password, roles. |
| `Markdig` | **Markdown**. Parse Markdown trong Q&A thành HTML. |
| `QRCoder` | **2FA**. Tạo mã QR cho việc setup Authenticator app. |
| `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` | **Dev Experience**. Hot-reload Razor Views khi đang dev. |
| `StackExchange.Redis` | **Caching** (tùy chọn). Redis client cho distributed caching. |
| `Elasticsearch.Net` | **Search** (tùy chọn). Full-text search. |
| `Microsoft.AspNetCore.SignalR.Common` | **Real-time** (tùy chọn). Cho tính năng thông báo live. |

### 4.2. Đăng Ký Service (`Program.cs`)

Tất cả service đều dùng **Scoped** lifetime (một instance cho mỗi HTTP request):

```csharp
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IAuthenticatorService, AuthenticatorService>();
builder.Services.AddScoped<IEmailSender, GmailEmailSender>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IVoteService, VoteService>();
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>(); // Dùng HttpClientFactory
```

**Tại sao dùng Scoped?** Các service phụ thuộc vào `AppDbContext`, mà DbContext cũng là scoped. Điều này đảm bảo cùng một DbContext instance được chia sẻ trong một request, giúp change tracking và transaction hoạt động đúng.

---

## 5. Giao Diện Người Dùng (UI/Frontend)

### 5.1. Công Nghệ Sử Dụng

| Khía Cạnh | Triển Khai |
|:---|:---|
| **Kiến trúc** | **MVC truyền thống với Razor Views** (server-rendered). |
| **CSS** | **Tailwind CSS** (cấu hình qua `tailwind.config.js`). |
| **Icons** | FontAwesome via `Blazorise.Icons.FontAwesome`. |
| **Tương tác** | Form HTML thông thường. Có dùng AJAX (`fetch()`) cho vote/comment. |

### 5.2. Tại Sao Không Dùng MVC Scaffolding Mặc Định?

| Hạn Chế Của Scaffolding | Lợi Ích Khi Custom |
|:---|:---|
| UI CRUD chung chung | **UI tùy chỉnh** với Tailwind CSS, modern và đẹp hơn. |
| Bind trực tiếp Entity | **ViewModel** cho tách biệt rõ ràng và bảo mật. |
| Full page reload | **AJAX endpoints** cho vote, comment không cần load lại trang. |
| Không có gamification | **Custom components** cho badge, XP, reputation. |

---

## 6. Cách Chạy Dự Án

```powershell
# 1. Clone repository
git clone <repo-url>
cd VzOverFlow

# 2. (Tùy chọn) Cấu hình email settings cho OTP
# Sửa appsettings.json hoặc dùng User Secrets

# 3. Chạy ứng dụng
dotnet run

# 4. Truy cập http://localhost:5142
```

**Khi khởi động, ứng dụng sẽ:**
1. Apply các database migration đang pending.
2. Seed data mặc định (admin user, tags ban đầu).
3. Tính lại điểm reputation cho tất cả user.
