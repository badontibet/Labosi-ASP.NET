# Project Upgrade Checklist - NAS Indexer

Audit date: 2026-07-08  
Branch: `project-upgrade`  
Scope: read-only project audit and implementation plan. No application behavior was changed.

## Verification Snapshot

- `dotnet build`: passed after logging implementation, 0 warnings, 0 errors.
- `dotnet test --logger "console;verbosity=minimal"`: passed after Playwright implementation. Integration tests: 142 passed, 0 failed, 0 skipped. E2E tests: 1 passed, 0 failed, 0 skipped.
- Static test count before logging implementation: 136 xUnit facts/theories under `Labosi-ASP.NET.Tests/Api`; logging implementation added 2 focused request-log tests; global search implementation added 4 focused MVC search tests; Playwright implementation added 1 browser E2E test.
- Responsive implementation changed only UI/static/documentation/log files; backend behavior was not changed.
- Pre-existing untracked files were present before this audit under `wwwroot/uploads/file-attachments/1/` and `wwwroot/uploads/file-attachments/3/`.

## Current Repository Evidence

- ASP.NET Core MVC NAS Indexer app: `Program.cs`, `Controllers/`, `Views/`, `Models/`, `Data/NasIndexerDbContext.cs`, `Repositories/EfNasRepository.cs`.
- API controllers and DTOs: `Controllers/Api/*ApiController.cs`, `Dtos/*Dto.cs`.
- Integration tests: `Labosi-ASP.NET.Tests/Api/*.cs`, `CustomWebApplicationFactory.cs`, `TestAuthHandler.cs`, `TestDataFactory.cs`.
- Identity and roles: `Models/AppUser.cs`, `Areas/Identity/Pages/Account/*`, `Data/IdentitySeedData.cs`, `Program.cs`.
- File attachment upload backend and AJAX UI: `Models/FileAttachment.cs`, `Services/FileAttachmentStorageService.cs`, `Controllers/Api/FileAttachmentsApiController.cs`, `Views/FileItems/_Attachments.cshtml`, `wwwroot/js/file-attachments.js`.
- Google login configuration: `Program.cs`, `Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs`; secrets are read from configuration keys, not tracked values.
- File-based application request logging: `Services/AppFileLogger.cs`, `Middleware/RequestFileLoggingMiddleware.cs`, `Program.cs`, `.gitignore`, and focused tests in `Labosi-ASP.NET.Tests/Api/RequestFileLoggingTests.cs`.
- Responsive CSS/UI patterns: viewport meta in `Views/Shared/_Layout.cshtml`, native mobile menu, mobile/tablet/desktop breakpoints, responsive grids, `table-responsive` wrappers, wrapped/stacked actions, and mobile-safe attachment upload controls in `wwwroot/css/site.css`, `wwwroot/css/lab4.css`, and `wwwroot/js/file-attachments.js`.
- Global search: `Controllers/SearchController.cs`, `ViewModels/GlobalSearchViewModel.cs`, `Views/Search/Index.cshtml`, shared layout search form, and focused tests in `Labosi-ASP.NET.Tests/Api/GlobalSearchTests.cs`.
- Playwright E2E scenario: `Labosi-ASP.NET.E2ETests/` with a temp SQLite database, temp upload/log roots, seeded Admin user, local app process, headless Chromium browser flow, and one 12-step Admin tag workflow.
- Page-local search remains available: `Search` MVC actions in list controllers, repository search methods, `data-lab4-search` inputs, and `wwwroot/js/lab4.js`.

## Scored Audit Matrix

