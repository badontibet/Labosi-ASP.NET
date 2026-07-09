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

## Visual Polish For Stability Demo

Implemented on 2026-07-09 with UI/documentation-only changes.

- Dashboard readability: metric cards now use larger numbers, short contextual helper text, more breathing room, and subtle health accents for active/completed/running/failed counts.
- Sidebar clarity: primary navigation now shows a route-aware active state, stronger hover/focus treatment, larger readable targets, and clearer separation between brand, global search, navigation, account links, and the repository status badge.
- Layout width: the main content deck is centered with a slightly narrower maximum width so desktop pages feel more intentional while existing table wrappers preserve data-heavy pages.
- Action areas: primary actions, row actions, destructive links, hero action groups, and form action groups have consistent sizing, spacing, wrapping, and mobile stacking.
- Badges/status indicators: scan status badges keep the existing color language and add a small dot affordance so completed/running/failed/pending states are visually distinct beyond text alone.
- Tables/lists: table rows have increased padding, safer long-text wrapping, clearer row endings, and existing `table-responsive` wrappers remain intact.
- Quick Links: dashboard quick links now read as action cards with a title and short purpose statement for demo navigation.
- Account consistency: Register and external-login `.form-panel` sections receive the same dark framed visual treatment as MVC CRUD forms without changing Identity behavior.

### Professor Demo Pages For Visual/Stability Criterion

Show these pages in order for the "Overall application functionality and stability" discussion:

1. Dashboard/Home: explain metrics, scan watchlist, recent file changes, and quick-link action cards.
2. NAS Servers list/details: show active sidebar state, search panel, responsive table, status badge, metadata cards, and details actions.
3. File Items list/details/edit: show long path wrapping, file metadata, edit form consistency, and authenticated attachment section.
4. Global search results: search for a menu term, file/tag/server term, and a nonsense term to show grouped results and empty state.
5. File attachment section: as an authenticated Admin/Manager, show upload/list/delete controls and responsive containment.
6. Login/Register pages: show dark operations-center visual consistency, framed account form, validation area styling, and unchanged Identity/Google login behavior.

### Stability Criterion Support

- The polish improves perceived stability by making primary navigation, current location, statuses, actions, and empty/loading states easier to identify during a live demo.
- The work is intentionally CSS/Razor-only and preserves existing routes, forms, validation, authorization, Identity, Google login, upload behavior, API behavior, domain models, DTOs, DbContext, migrations, and tests.
- The implementation complements the existing automated safety net: MVC/API integration tests, logging tests, global search tests, and the Playwright Admin workflow remain the objective stability proof.

## MAX Impression Dashboard And Activity Upgrade

Implemented on 2026-07-09 to make the NAS Operations Control Center feel live, trustworthy, and demo-ready.

### Improvements Made

- Added a centralized `FileChangeLogService` for safe file activity creation.
- FileItem MVC create and edit actions now create real `FileChangeLog` rows after successful saves.
- FileItem API create and edit actions now create real `FileChangeLog` rows after successful saves.
- Attachment upload and delete API actions now add file-level `Modified` activity using only safe metadata: original filename and byte count.
- Dashboard Recent File Changes now reads newest entries directly from the `FileChangeLogs` table with `AsNoTracking`, ordered newest-first and capped for dashboard use.
- Dashboard now includes `Data as of`, deterministic Operational Status, database-backed metrics, scan warning count, active server ratio, scan progress bars, activity timeline rows, and expanded quick-link action cards.
- The visible application UI was cleaned up after the MAX pass so it remains domain-focused; the former Demo Readiness panel and assignment-style capability checklist are no longer shown on the dashboard.
- Subtle live-style polish was added with CSS-only motion: repository feed dot pulse, critical/running/failed badge glow, active scan progress stripe motion, dashboard section entrance, and hover transitions for navigation, cards, quick links, and timeline rows.
- Accessibility note: `prefers-reduced-motion: reduce` disables/minimizes these animations and transitions.
- Dashboard health is deterministic:
  - `Critical` when failed scans exist.
  - `Warning` when scans are running, servers are inactive, or no active server exists.
  - `Healthy` when active servers exist and no scan failures are recorded.

### Why Recent File Changes Did Not Update Before

Before this upgrade, the dashboard displayed `FileItem.ChangeLogs`, but normal FileItem create/edit actions did not create `FileChangeLog` records. Seeded or manually inserted audit records could appear, but real user edits often produced no new dashboard activity. The dashboard now reads the audit table directly, and real create/edit/upload/delete-attachment actions write audit rows through one centralized service.

### User Actions That Create Activity

- MVC FileItem create: `Created`.
- MVC FileItem edit: `Modified`.
- API FileItem create: `Created`.
- API FileItem edit: `Modified`.
- API attachment upload: file-level `Modified`.
- API attachment delete: file-level `Modified`.

FileItem delete activity was intentionally not added because existing business rules block deleting files that have change logs. Since create/edit now correctly create change logs, adding a delete log would require changing that rule or creating orphan-style audit data. The existing delete behavior was preserved.

### Dashboard Demo Script

1. Log in as Admin.
2. Open Dashboard and point out `Data as of`, Operational Status, metrics, scan progress, Recent File Changes, and Quick Links.
3. Open Files.
4. Edit an existing FileItem with a safe visible metadata change.
5. Save.
6. Return to Dashboard.
7. Show the new `Modified` activity with the logged-in user and timestamp.
8. Use Global Search for that file, tag, or server.
9. Open File details.
10. Upload a small attachment if using an Admin/Manager account and attachment storage is configured.
11. Open Dashboard or Change Logs again and show the attachment activity.
12. Open `logs/app-yyyyMMdd.log` to show request logging evidence.
13. Run or show the Playwright E2E test as automated browser proof.

