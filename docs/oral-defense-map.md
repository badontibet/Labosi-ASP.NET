# NAS Indexer Oral Defense Map

Use this as a quick navigation guide during the defense. Open the listed files, explain the purpose in plain language, then show the manual demo step.

## 1. ASP.NET Core MVC Structure

- What it does: Separates request handling, domain data, and Razor UI into controllers, models, views, repositories, and view models.
- Why it exists: Keeps CRUD screens and dashboard behavior easy to explain and maintain.
- Open: `Controllers/`, `Views/`, `Models/`, `ViewModels/`, `Repositories/`, `Program.cs`.
- Say: "The MVC controllers receive browser requests and choose views. Models represent NAS domain data, view models shape form/dashboard data, and repositories centralize database access for MVC screens."
- Likely question: "Where does a browser request go first?"
- Short answer: "Routing in `Program.cs` maps it to a controller action, then the action returns a Razor view with a model."
- Demonstrate: Open `/FileItems`, then show `Controllers/FileItemsController.cs` and `Views/FileItems/Index.cshtml`.

## 2. API Controllers

- What it does: Exposes JSON endpoints for NAS domain entities.
- Why it exists: Provides testable programmatic access separate from MVC pages.
- Open: `Controllers/Api/`, especially `FileItemsApiController.cs`, `FileChangeLogsApiController.cs`, `FileAttachmentsApiController.cs`.
- Say: "API controllers use `[ApiController]`, DTOs, HTTP status codes, and role attributes. They do not return EF entities directly."
- Likely question: "How do you prove the APIs work?"
- Short answer: "The integration tests call real HTTP endpoints through `WebApplicationFactory`."
- Demonstrate: Run or show `Labosi-ASP.NET.Tests/Api/FileItemsApiTests.cs`.

## 3. DTOs And Password Safety

- What it does: DTOs define safe API request and response shapes.
- Why it exists: Prevents exposing internal EF navigation data and sensitive fields.
- Open: `Dtos/`, `Controllers/Api/NasServersApiController.cs`, `Controllers/Api/SystemAdminsApiController.cs`.
- Say: "DTOs deliberately omit `NasServer.Password` and `SystemAdmin.Password`. API mapping is manual so sensitive fields never accidentally leave the server."
- Likely question: "Could a submitted password JSON field be accepted by the API?"
- Short answer: "No, the create/update DTOs do not define password fields, so extra JSON is ignored by model binding."
- Demonstrate: Show `SystemAdminDto.cs`, `CreateSystemAdminDto.cs`, and the password-safety tests.

## 4. Entity Framework Core And SQLite

- What it does: Persists NAS servers, scan jobs, directories, files, tags, change logs, attachments, admins, and Identity tables.
- Why it exists: Gives the project relational persistence with migrations and testable queries.
- Open: `Data/NasIndexerDbContext.cs`, `Migrations/`, `Repositories/EfNasRepository.cs`.
- Say: "`NasIndexerDbContext` defines DbSets and relationships. SQLite is used locally, and tests use isolated SQLite databases or in-memory connections."
- Likely question: "Where are relationships configured?"
- Short answer: "In `OnModelCreating`, including FileItem to ChangeLogs and FileItem to Attachments."
- Demonstrate: Open `NasIndexerDbContext.cs` and point to the FileItem relationships.

## 5. ASP.NET Core Identity

- What it does: Provides local accounts, login/logout, password hashing, and user tables.
- Why it exists: Keeps authentication separate from NAS domain entities such as `SystemAdmin`.
- Open: `Program.cs`, `Models/AppUser.cs`, `Areas/Identity/Pages/Account/`.
- Say: "Identity handles application users. `SystemAdmin` remains NAS domain data and is not used for login."
- Likely question: "Where is Identity registered?"
- Short answer: "In `Program.cs` with `AddDefaultIdentity<AppUser>()`, roles, and EF stores."
- Demonstrate: Open the login/register pages and `Program.cs`.

## 6. AppUser With OIB/JMBG

- What it does: Extends Identity users with required Croatian personal identifiers.
- Why it exists: Satisfies the account-data requirement without changing domain entities.
- Open: `Models/AppUser.cs`, `Areas/Identity/Pages/Account/Register.cshtml`, `Register.cshtml.cs`.
- Say: "`AppUser` extends `IdentityUser` and adds validation for OIB and JMBG. Registration captures those fields."
- Likely question: "Are OIB and JMBG stored on SystemAdmin?"
- Short answer: "No, they belong to `AppUser`, the authentication entity."
- Demonstrate: Show Register page fields and `AppUser` validation attributes.