| Criterion | Points | Status | Evidence | Manual demonstration | Automated verification | Remaining work | Risk |
|---|---:|---|---|---|---|---|---|
| Deploy to a cloud provider or virtual machine | 3 | Missing | No Dockerfile, publish profile, cloud manifest, workflow, or deployment docs found. `.github` contains hooks/skills only. | None currently. | `rg -i "docker|azure|render|railway|fly|publish"` found no concrete deployment config. | Add a low-risk deployment path, environment config notes, database/file storage plan, and demo URL. | Medium: SQLite and uploads need a persistent production storage decision. |
| Tests for all API endpoints | 2 | Complete | 8 API controller test files plus authorization tests cover list/search/details/create/update/delete where supported; FileChangeLog write endpoints are tested as unavailable by rule. | Explain API routes and show test project structure. | `dotnet test --logger "console;verbosity=minimal"` reports 136/136 passed. | Preserve coverage when adding new endpoints such as global search, logging, AI, or MCP. | Low if future endpoints get tests immediately. |
| Optional Playwright scenario with 10 steps | Up to 3 extra | Complete | Added `Labosi-ASP.NET.E2ETests` with `Microsoft.Playwright.Xunit`. The E2E fixture seeds a temp SQLite DB and Admin user, starts the app on a random localhost port, and runs one headless Chromium browser scenario with 12 visible steps. | Run the E2E command and show the browser workflow steps in `AdminTagWorkflowE2ETests.cs`; optionally run headed by changing Playwright launch options locally for demo only. | `dotnet test Labosi-ASP.NET.E2ETests/Labosi-ASP.NET.E2ETests.csproj --logger "console;verbosity=minimal"` passed; full `dotnet test` also passed. | Browser binaries must be installed on the machine before first run. | Medium: browser install is machine-local and not committed; test uses temp DB/storage and avoids Google/user-secrets. |
| AI integration for data entry or similar use | 3 | Missing | No OpenAI/Azure AI package, service, controller, view integration, or config keys found. | None currently. | `rg -i "OpenAI|Azure.AI|AI"` found no concrete feature. | Add a small NAS-domain data-entry helper, for example metadata/tag suggestions for FileItem creation. | Medium/high: secrets, prompt safety, validation, and graceful offline fallback are required. |
| Global search across menus, pages, and data | 2 | Complete | Shared layout includes a global search form that submits to `GET /Search?q=...`; `SearchController` performs server-side grouped search across pages, NAS servers, directories, files, file tags, scan jobs, and attachments by original filename. Results are capped per group and use safe list links for anonymous users. | Search for a known server, file, tag, scan path, menu term such as `files`, and a nonsense term. Show grouped results and friendly empty state. | `GlobalSearchTests` cover empty query, known grouped data results, page/menu results, anonymous-safe links, and password exclusion; `dotnet test` reports 142/142 passed. | Manual browser proof before grading is recommended. | Low/medium: search intentionally avoids passwords/secrets/raw hidden data and keeps detail links auth-aware. |
| Logging mechanism using a file or API | 2 | Complete | Custom `AppFileLogger` writes to `logs/app-yyyyMMdd.log`; `RequestFileLoggingMiddleware` logs completed MVC/API requests and unhandled exceptions without request bodies, cookies, auth headers, uploaded contents, or unsafe query values. `.gitignore` excludes `logs/`. | Run the app, open `/`, `/api/tags?query=demo`, and an authenticated page if available; inspect `logs/app-yyyyMMdd.log` for method, path, status, elapsed time, and user/anonymous. | `RequestFileLoggingTests` verify request logging and sensitive query redaction; `dotnet test` reports 138/138 passed. | Preserve logging when future endpoints are added; add exception demo only in a controlled development scenario if needed. | Low/medium: avoid future changes that log bodies, cookies, secrets, OIB/JMBG, or multipart form data. |
| Responsive mobile/web UI | 2 | Complete | UX/UI sub-agent audit completed. Shared layout now has a native mobile menu; entity tables and attachment tables use `table-responsive`; action/form/upload controls wrap or stack on narrow screens; dashboard/cards/details/forms use responsive grid behavior; long table/detail text wraps safely; Identity `page-header` styling is aligned with app panels. | Use DevTools at 375px, 768px, and desktop width; open the menu, dashboard, entity lists, create/edit form, details page, FileItem attachment section, and register/external-login pages. | `dotnet build`, `dotnet test`, and `git diff --check`; static inspection of `Views/**/*.cshtml`, Identity pages, `site.css`, `lab4.css`, and attachment JS. | Manual browser proof remains recommended before grading; no Playwright screenshot test was added in this stage. | Low: changes are CSS/Razor/attachment-list markup only and preserve backend behavior. |
| CRUD must work without errors | 2 | Complete for existing domain surfaces | MVC controllers and API tests cover CRUD/business rules; FileChangeLog is intentionally read-only; FileAttachment API covers upload/list/delete. | Manually create/edit/delete allowed NAS servers, scan jobs, directories, files, tags, admins; verify blocked deletes show errors. | 136 integration tests passed, including CRUD success, invalid input, not found, conflict, and authorization cases. | Re-test after each upgrade stage. | Low if changes stay scoped. |
| Expose MCP and access through an agentic IDE | 2 | Missing | `.agents/skills/ux-ui-subagent` and `.github/skills/*` exist, but no MCP server, manifest, endpoint, or IDE connection instructions were found. | None currently. | `rg -i "MCP|ModelContextProtocol"` found no concrete support. | Add a minimal NAS Indexer MCP surface or documented MCP server exposing selected data/actions. | Medium/high: must define safe tools and auth/read-only boundaries. |
| Overall application functionality and stability | 12 | Partial to strong | Build/test pass; Identity, roles, API, DTOs, upload backend/UI, Google login config, AJAX search, file logging, responsive UI, global search, Playwright E2E, and domain business rules exist. Missing deployment, AI, and MCP. | Full smoke demo: dashboard, auth, CRUD, API, upload, search, Google login if secrets configured, and Playwright E2E run. | `dotnet build`; `dotnet test`; dedicated E2E command. | Finish deployment, optional AI/MCP, and final defense prep while keeping Lab 5 functionality intact. | Medium: new features could destabilize auth, uploads, or navigation if not staged. |
| Oral verification of code understanding | 40 | Not a repository feature | No code change can earn this directly; documentation can prepare evidence and talking points. | Explain architecture, DbContext relationships, DTO mapping, auth/roles, tests, upload storage, and planned upgrade choices. | N/A. | Prepare defense notes after final audit. | High if the implementer cannot explain the code paths and tradeoffs. |

