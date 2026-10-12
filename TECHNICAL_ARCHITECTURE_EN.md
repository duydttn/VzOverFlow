# VzOverFlow Technical Architecture

> A comprehensive technical documentation for the VzOverFlow Q&A platform.

---

## 1. Project Overview

**VzOverFlow** is a **StackOverflow clone** built with **ASP.NET Core 9.0 MVC**. It is a traditional server-rendered web application providing a Q&A platform featuring:

- **Questions & Answers** with Markdown support
- **Voting, Comments, and Tagging** systems
- **User Reputation & Gamification** (XP, Badges, Daily Missions)
- **Two-Factor Authentication (2FA)** via Authenticator Apps and Email OTP
- **AI-Powered Features** using Google Gemini API
- **Content Moderation & Reporting** system

---

## 2. Architectural Pattern & Logic Flow

### 2.1. Pattern: Service-Based MVC

This project uses a **Service Layer Pattern** on top of **ASP.NET Core MVC**. It does **not** implement a formal Repository pattern. The `AppDbContext` (EF Core) acts as both Unit of Work and Repository.

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   Controller    │─────▶│     Service     │─────▶│  AppDbContext   │───▶ SQLite
│   (HTTP I/O)    │      │ (Business Logic)│      │     (ORM)       │
└─────────────────┘      └─────────────────┘      └─────────────────┘
        │                                                  ▲
        │                                                  │
        └──▶ ViewModels ───────────────────────────────────┘
                  (Data Shaping for Views)
```

**Key Responsibilities:**
| Layer | Responsibility |
|:---|:---|
| **Controller** | Handles HTTP requests, validates input, calls services, returns Views or JSON. |
| **Service** | Contains business logic, orchestrates data access, maps entities to ViewModels. |
| **AppDbContext** | Entity Framework Core context. Direct database access via LINQ. |

### 2.2. Why Custom ViewModels/DTOs?

The project extensively uses custom ViewModels in `Models/ViewModels/`. This is a **deliberate architectural decision** for:

| Reason | Explanation |
|:---|:---|
| **🔒 Security (Over-Posting Prevention)** | Binding directly to entities like `User` allows malicious clients to modify sensitive fields (`Reputation`, `TwoFactorEnabled`). ViewModels expose **only** the fields a form should accept. |
| **🔗 Decoupling View from Domain** | A `Question` entity has many navigation properties (`Answers`, `Comments`, `Tags`, `Votes`). A question list view doesn't need all that data. `QuestionListViewModel` provides only what the view needs. |
| **✅ Input Validation** | ViewModels like `RegisterViewModel` contain validation attributes (`[Required]`, `[MinLength]`, `[Compare]`) that are specific to user input, not database constraints. |
| **📦 Data Aggregation** | ViewModels like `UserProfileViewModel` aggregate data from multiple entities (`User`, `Questions`, `Answers`, `Badges`) into a single object. |

### 2.3. Request Workflow Example

**Scenario:** User views a question detail page (`GET /questions/details/5`)

1. **HTTP Request** → `QuestionsController.Details(5)`
2. **Controller** calls `IQuestionService.GetQuestionDetailsAsync(5, currentUserId)`
3. **Service**:
   - Queries `AppDbContext.Questions` with `.Include()` for related entities.
   - Increments `ViewCount`.
   - Checks if the current user has voted.
   - Maps `Question` entity → `QuestionDetailViewModel`.
4. **Controller** returns `View(viewModel)`.
5. **Razor View** (`Details.cshtml`) renders the HTML using the strongly-typed ViewModel.

---

## 3. Data Layer & Persistence

### 3.1. Database Strategy

| Aspect | Detail |
|:---|:---|
| **ORM** | Entity Framework Core 9.0 |
| **Database Provider** | **SQLite** (`VzOverFlow.db`) |
| **Connection String** | Configured in `appsettings.json` → `DefaultConnection` |
| **Initialization Strategy** | **Code-First Migrations** |
| **Startup Execution** | `await context.Database.MigrateAsync();` in `Program.cs` |
| **Seed Data** | `SeedData.InitializeAsync(services)` creates default admin and tags. |

### 3.2. Entity Catalog

| Entity Name | Primary Key | Key Relationships |
|:---|:---:|:---|
| `User` | `Id` (int) | `1:N` Questions, Answers, Comments, Votes, Activities, Badges. `N:M` Followers/Following. |
| `Question` | `Id` (int) | `N:1` User. `1:N` Answers, Comments, Votes. `N:M` Tags. `1:1` AcceptedAnswer (nullable). |
| `Answer` | `Id` (int) | `N:1` Question, User. `1:N` Comments, Votes. |
| `Comment` | `Id` (int) | `N:1` User, Question (or Answer). |
| `Tag` | `Id` (int) | `N:M` Questions. |
| `Vote` | `Id` (int) | `N:1` User, Question (or Answer). Value: `+1` or `-1`. |
| `OneTimeCode` | `Id` (int) | `N:1` User. Stores OTP for 2FA/Email verification. |
| `Report` | `Id` (int) | `N:1` Reporter, Question/Answer, ResolvedBy. Content moderation. |
| `UserActivity` | `Id` (int) | `N:1` User. Logs actions (ask, answer, vote) for gamification. |
| `UserBadge` | `Id` (int) | `N:1` User. Stores earned badges. |
| `DailyMission` | `Id` (int) | `N:1` User. Tracks daily gamification goals. |
| `UserFollow` | `FollowerId, FollowedUserId` | Composite PK. Self-referencing `N:M` on `User`. |
| `Notification` | `Id` (int) | `N:1` User. |

---

## 4. Dependency Injection & Packages

### 4.1. Key NuGet Packages (`.csproj`)

| Package | Purpose |
|:---|:---|
| `Microsoft.EntityFrameworkCore.Sqlite` | **ORM**. SQLite database provider. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | **Authentication**. User management, password hashing, roles. |
| `Markdig` | **Markdown**. Parses Markdown in Q&A bodies to HTML. |
| `QRCoder` | **2FA**. Generates QR codes for authenticator app setup. |
| `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` | **Dev Experience**. Hot-reload Razor views during development. |
| `StackExchange.Redis` | **Caching** (optional). Redis client for future distributed caching. |
| `Elasticsearch.Net` | **Search** (optional). Full-text search capability. |
| `Microsoft.AspNetCore.SignalR.Common` | **Real-time** (optional). For future live notifications. |

### 4.2. Service Registration (`Program.cs`)

All application services use **Scoped** lifetime (one instance per HTTP request):

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
builder.Services.AddHttpClient<IGeminiService, GeminiService>(); // Uses HttpClientFactory
```