### Dashboard UI Cleanup Demo Notes

1. Open Dashboard.
2. Confirm no visible `Demo Readiness`, assignment checklist, grading, professor, API/DTO, Identity role, File logging, or Playwright marketing text appears in the app UI.
3. Confirm the repository feed dot gently pulses.
4. Confirm Critical, Running, and Failed statuses have subtle emphasis.
5. Confirm active scan progress bars use a restrained stripe motion.
6. Hover dashboard metric cards, quick-link cards, timeline rows, and sidebar links to show small product-like feedback.
7. Confirm Recent File Changes still displays real database activity.
8. Confirm mobile and desktop layouts remain stable.

### Oral Defense Notes

- Activity logging is centralized so MVC, API, and attachment actions use one safe implementation and avoid inconsistent audit text.
- `FileChangeLog` remains read-only from the public API: there are still no public POST, PUT, or DELETE endpoints for file change logs.
- Dashboard health is computed from real database values, not fake random demo data.
- Activity values intentionally avoid passwords, request bodies, cookies, Google credentials, authorization headers, uploaded file contents, and multipart payloads.
- No migration was required because the existing `FileChangeLog` table already supports the needed activity records.

### Files Changed By MAX Upgrade

- `Services/FileChangeLogService.cs`
- `Controllers/HomeController.cs`
- `Controllers/FileItemsController.cs`
- `Controllers/Api/FileItemsApiController.cs`
- `Controllers/Api/FileAttachmentsApiController.cs`
- `ViewModels/DashboardViewModel.cs`
- `Views/Home/Index.cshtml`
- `wwwroot/css/site.css`
- `Labosi-ASP.NET.Tests/Api/FileItemsApiTests.cs`
- `Labosi-ASP.NET.Tests/Api/FileAttachmentsApiTests.cs`
- `Labosi-ASP.NET.E2ETests/AdminTagWorkflowE2ETests.cs`
- `docs/project-upgrade-checklist.md`
- `lab-1/agent_log.txt`

### Known Limitations

- FileItem delete activity is skipped to preserve the existing delete guard for files with change logs.
- Attachment activity is represented as file-level `Modified` because `FileChangeLog` links to `FileItem`, not directly to `FileAttachment`.
- Dashboard is request/refesh based. No SignalR, timers, fake random activity, or background workers were added.
- The Playwright scenario now includes an isolated Admin FileItem edit, Dashboard return, Recent File Changes verification, and global search for the edited file before continuing through the existing tag workflow.

## Final Pre-Defense Polish

Completed on 2026-07-09 as a UI/documentation-only final pass.

- Dashboard remains domain-focused as the NAS Operations Control Center.
- Visible assignment-style wording was removed from the app UI, including the former Demo Readiness panel and implementation-checklist capability chips.
- Dashboard wording now uses operator language such as Repository feed, Operational Status, Scan Watchlist, Recent File Changes, and Quick Links.
- Recent File Changes remains a rich audit timeline with action badge, file name, author, timestamp, context, and an Audit record link.
- Recent File Changes now also exposes a `View all changes` link to the Change Logs list page.
- Quick Links remain card-style operator shortcuts with title, count/context, and Open action.
- Subtle CSS-only live polish remains in place: repository/status dot pulse, critical/running/failed badge emphasis, progress bar shimmer, card hover, quick-link hover, timeline row hover, and dashboard section entrance.
- `prefers-reduced-motion: reduce` minimizes animations and transitions.
- No backend behavior, API behavior, Identity/Google behavior, upload behavior, FileChangeLog behavior, packages, or migrations were changed in this final polish step.

### Final Dashboard Demo Route

1. Start the app.
2. Open `/` or `/dashboard`.
3. Show Repository feed, Operational Status, metrics, Scan Watchlist progress bars, Recent File Changes, `View all changes`, and Quick Links.
4. Edit a FileItem as an Admin or Manager, return to Dashboard, and show the new Recent File Changes entry.
5. Open `/FileChangeLogs` from `View all changes` to show the full audit list.
6. Use Global Search from the sidebar for the edited file, a tag, and a server.

### Oral Defense Reference

- Use `docs/oral-defense-map.md` as the practical code navigation script.
- It maps each defense topic to exact files/folders, a short explanation, a likely professor question, a strong short answer, and a manual demo step.

### Final Verification Commands

- `dotnet build`
- `dotnet test --logger "console;verbosity=minimal"`
- `dotnet test Labosi-ASP.NET.E2ETests/Labosi-ASP.NET.E2ETests.csproj --logger "console;verbosity=minimal"`
- `git diff --check`
- Manual browser inspection at desktop, 768px, and 375px.

### Demo Account And Secrets Notes

- Use a locally configured Admin or Manager account for the manual dashboard/FileItem edit demo.
- The Playwright E2E test seeds its own isolated Admin user in a temporary database and does not need Google login or real secrets.
- Google ClientId and ClientSecret must remain in user-secrets or environment configuration, never in tracked files.
- Do not commit generated `logs/`, runtime uploads, Playwright reports, screenshots, videos, traces, browser binaries, local database files, `bin/`, or `obj/`.

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