## Recommended Staged Implementation Plan

Follow this order unless a later audit finds a hard dependency. Every implementation stage should preserve the NAS Indexer domain and avoid unrelated teaching-example artifacts.

### 1. Logging

- Status: implemented.
- Expected points: 2.
- Recommended Codex model/effort: GPT-5.5 Medium.
- Files affected: `Program.cs`, `Services/AppFileLogger.cs`, `Middleware/RequestFileLoggingMiddleware.cs`, `.gitignore`, `Labosi-ASP.NET.Tests/CustomWebApplicationFactory.cs`, `Labosi-ASP.NET.Tests/Api/RequestFileLoggingTests.cs`.
- Verification commands: `dotnet build`; `dotnet test --logger "console;verbosity=minimal"`; `git diff --check`; manual log file check.
- Manual demonstration: perform login, create/edit/delete a domain record, upload/delete an attachment, then show redacted audit/log entries.
- Rollback or stop conditions: logging captures secrets/OIB/JMBG/raw uploaded file paths, causes startup failure, or requires an unapproved package.

#### Manual Logging Demo

1. Run the app from the repository root with `dotnet run`.
2. Open the dashboard or home page, then open `/api/tags?query=demo-safe`.
3. Optionally log in and open an authenticated details page so the `user=` field shows the authenticated username/email instead of `anonymous`.
4. Open `logs/app-yyyyMMdd.log`.
5. A sample request line proves the grading criterion because it includes a timestamp, `level=INFO`, HTTP method, request path, safe query string, response status, elapsed milliseconds, and `user=...`.
6. Demonstrate safety by noting that the middleware never reads request bodies, cookies, authorization headers, uploaded file contents, or multipart form data, and unsafe query keys such as `password`, `ClientSecret`, `token`, or OAuth `code` are logged as `query=[redacted]`.

### 2. Responsive UI Audit And Fixes