## 7. Admin And Manager Roles

- What it does: Restricts write/delete operations by role.
- Why it exists: Separates read access, management actions, and destructive actions.
- Open: `Data/IdentitySeedData.cs`, controllers under `Controllers/` and `Controllers/Api/`, `Labosi-ASP.NET.Tests/Api/AuthorizationTests.cs`.
- Say: "Managers can create/edit, Admins can delete, and anonymous users can access public lists/search where allowed."
- Likely question: "How are roles created?"
- Short answer: "`IdentitySeedData` seeds the Admin and Manager roles without hardcoding passwords."
- Demonstrate: Show `[Authorize(Roles = "Admin,Manager")]` and `[Authorize(Roles = "Admin")]`.

## 8. Authentication Vs Authorization

- What it does: Authentication identifies the user; authorization decides what the user can do.
- Why it exists: Prevents unauthenticated or insufficient-role users from modifying data.
- Open: `Program.cs`, `Controllers/FileItemsController.cs`, `Controllers/Api/FileItemsApiController.cs`, `Views/Shared/_LoginPartial.cshtml`.
- Say: "`UseAuthentication` runs before `UseAuthorization`. Controller attributes then enforce whether a user must be logged in or in a role."
- Likely question: "What happens when an anonymous user opens an edit page?"
- Short answer: "They are challenged or redirected because edit actions require Admin or Manager."
- Demonstrate: Open a protected details/edit URL while logged out.

## 9. Google External Login And User-Secrets

- What it does: Enables Google login only when client id and secret are configured.
- Why it exists: Supports external login without committing secrets.
- Open: `Program.cs`, `Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs`, `Labosi-ASP.NET.csproj`.
- Say: "Google auth is conditional. If the config values are missing, local login still works and no fake secrets are tracked."
- Likely question: "Where are Google secrets stored?"
- Short answer: "In user-secrets or environment configuration, not in the repository."
- Demonstrate: Show the `Authentication:Google:ClientId` and `ClientSecret` configuration reads in `Program.cs`.

## 10. File Upload And FileAttachment Storage

- What it does: Lets authenticated users list attachments, Managers/Admins upload, and Admins delete.
- Why it exists: Adds real file-management behavior to FileItem records.
- Open: `Models/FileAttachment.cs`, `Services/FileAttachmentStorageService.cs`, `Controllers/Api/FileAttachmentsApiController.cs`, `Views/FileItems/_Attachments.cshtml`, `wwwroot/js/file-attachments.js`.
- Say: "Uploads are stored with generated names and metadata. The API does not expose absolute server paths or uploaded file contents."
- Likely question: "How do you prevent unsafe uploads?"
- Short answer: "The storage service validates size, extension, generated filename, and safe root paths."
- Demonstrate: Open a File details page as Admin/Manager and upload a small allowed file.

## 11. FileChangeLog And Real Dashboard Activity

- What it does: Records file activity for create/edit and attachment upload/delete.
- Why it exists: Makes the dashboard Recent File Changes reflect real user actions.
- Open: `Models/FileChangeLog.cs`, `Services/FileChangeLogService.cs`, `Controllers/FileItemsController.cs`, `Controllers/Api/FileItemsApiController.cs`, `Views/Home/Index.cshtml`.
- Say: "Activity logging is centralized in `FileChangeLogService`, so MVC, API, and attachment flows write consistent safe audit records."
- Likely question: "Why is FileChangeLog API read-only?"
- Short answer: "It is audit/history data. Public clients can read it, but cannot create or rewrite audit records directly."
- Demonstrate: Edit a FileItem, return to Dashboard, and show the new Recent File Changes row.

## 12. Dashboard Metrics And Operational Status

- What it does: Shows data-backed metrics, scan warnings, health status, progress bars, and recent activity.
- Why it exists: Gives the application a real operations-center landing page.
- Open: `Controllers/HomeController.cs`, `ViewModels/DashboardViewModel.cs`, `Views/Home/Index.cshtml`, `wwwroot/css/site.css`.
- Say: "The dashboard uses database queries with `AsNoTracking`, computes health deterministically from scan/server state, and avoids fake activity."
- Likely question: "How is Critical status computed?"
- Short answer: "If failed scan jobs exist, status is Critical; otherwise running scans or inactive servers produce Warning."
- Demonstrate: Open `/` and point to Operational Status, Scan Watchlist, and Recent File Changes.