**Why Scoped?** Services depend on `AppDbContext`, which is also scoped. This ensures the same DbContext instance is shared across a single request, enabling proper change tracking and transactions.

---

## 5. UI / Frontend Integration

### 5.1. Technology Choices

| Aspect | Implementation |
|:---|:---|
| **Architecture** | Traditional **server-rendered MVC with Razor Views** (`.cshtml`). |
| **Styling** | **Tailwind CSS** (configured via `tailwind.config.js`, `postcss.config.js`). |
| **Icons** | FontAwesome via `Blazorise.Icons.FontAwesome`. |
| **Interactivity** | Standard HTML forms. Some AJAX via `fetch()` for voting/comments. |

### 5.2. Why Not Standard MVC Scaffolding?

| Scaffolding Limitation | Custom Implementation Benefit |
|:---|:---|
| Generic CRUD UI | **Custom-designed UI** with Tailwind CSS for a modern, branded look. |
| Tightly coupled to Entity | **ViewModels** for clean separation and security. |
| No partial updates | **AJAX endpoints** for voting, comments without full page reload. |
| No gamification | **Custom components** for badges, XP, reputation display. |

---

## 6. How to Run

```powershell
# 1. Clone the repository
git clone <repo-url>
cd VzOverFlow

# 2. Configure email settings (optional, for OTP)
# Edit appsettings.json or use User Secrets

# 3. Run the application
dotnet run

# 4. Access at http://localhost:5142
```

**On Startup, the application will:**
1. Apply pending database migrations.
2. Seed default data (admin user, initial tags).
3. Recalculate all user reputations.