- Status: implemented.
- Expected points: 2.
- Recommended Codex model/effort: GPT-5.5 Medium; invoke explicit UX/UI sub-agent before changing UI.
- Files affected: `Views/Shared/_Layout.cshtml`; `Views/Admins/Index.cshtml`; `Views/Directories/Index.cshtml`; `Views/FileChangeLogs/Index.cshtml`; `Views/FileItems/Details.cshtml`; `Views/FileItems/Index.cshtml`; `Views/NasServers/Index.cshtml`; `Views/ScanJobs/Index.cshtml`; `Views/Tags/Index.cshtml`; `wwwroot/css/site.css`; `wwwroot/css/lab4.css`; `wwwroot/js/file-attachments.js`.
- Verification commands: `dotnet build`; `dotnet test --logger "console;verbosity=minimal"`; browser/mobile smoke pass; later Playwright screenshot checks if Playwright is added.
- Manual demonstration: inspect dashboard, all list pages, forms, details, auth pages, and attachment UI at desktop and mobile widths.
- Rollback or stop conditions: nav becomes harder to use, tables lose data, forms overlap, auth/upload controls break, or Lab 5 behavior changes.

#### Responsive Audit And Fix Summary

- Inspected shared layout/navigation, Home dashboard, entity list tables, create/edit form patterns, details/action areas, FileItem attachments, Identity register/external-login pages, validation summaries, and long path/name/tag display.
- Added a native `<details>` mobile menu so the side navigation collapses at phone width while remaining visible on desktop and tablet widths.
- Added Bootstrap-compatible `table-responsive` wrappers to list/detail tables and the AJAX-rendered attachment table while keeping the existing dark table styling.
- Tightened CSS for 375px screens: actions and attachment controls stack full-width, cards/details collapse cleanly, table wrappers scroll horizontally, file input fits its drop zone, search controls get consistent spacing, and long cell text can wrap.
- Improved 1920px desktop use by centering the content deck and allowing a wider maximum content area for data-heavy pages.
- No controllers, routes, DTOs, models, migrations, Identity logic, Google login behavior, upload backend behavior, or API behavior were changed.

#### Manual Responsive Demo

1. Run the app from the repository root with `dotnet run`.
2. Open browser DevTools and set the viewport to 375px wide.
3. Open the mobile menu, then visit Dashboard, NAS Servers or Files list, a create/edit form, a details page, FileItem attachments, Register, and External Login if available.
4. Confirm tables scroll inside their wrappers instead of widening the page, action buttons stack, forms fit one column, long paths/names wrap, and attachment upload/list/delete controls remain usable.
5. Set the viewport to 768px wide and confirm navigation/content remain usable, tables are still contained, and forms/cards use tablet-friendly spacing.
6. Set the viewport to desktop width, including 1920px if available, and confirm the desktop side navigation and wider centered content deck remain visually consistent.
7. Explain that the UI uses Bootstrap-compatible `table-responsive` class names plus small existing-site CSS rules rather than a new frontend framework.

### 3. Global Search

- Status: implemented.
- Expected points: 2.
- Recommended Codex model/effort: GPT-5.5 Medium.
- Files affected: `Controllers/SearchController.cs`; `ViewModels/GlobalSearchViewModel.cs`; `Views/Search/Index.cshtml`; `Views/Shared/_Layout.cshtml`; `wwwroot/css/site.css`; `Labosi-ASP.NET.Tests/Api/GlobalSearchTests.cs`; `docs/project-upgrade-checklist.md`; `lab-1/agent_log.txt`.
- Verification commands: `dotnet build`; `dotnet test --logger "console;verbosity=minimal"`; `git diff --check`; targeted integration tests for search results and safe anonymous links.
- Manual demonstration: one layout search query returns menu/page shortcuts plus matching NAS servers, scan jobs, directories, files, tags, and attachments when present.
- Rollback or stop conditions: search exposes unauthorized details, raw passwords, secrets, OIB/JMBG, or breaks page-local search.

#### Global Search Implementation Summary

- Added `GET /Search?q=...` as a public MVC route using `SearchController`.
- Added a global search form to the shared layout so search is available from the main navigation on desktop, tablet, and mobile.
- Added grouped result view models and a responsive results page.
- Search covers menu/page entries, NAS servers, directories, file items, file tags, scan jobs, and file attachments by original filename.
- Empty or whitespace queries return a friendly message and skip database-heavy search.
- Queries are trimmed and capped at 80 characters.
- EF Core search uses `AsNoTracking`, `EF.Functions.Like`, ordering, and `Take(10)` per group.
- Search results do not include passwords, Google secrets, cookies, request bodies, raw hidden data, or uploaded file contents.
- Anonymous users get safe public list links for protected entity data; authenticated users get details links where existing MVC authorization already allows details pages.

