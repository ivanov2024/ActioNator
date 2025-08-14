# ActioNator

ActioNator is a layered ASP.NET Core web application for personal improvement (fitness, wellness, self‑development). It provides goal tracking, journaling, workouts, a social feed, real‑time updates, and role‑based admin/moderation and coach verification.

## Features

- User, Coach, and Admin roles (ASP.NET Identity with roles)
- Community feed: create posts with images, like, comment, report
- Real‑time updates via SignalR (comments/refresh)
- Goal management with due dates and completion
- Journal entries with pagination
- Workouts with exercises and templates
- User profiles with profile/cover images
- Coach verification workflow (upload, review, approve/reject)
- Admin report review: posts, comments, users
- Secure file uploads (validators, content inspectors, allowlists)
- Responsive UI using Bootstrap, Tailwind, and Alpine.js

## Tech Stack

- ASP.NET Core MVC (.NET 8)
- Entity Framework Core + SQL Server
- ASP.NET Identity (ApplicationUser + IdentityRole<Guid>)
- SignalR (hub mapped at `/communityHub`)
- Cloudinary (image hosting) and Dropbox (OAuth/token storage)
- NUnit + Moq test suite (controllers and services)

## Solution Structure

- `ASP.NET Final exam.sln` (solution)
- `ActioNator/` (web app)
  - `Program.cs` – DI, middleware, antiforgery, session, routing, SignalR hub
  - `Areas/Admin|User|Coach|Identity` – areas, controllers, views/pages
  - `Views/Shared/` – `_Layout.cshtml`, `_LayoutStandards.cshtml`, shared partials
  - `wwwroot/` – static assets (Bootstrap, Tailwind, Alpine, JS)
- `ActioNator.Data/` – EF Core DbContext, migrations, entity configurations
- `ActioNator.Data.Models/` – domain entities (20+)
- `ActioNator.Services/` – interfaces, implementations, validators, seeding
- `ActioNator.ViewModels/` – view models by feature
- `WebTests/` – NUnit tests for controllers/services with in‑memory contexts

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 / Rider

### Configuration

Copy `ActioNator/appsettings.json` and set the following sections:

```json
{
  "ConnectionStrings": {
    "DefaultActioNatorConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ActioNator;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "FileUpload": {
    "AllowedImageExtensions": [ ".jpg", ".jpeg", ".png", ".gif", ".webp" ],
    "AllowedPdfExtensions": [ ".pdf" ],
    "DangerousExtensions": [ ".exe", ".bat", ".cmd", ".js", ".msi" ]
  },
  "CloudinarySettings": {
    "CloudName": "<your-cloud>",
    "ApiKey": "<your-key>",
    "ApiSecret": "<your-secret>"
  },
  "Dropbox": {
    "ClientId": "<optional>",
    "ClientSecret": "<optional>"
  }
}
```

### Database

- Apply EF Core migrations (migrations live in `ActioNator.Data/Migrations/`):

```bash
# from repo root
 dotnet ef database update --project "ASP.NET Final exam/ActioNator.Data" --startup-project "ASP.NET Final exam/ActioNator"
```

- On first run, roles are seeded by `RoleSeeder`.

### Run the App

```bash
# from repo root
 dotnet build "ASP.NET Final exam/ActioNator/ActioNator.csproj"
 dotnet run --project "ASP.NET Final exam/ActioNator"
```

- The app redirects `/` to `Identity/Account/Access`.
- Register a user; assign roles manually as needed (e.g., Admin/Coach).

### Tests

```bash
# full test suite
 dotnet test "ASP.NET Final exam/WebTests/WebTests.csproj"
```

The suite includes comprehensive NUnit tests for controllers and services using in‑memory DbContexts. 

## Architecture & Security

- **Areas & Layouts**: `Areas/User/Views/_ViewStart.cshtml` uses `Views/Shared/_LayoutStandards.cshtml`; Identity uses `Views/Shared/_Layout.cshtml`.
- **DI & Services**: Interfaces under `ActioNator.Services/Interfaces/*` with implementations under `.../Implementations/*` registered in `Program.cs`.
- **EF Core**: `ActioNator.Data/ActioNatorDbContext.cs`, entity configurations in `ActioNator.Data/EntityConfigurations/*`.
- **Auth**: Identity configured with roles; cookie/paths set; session (2h) enabled.
- **CSRF**: Antiforgery configured (custom header/cookie); controllers use `[ValidateAntiForgeryToken]` on POST.
- **Validation**: Data annotations in entities; file validation orchestrator + image/pdf validators; content inspection.
- **SignalR**: `Program.cs` maps `/communityHub`; hub in `ActioNator.Services/Implementations/Hubs/CommunityHub.cs`.

## Notable Features

- Community moderation (admin): review and dismiss/delete reports for posts/comments/users.
- Coach verification pipeline: upload docs, admin review, badge on approval.
- Alias routing: Admin/Coach prefixes mapping into User area with policies.

## Bonus / Optional

- **Cloud**: Cloudinary integration for images; Dropbox OAuth/token handling.
- **Realtime**: SignalR live updates for community interactions.
- **Front‑end**: Tailwind + Alpine used alongside Bootstrap.