## 13. Global Search

- What it does: Searches menu/page entries and NAS data from one shared layout box.
- Why it exists: Makes the app easier to navigate during real use.
- Open: `Controllers/SearchController.cs`, `ViewModels/GlobalSearchViewModel.cs`, `Views/Search/Index.cshtml`, `Views/Shared/_Layout.cshtml`.
- Say: "Global search is server-side, grouped by type, capped per group, and avoids sensitive fields."
- Likely question: "Can anonymous users use search?"
- Short answer: "Yes, but links are chosen to respect existing authorization boundaries."
- Demonstrate: Search for a file, server, tag, and a nonsense term.

## 14. File/Request Logging

- What it does: Writes one safe line per completed HTTP request and logs unhandled exceptions.
- Why it exists: Provides operational evidence without logging bodies, cookies, auth headers, or secrets.
- Open: `Services/AppFileLogger.cs`, `Middleware/RequestFileLoggingMiddleware.cs`, `.gitignore`, `logs/` when running locally.
- Say: "The request logging middleware runs after auth, writes to `logs/app-yyyyMMdd.log`, and redacts unsafe query keys."
- Likely question: "Are uploads or passwords logged?"
- Short answer: "No. The middleware never reads bodies, forms, multipart content, cookies, or authorization headers."
- Demonstrate: Open a page, then inspect `logs/app-yyyyMMdd.log`.

## 15. Integration Tests With WebApplicationFactory

- What it does: Runs HTTP-level tests against the app with isolated database/configuration.
- Why it exists: Proves controllers, auth rules, validation, and status codes work together.
- Open: `Labosi-ASP.NET.Tests/CustomWebApplicationFactory.cs`, `TestAuthHandler.cs`, `Labosi-ASP.NET.Tests/Api/`.
- Say: "`WebApplicationFactory` boots the real app pipeline. Test auth headers simulate authenticated users and roles."
- Likely question: "Do tests touch production uploads/logs?"
- Short answer: "No, the factory uses temp upload and log roots."
- Demonstrate: Run `dotnet test --logger "console;verbosity=minimal"`.

## 16. Playwright End-To-End Test

- What it does: Runs a browser scenario through login, FileItem edit, dashboard activity proof, global search, and tag workflow.
- Why it exists: Proves the UI works in a real browser, not just via HTTP tests.
- Open: `Labosi-ASP.NET.E2ETests/AdminTagWorkflowE2ETests.cs`, `E2EWebAppFixture.cs`.
- Say: "The E2E fixture uses a temp SQLite database, seeds an Admin user, starts the app locally, and launches Chromium headlessly."
- Likely question: "Does it require Google login or real secrets?"
- Short answer: "No, it uses a seeded local Admin account in isolated temp storage."
- Demonstrate: Run `dotnet test Labosi-ASP.NET.E2ETests/Labosi-ASP.NET.E2ETests.csproj --logger "console;verbosity=minimal"`.

## 17. Responsive UI

- What it does: Keeps navigation, tables, forms, dashboard cards, and attachment controls usable on mobile/tablet/desktop.
- Why it exists: Improves stability and usability during live demonstration.
- Open: `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css`, `Views/FileItems/_Attachments.cshtml`, list views under `Views/*/Index.cshtml`.
- Say: "The app uses a native mobile menu, responsive grids, table wrappers, stacked actions, safe text wrapping, and reduced-motion support."
- Likely question: "How do long paths behave on small screens?"
- Short answer: "Tables stay inside responsive wrappers and path text wraps safely."
- Demonstrate: Inspect Dashboard, Files, and Change Logs at 375px, 768px, and desktop width.

## 18. Git/Branch Workflow And Final Verification

- What it does: Keeps upgrade work isolated on `project-upgrade` and verifies before defense.
- Why it exists: Prevents accidental generated files or unrelated behavior changes from entering the final state.
- Open: `git status`, `.gitignore`, `docs/project-upgrade-checklist.md`, `lab-1/agent_log.txt`.
- Say: "Final verification is build, full tests, dedicated E2E, diff check, manual browser smoke, and generated-file review."
- Likely question: "What should never be committed?"
- Short answer: "Generated logs, uploads, Playwright reports, traces, screenshots, browser binaries, `bin/`, `obj/`, and local database files."
- Demonstrate: Run `git status --short --ignored` and `git diff --check`.