#### Manual Global Search Demo

1. Run the app from the repository root with `dotnet run`.
2. Use the global search box in the side navigation or open `/Search?q=files`.
3. Search for a known NAS server name and show the `NAS servers` group.
4. Search for a known file name and show the `Files` group.
5. Search for a known tag and show the `Tags` group.
6. Search for a scan root path or server name tied to a scan job and show the `Scan jobs` group.
7. Search for a menu term such as `files`, `servers`, or `dashboard` and show the `Pages` group.
8. Search for a nonsense term and show the friendly empty result.
9. Explain that anonymous result links go to safe public list pages, while authenticated users can use detail links where existing rules permit them.

### 4. Playwright End-to-End Scenario

- Status: implemented.
- Expected points: up to 3 extra.
- Recommended Codex model/effort: GPT-5.5 Medium.
- Files affected: `Labosi-ASP.NET.E2ETests/Labosi-ASP.NET.E2ETests.csproj`; `Labosi-ASP.NET.E2ETests/E2EWebAppFixture.cs`; `Labosi-ASP.NET.E2ETests/AdminTagWorkflowE2ETests.cs`; `Labosi-ASP.NET.sln`; `Labosi-ASP.NET.csproj`; `.gitignore`; `Views/Shared/_Layout.cshtml`; `docs/project-upgrade-checklist.md`; `lab-1/agent_log.txt`.
- Verification commands: `dotnet build`; `dotnet test --logger "console;verbosity=minimal"`; `dotnet test Labosi-ASP.NET.E2ETests/Labosi-ASP.NET.E2ETests.csproj --logger "console;verbosity=minimal"`; `git diff --check`.
- Browser install command used on this Windows machine: `powershell -ExecutionPolicy Bypass -File Labosi-ASP.NET.E2ETests\bin\Debug\net8.0\playwright.ps1 install chromium --no-shell`.
- Manual demonstration: show the 12 commented browser steps in `AdminTagWorkflowE2ETests.cs`, run the E2E command, and explain the temp DB/Admin/localhost isolation.
- Rollback or stop conditions: test requires real external secrets, mutates non-test data, is flaky on clean checkout, or needs broad app behavior changes.

#### Playwright E2E Scenario Coverage

The automated browser scenario performs these 12 visible user-level steps:

1. Admin opens the public home page.
2. Admin opens the local login page.
3. Admin logs in with the isolated E2E Admin account.
4. Admin uses global search for the Tags page.
5. Admin opens the Tags page from search results.
6. Admin opens the Create Tag form.
7. Admin creates a safe NAS Indexer tag.
8. Admin searches globally for the new tag.
9. Admin opens the tag details page from global search.
10. Admin opens the edit page.
11. Admin changes the Description field and saves.
12. Admin verifies the updated content appears back on the list.

#### Playwright Run Notes

- The E2E fixture creates a temp SQLite database and does not use the developer's local `nas-indexer.db`.
- The fixture seeds an Admin Identity user directly into the temp database and does not rely on user-secrets or Google login.
- The app is started as a local process on a random `127.0.0.1` port.
- Temp upload and log roots are used and cleaned up where Windows file locks allow.
- Browser binaries are installed under the user profile by Playwright and are not committed to the repository.
- `pwsh` was not available on this machine, so Windows PowerShell was used. The initial normal install hit a disk-space error while downloading the extra Chromium headless shell; `install chromium --no-shell` completed and the test launches the installed Chrome-for-Testing binary headlessly.

### 5. Cloud Deployment

- Expected points: 3.
- Recommended Codex model/effort: GPT-5.5 Medium.
- Files likely affected: deployment docs/config, environment variable docs, optional Dockerfile or provider manifest, possibly database/storage configuration.
- Verification commands: `dotnet publish -c Release`; provider-specific smoke check; `dotnet test` before deploy.
- Manual demonstration: open deployed URL, log in/register, demonstrate CRUD/search/upload within provider limits.
- Rollback or stop conditions: tracked secrets, broken local development, nonpersistent database/upload assumptions not documented, or provider requires incompatible runtime changes.

### 6. Optional AI Integration

- Expected points: 3.
- Recommended Codex model/effort: GPT-5.5 Medium or High depending on provider/API uncertainty.
- Files likely affected: service abstraction, controller/API endpoint, FileItem/Tag data-entry view, config docs, tests for fallback/validation.
- Verification commands: `dotnet build`; `dotnet test`; mocked/no-secret tests; manual run with user-secrets if configured.
- Manual demonstration: ask AI to suggest FileItem metadata/tags from NAS path/name, review generated suggestion, save only validated user-approved data.
- Rollback or stop conditions: secrets in tracked files, direct unvalidated writes, unavailable AI breaks normal CRUD, or generated data bypasses domain validation.

### 7. Optional MCP Integration

- Expected points: 2.
- Recommended Codex model/effort: GPT-5.5 High if implementing server/tooling from scratch.
- Files likely affected: MCP host/tool project or scripts, docs, safe read-only adapters, maybe tests.
- Verification commands: build/test for any MCP component; manual connection from an agentic IDE.
- Manual demonstration: agentic IDE connects to MCP and reads NAS Indexer data or performs a narrowly safe action.
- Rollback or stop conditions: unclear grading acceptance, unsafe write tools, auth bypass, or large unrelated architecture changes.

### 8. Final Audit And Defense Preparation

- Expected points: improves stability score and oral verification readiness; oral criterion is 40 points.
- Recommended Codex model/effort: GPT-5.5 Medium.
- Files likely affected: `docs/`, `lab-1/agent_log.txt`, possibly final smoke-test notes.
- Verification commands: `dotnet build`; `dotnet test --logger "console;verbosity=minimal"`; `git diff --check`; search for secrets; deployment/E2E commands if added.
- Manual demonstration: rehearse architecture and feature flow from request to controller/service/repository/DbContext/tests.
- Rollback or stop conditions: final changes modify application behavior instead of documenting or fixing verified defects.

## Score Projection

These are conservative planning estimates, not guaranteed grading outcomes.

Starting lab points: 16.

Conservative current upgrade evidence:

- API endpoint tests: 2.
- CRUD without errors: 2.
- Responsive UI evidence: 1 of 2 until manually/browser verified.
- Overall functionality/stability: about 8 of 12 based on passing build/tests and implemented Lab 5 features.
- Current subtotal estimate: 16 + 13 = 29.

Path to at least 50:

- Add logging: +2 completed.
- Complete responsive audit/fixes and evidence: +1 completed.
- Add global search: +2 completed.
- Add cloud deployment: +3.
- Add Playwright scenario with at least partial credit: +2 to +3 completed.
- Improve final stability through verified implementation: +1 to +2.
- Oral verification target needed after those items: about 10 to 11 of 40.
- Conservative total path: 16 + 22 to 24 implementation/extra + 10 to 12 oral = 50 to 52.

Safer target with margin:

- Complete logging, responsive verification, global search, deployment, full Playwright, AI integration, and MCP integration: about +16 to +17 beyond the current conservative 13.
- Target oral verification: at least 18 to 22 of 40.
- Safer total projection: 16 + 28 to 30 implementation/extra + 18 to 22 oral = 62 to 68.

## Defense Preparation Topics

- Explain why `FileChangeLog` is read-only and how tests prove write endpoints are unavailable.
- Explain DTO mapping and why API responses do not expose `NasServer.Password` or `SystemAdmin.Password`.
- Explain Identity roles: anonymous list/search, authenticated details, Manager create/edit/upload, Admin delete.
- Explain attachment storage safeguards: generated filename, allow-list, max size, path traversal guard.
- Explain repository search boundaries: current page-local search versus planned global search.
- Explain how `CustomWebApplicationFactory` isolates integration tests with SQLite in-memory and test auth.
- Explain deployment storage/database tradeoffs before choosing a provider.

## Final Audit Notes

- No application features were implemented during this audit.
- Existing Lab 5 functionality should be preserved in all future stages.
- Any future UI/UX screen changes must first invoke the explicit UX/UI sub-agent and log the invocation plus summary in `lab-1/agent_log.txt`.
