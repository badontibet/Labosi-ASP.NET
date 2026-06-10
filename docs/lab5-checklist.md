# Lab 5 Checklist - NAS Indexer API, Auth, Uploads, Tests

Status: planning and repository audit only. No application code has been implemented by this document.

Branch audited: `lab-5`

Source material: `C:\Users\Domagoj\Downloads\Lab5.md`

## Scope Decision

Lab 5 examples are written around a quiz domain. This project must keep the existing NAS Indexer domain and translate every requirement to the current entities, relationships, and business rules.

Do not add quiz/course/category teaching-example models, controllers, DTOs, routes, migrations, seed data, or views. The API, Identity, upload, external login, and integration tests must stay inside the existing NAS Indexer domain and use the entities already present in the project.

## Current Repository Inventory

### Project Structure

| Area | Current files/folders |
|---|---|
| App entry/config | `Program.cs`, `appsettings.json`, `Labosi-ASP.NET.csproj`, `Labosi-ASP.NET.sln` |
| Data | `Data/NasIndexerDbContext.cs`, `Data/NasIndexerDbInitializer.cs` |
| Models | `Models/NasServer.cs`, `Models/ScanJob.cs`, `Models/DirectoryItem.cs`, `Models/FileItem.cs`, `Models/FileTag.cs`, `Models/FileChangeLog.cs`, `Models/SystemAdmin.cs`, `Models/ScanStatus.cs`, `Models/ChangeType.cs` |
| Controllers | `Controllers/HomeController.cs`, `Controllers/NasServersController.cs`, `Controllers/ScanJobsController.cs`, `Controllers/DirectoriesController.cs`, `Controllers/FileItemsController.cs`, `Controllers/FileChangeLogsController.cs`, `Controllers/TagsController.cs`, `Controllers/AdminsController.cs` |
| Repositories | `Repositories/INasRepository.cs`, `Repositories/EfNasRepository.cs`, `Repositories/MockNasRepository.cs` |
| View models | `ViewModels/*FormViewModel.cs`, `ViewModels/AutocompleteDropdownViewModel.cs`, `ViewModels/DateTimePickerViewModel.cs`, `ViewModels/DashboardViewModel.cs` |
| Views | MVC Razor folders for NAS servers, scan jobs, directories, files, change logs, tags, admins, shared partials, home dashboard |
| JavaScript/CSS | `wwwroot/js/lab4.js`, `wwwroot/css/lab4.css`, `wwwroot/css/site.css` |
| Migrations | `Migrations/20260519182421_InitialCreate.cs`, designer, `NasIndexerDbContextModelSnapshot.cs` |
| Docs/log | `docs/lab4-checklist.md`, `docs/lab4-business-rules.md`, `docs/migrations-readme.md`, `lab-1/agent_log.txt` |

### Current DbContext Sets And Relationships

`NasIndexerDbContext` currently exposes:

| DbSet | Relationship summary |
|---|---|
| `NasServers` | One server has many `ScanJobs`; many-to-many with `SystemAdmins` through `SystemAdminNasServers` |
| `ScanJobs` | Belongs to one `NasServer`; has many scanned `DirectoryItems` |
| `DirectoryItems` | Optional `ScanJob`; optional self-referencing parent; has many child directories and files |
| `FileItems` | Belongs to one directory; many-to-many with `FileTags`; has many `FileChangeLogs` |
| `FileTags` | Many-to-many with files through `FileItemTags` |
| `FileChangeLogs` | Belongs to one file; audit/history records |
| `SystemAdmins` | Many-to-many with NAS servers |

Current delete behavior includes EF cascade/set-null/restrict rules, but Lab 4 business rules already add application-level delete blocks to prevent destructive UI actions.

### Current MVC Controller Surface

| Entity | Current MVC controller | Current actions/patterns |
|---|---|---|
| `NasServer` | `NasServersController` | `Index`, `Search`, `Details`, `Create`, `Edit`, `Delete`; restricted password-safe CRUD; delete blocked when scan jobs or managed admins exist |
| `ScanJob` | `ScanJobsController` | `Index`, `Search`, `Details`, `Create`, `Edit`, `Delete`, `NasServerAutocomplete`; delete blocked when scanned directories exist |
| `DirectoryItem` | `DirectoriesController` | `Index`, `Search`, `Details`, `Create`, `Edit`, `Delete`, parent directory autocomplete, scan job autocomplete; delete blocked when children/files exist; parent cycle checks |
| `FileItem` | `FileItemsController` | `Index`, `Search`, `Details`, `Create`, `Edit`, `Delete`, directory autocomplete; delete blocked when change logs exist |
| `FileTag` | `TagsController` | `Index`, `Search`, `Details`, `Create`, `Edit`, `Delete`; delete blocked when assigned to files |
| `FileChangeLog` | `FileChangeLogsController` | `Index`, `Search`, `Details` only; read-only audit surface |
| `SystemAdmin` | `AdminsController` | `Index`, `Search`, `Details`, `Create`, `Edit`, `Delete`; password-safe restricted CRUD; delete blocked when managing servers |

### Current JavaScript/AJAX Patterns

`wwwroot/js/lab4.js` already provides reusable patterns that Lab 5 should preserve and extend only where useful:

| Pattern | Current usage |
|---|---|
| AJAX search | List pages use `data-lab4-search` and `data-lab4-search-form`; JS fetches row partials and updates result containers |
| AJAX filters | File change log search supports named filter fields such as `changeType` |
| Autocomplete | Shared `_AutocompleteDropdown.cshtml` uses visible text plus hidden ID; endpoints return `{ id, text }` |
| Blur validation | Shared validation for required, max length, hex color, nonnegative numbers, integer range, IP address, email, date-time, date range, autocomplete |
| Delete confirmation | Delete forms use `data-lab4-delete-form` and optional `data-confirm-message` |
| Date/time entry | Shared `_DateTimePicker.cshtml` and `DateTimeInputParser` avoid native browser date inputs |

Lab 5 API work should not break these MVC/AJAX patterns. Upload UI can reuse the same data-attribute style even if Dropzone or an alternative library is added later.

### Current Validation Patterns

| Pattern | Current files |
|---|---|
| Form-specific view models instead of direct entity binding | `ViewModels/NasServerFormViewModel.cs`, `ScanJobFormViewModel.cs`, `DirectoryItemFormViewModel.cs`, `FileItemFormViewModel.cs`, `FileTagFormViewModel.cs`, `SystemAdminFormViewModel.cs` |
| `IValidatableObject` cross-field checks | Scan job dates/progress, directory dates/self-parent, file dates, admin dates, NAS server IP/date |
| Server-side relationship validation | Controllers verify selected NAS server, directory, scan job, tags, and managed servers |
| Sensitive-field omission | NAS server and system admin UI avoids raw `Password` editing/display |
| Audit read-only rule | `FileChangeLog` has no Create/Edit/Delete MVC surface |

## Entity API Decisions

### Full API CRUD Entities

These entities should receive full JSON API CRUD because business rules allow create/update/delete with guards:

| Entity | Planned API route | CRUD decision | Required delete/update guards |
|---|---|---|---|
| `NasServer` | `api/nas-servers` | Full restricted CRUD | Do not expose or accept `Password`; delete blocked if scan jobs or managed admins exist |
| `ScanJob` | `api/scan-jobs` | Full CRUD | Valid NAS server required; `EndTime >= StartTime`; `ProcessedFiles <= TotalFiles`; delete blocked if directories exist |
| `DirectoryItem` | `api/directories` | Full CRUD | Valid optional scan job/parent; no self-parent or cycles; `ModifiedDate >= CreatedDate`; delete blocked if child dirs/files exist |
| `FileItem` | `api/files` | Full metadata CRUD | Valid directory; valid selected tag IDs; size nonnegative; `ModifiedDate >= CreatedDate`; delete blocked if change logs exist |
| `FileTag` | `api/tags` | Full CRUD | Valid hex color; delete blocked if assigned to files |
| `SystemAdmin` | `api/system-admins` | Full password-safe NAS domain CRUD where business rules allow | Keep `SystemAdmin` as a NAS domain entity; do not use `SystemAdmin.Password` for login; do not expose or accept `Password`; validate managed NAS server IDs; delete blocked if assigned to servers |

### Read-Only API Entity

| Entity | Planned API route | Decision | Reason |
|---|---|---|---|
| `FileChangeLog` | `api/file-change-logs` | Read-only: `GET all/search`, `GET by id`; no normal `POST`, `PUT`, or `DELETE` | This is audit/history data. Existing Lab 4 rule treats it as append-only/read-only. API must not let clients rewrite history. |

If the instructor interprets "CRUD for all entities" literally, document this exception in the API and tests: `POST`, `PUT`, and `DELETE` should be absent or return `405 Method Not Allowed`/`403 Forbidden`, with integration tests proving the read-only rule.

## Attachment Upload Decision

Lab 5 says upload must be attached to a concrete quiz. In NAS Indexer, the equivalent concrete aggregate should be `FileItem`.

Planned translation:

| Original Lab 5 wording | NAS Indexer implementation |
|---|---|
| Upload files for a quiz | Upload attachments for a `FileItem` metadata record |
| Store file on disk | Store binary attachment under `wwwroot/uploads/files/{fileItemId}/` or a non-public storage root with controlled download endpoint |
| Store metadata/path in DB | Add `FileAttachment`/`Attachment` entity linked to `FileItem` |
| Load file list by AJAX | Add attachment list endpoint and render a partial or JSON-driven list on `FileItems/Edit`/`Details` |
| Delete existing files | Add authorized delete endpoint for file attachments |

Why `FileItem` is the right host:

- It is the existing entity that represents file metadata in the NAS index.
- It already owns tags and change logs, so related document evidence fits naturally.
- Uploading to `DirectoryItem` or `NasServer` would be less specific and could confuse indexed NAS files with app-managed attachments.
- `FileChangeLog` should stay read-only audit data and is not a suitable upload owner.

Planned upload model:

```csharp
public class FileAttachment
{
    public int Id { get; set; }
    public int FileItemId { get; set; }
    public FileItem FileItem { get; set; } = null!;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UploadedByUserId { get; set; }
}
```

## Planned DTO Classes

Create DTOs under a new folder such as `Dtos/` or `Dtos/Api/`. Keep DTOs separate from MVC form view models.

### Shared/Nested DTOs

| DTO | Purpose |
|---|---|
| `NasServerSummaryDto` | Nested server reference without password |
| `ScanJobSummaryDto` | Nested scan job reference for directories |
| `DirectorySummaryDto` | Nested directory reference for files/child directories |
| `FileTagSummaryDto` | Nested tag reference for file lists |
| `FileItemSummaryDto` | Nested file reference for change logs and attachments |
| `SystemAdminSummaryDto` | Nested admin reference without password |
| `ApiErrorDto` or default validation problem | Optional consistent error wrapper if not relying on `[ApiController]` validation problem responses |

### Entity DTOs

| Entity | Response DTOs | Create/Update DTOs | Notes |
|---|---|---|---|
| `NasServer` | `NasServerDto`, `NasServerSummaryDto` | `CreateNasServerDto`, `UpdateNasServerDto` | Exclude `Password`; include scan job/admin counts or summaries |
| `ScanJob` | `ScanJobDto`, `ScanJobSummaryDto` | `CreateScanJobDto`, `UpdateScanJobDto` | Include nested `NasServerSummaryDto`; expose `ScanStatus` as string or enum consistently |
| `DirectoryItem` | `DirectoryItemDto`, `DirectorySummaryDto` | `CreateDirectoryDto`, `UpdateDirectoryDto` | Include parent summary, scan job summary, counts for files/subdirectories |
| `FileItem` | `FileItemDto`, `FileItemSummaryDto` | `CreateFileItemDto`, `UpdateFileItemDto` | Include directory summary, tag summaries, change log count, attachment count later |
| `FileTag` | `FileTagDto`, `FileTagSummaryDto` | `CreateFileTagDto`, `UpdateFileTagDto` | Include file count, not full file graph by default |
| `FileChangeLog` | `FileChangeLogDto` | None for normal API | Read-only DTO only |
| `SystemAdmin` | `SystemAdminDto`, `SystemAdminSummaryDto` | `CreateSystemAdminDto`, `UpdateSystemAdminDto` | NAS domain CRUD DTOs only; exclude `Password`; do not use these DTOs for ASP.NET Core Identity login/registration |
| `FileAttachment` | `FileAttachmentDto` | Upload uses `IFormFile`; optional `CreateFileAttachmentMetadataDto` only if metadata fields are added | Linked to `FileItem` |

### Mapping

Use simple manual mapping first:

- private mapping methods per API controller for small controllers, or
- a shared `ApiDtoMapper`/extension methods if duplication grows.

Avoid returning EF entities directly because `NasServer.Password`, `SystemAdmin.Password`, navigation cycles, and many-to-many relationships make direct serialization risky.

## Planned API Controllers

Create controllers deriving from `ControllerBase`, marked with `[ApiController]` and attribute routes.

| Controller | Route | Planned endpoints |
|---|---|---|
| `NasServersApiController` | `api/nas-servers` | `GET ?query=`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` |
| `ScanJobsApiController` | `api/scan-jobs` | `GET ?query=&status=&nasServerId=`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` |
| `DirectoriesApiController` | `api/directories` | `GET ?query=&scanJobId=&parentId=`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` |
| `FileItemsApiController` | `api/files` | `GET ?query=&directoryId=&tagId=&extension=`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` |
| `FileTagsApiController` | `api/tags` | `GET ?query=`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` |
| `FileChangeLogsApiController` | `api/file-change-logs` | `GET ?query=&changeType=&fileId=`, `GET {id}` only |
| `SystemAdminsApiController` | `api/system-admins` | `GET ?query=&nasServerId=`, `GET {id}`, `POST`, `PUT {id}`, `DELETE {id}` using password-safe NAS domain DTOs |
| `FileAttachmentsApiController` | `api/files/{fileItemId}/attachments` | `GET`, `POST multipart/form-data`, `DELETE {attachmentId}` |

### Implemented API Status

| Slice | Status | Files | Notes |
|---|---|---|---|
| `FileTag` DTOs | Implemented | `Dtos/FileTagDto.cs`, `Dtos/CreateFileTagDto.cs`, `Dtos/UpdateFileTagDto.cs` | DTOs are separate from MVC form view models and do not expose EF entities directly |
| `FileTag` API controller | Implemented | `Controllers/Api/FileTagsApiController.cs` | Uses `[ApiController]`, `ControllerBase`, route `api/tags`, manual mapping, existing repository methods, validation limits matching `FileTagFormViewModel`, and existing delete guard for assigned files |
| `FileTag` API integration tests | Implemented | `Labosi-ASP.NET.Tests/CustomWebApplicationFactory.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs`, `Labosi-ASP.NET.Tests/Api/FileTagsApiTests.cs` | Tests call real HTTP endpoints through `HttpClient` using `WebApplicationFactory` and isolated SQLite in-memory database |
| `NasServer` DTOs | Implemented | `Dtos/NasServerDto.cs`, `Dtos/CreateNasServerDto.cs`, `Dtos/UpdateNasServerDto.cs` | DTOs expose non-sensitive server metadata plus counts only; `Password` is not exposed or accepted |
| `NasServer` API controller | Implemented | `Controllers/Api/NasServersApiController.cs` | Uses `[ApiController]`, `ControllerBase`, route `api/nas-servers`, manual mapping, existing repository methods, metadata-only create/update, and existing delete guard for scan jobs/managed admins |
| `NasServer` API integration tests | Implemented | `Labosi-ASP.NET.Tests/Api/NasServersApiTests.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Tests call real HTTP endpoints through `HttpClient`, verify CRUD/search/status codes, verify password is absent from JSON, verify submitted password is ignored, and verify update preserves stored password |
| `ScanJob` DTOs | Implemented | `Dtos/ScanJobDto.cs`, `Dtos/CreateScanJobDto.cs`, `Dtos/UpdateScanJobDto.cs` | DTOs expose scan metadata, `ScanStatus`, counts, and a non-sensitive nested NAS server summary |
| `ScanJob` API controller | Implemented | `Controllers/Api/ScanJobsApiController.cs` | Uses `[ApiController]`, `ControllerBase`, route `api/scan-jobs`, manual mapping, existing repository methods, status/NAS server/query filters, existing validation rules, and existing delete guard for scanned directories |
| `ScanJob` API integration tests | Implemented | `Labosi-ASP.NET.Tests/Api/ScanJobsApiTests.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Tests call real HTTP endpoints through `HttpClient` and verify CRUD/search/filter/status codes, invalid NAS server, invalid time/progress rules, and scanned-directory delete conflict |
| `DirectoryItem` DTOs | Implemented | `Dtos/DirectoryItemDto.cs`, `Dtos/CreateDirectoryItemDto.cs`, `Dtos/UpdateDirectoryItemDto.cs` | DTOs expose directory metadata, optional scan job/parent IDs, nested safe summaries, child directory count, and file count |
| `DirectoryItem` API controller | Implemented | `Controllers/Api/DirectoriesApiController.cs` | Uses `[ApiController]`, `ControllerBase`, route `api/directories`, manual mapping, existing repository methods, query/scanJobId/parentId filters, relationship validation, parent-cycle validation, MVC-compatible name/path normalization, and existing delete guard for child directories/files |
| `DirectoryItem` API integration tests | Implemented | `Labosi-ASP.NET.Tests/Api/DirectoriesApiTests.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Tests call real HTTP endpoints through `HttpClient` and verify CRUD/search/filter/status codes, invalid required/relationship/date rules, self-parent, descendant-cycle validation, and child/file delete conflicts |
| `FileItem` DTOs | Implemented | `Dtos/FileItemDto.cs`, `Dtos/CreateFileItemDto.cs`, `Dtos/UpdateFileItemDto.cs` | DTOs expose file metadata, required directory ID, nested safe directory summary, tag summaries, and change log count |
| `FileItem` API controller | Implemented | `Controllers/Api/FileItemsApiController.cs` | Uses `[ApiController]`, `ControllerBase`, route `api/files`, manual mapping, existing repository methods, query/directoryId/tagId/extension filters, required directory validation, tag ID validation/deduplication, date/size validation, tag replacement, and existing delete guard for change logs |
| `FileItem` API integration tests | Implemented | `Labosi-ASP.NET.Tests/Api/FileItemsApiTests.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Tests call real HTTP endpoints through `HttpClient` and verify CRUD/search/filter/status codes, invalid required/directory/tag/date/size rules, tag replacement, missing IDs, ID mismatch, and change-log delete conflict |
| `FileChangeLog` DTOs | Implemented | `Dtos/FileChangeLogDto.cs` | DTO exposes audit fields plus a non-sensitive file summary; no create/update DTO exists |
| `FileChangeLog` API controller | Implemented | `Controllers/Api/FileChangeLogsApiController.cs` | Read-only API only, route `api/file-change-logs`, with collection/query/changeType/fileId filters and details endpoint; no POST, PUT, or DELETE actions |
| `FileChangeLog` API integration tests | Implemented | `Labosi-ASP.NET.Tests/Api/FileChangeLogsApiTests.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Tests call real HTTP endpoints through `HttpClient` and verify read-only GET/filter/detail behavior plus unavailable POST/PUT/DELETE |
| `SystemAdmin` DTOs | Implemented | `Dtos/SystemAdminDto.cs`, `Dtos/CreateSystemAdminDto.cs`, `Dtos/UpdateSystemAdminDto.cs` | DTOs keep `SystemAdmin` as a NAS domain entity, expose non-sensitive admin metadata and managed NAS server references, and do not expose or accept `Password` |
| `SystemAdmin` API controller | Implemented | `Controllers/Api/SystemAdminsApiController.cs` | Uses `[ApiController]`, `ControllerBase`, route `api/system-admins`, manual mapping, query/nasServerId filters, safe placeholder password on API create, update that preserves existing password, managed server ID validation, relationship replacement, and existing delete guard for managed servers |
| `SystemAdmin` API integration tests | Implemented | `Labosi-ASP.NET.Tests/Api/SystemAdminsApiTests.cs`, `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Tests call real HTTP endpoints through `HttpClient` and verify CRUD/filter/status codes, managed server validation/replacement, delete conflict, password omission, raw password absence, submitted password ignored on create, and password preservation on update |
| Identity local account foundation | Implemented | `Models/AppUser.cs`, `Data/NasIndexerDbContext.cs`, `Program.cs`, `Areas/Identity/Pages/Account/Register.cshtml`, `Areas/Identity/Pages/Account/Register.cshtml.cs`, `Views/Shared/_LoginPartial.cshtml`, `Views/Shared/_Layout.cshtml`, `Migrations/20260610190429_AddIdentityAppUser.cs` | Uses ASP.NET Core Identity with separate `AppUser`, required OIB/JMBG fields, local registration/login/logout, Identity UI Razor Pages, and no role authorization or external login yet |
| Upload support | Not started | None | Deferred intentionally |
| Attachment/Auth integration tests | Not started | None | Auth role tests and upload tests are deferred intentionally |

Authorization should be applied consistently to MVC and API surfaces:

| Operation | Access rule |
|---|---|
| MVC `Index` and `Search`; API `GET` collection/search endpoints | Anonymous users allowed |
| MVC `Details`; API `GET {id}` endpoints | Authenticated users |
| MVC/API `Create` and `Edit`; API `POST` and `PUT` | `Admin` or `Manager` |
| MVC/API `Delete`; API `DELETE`; attachment removal | `Admin` only |
| `FileChangeLog` `POST`, `PUT`, `DELETE` | Not available because audit/history records are read-only |

## Identity/Auth Files Likely Added Or Changed Later

Current status: local ASP.NET Core Identity account foundation is implemented. The project now has an `AppUser`, Identity packages, `IdentityDbContext<AppUser>`, authentication middleware, default Identity UI login/logout pages, and a custom local registration page that captures OIB and JMBG.

Packages:

- Implemented: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- Implemented: `Microsoft.AspNetCore.Identity.UI`
- Planned later: `Microsoft.AspNetCore.Authentication.Google` or Facebook equivalent
- `Microsoft.VisualStudio.Web.CodeGeneration.Design` only if scaffolding Identity UI

Identity files:

| File/folder | Planned purpose |
|---|---|
| `Models/AppUser.cs` | Implemented `IdentityUser` subclass with required `OIB` and `JMBG` validation |
| `Data/NasIndexerDbContext.cs` | Implemented `IdentityDbContext<AppUser>` while preserving all NAS Indexer DbSets and relationships |
| `Program.cs` | Implemented local Identity registration plus `UseAuthentication()` before `UseAuthorization()` and `MapRazorPages()` |
| `Areas/Identity/Pages/Account/Register.cshtml` | Implemented local registration UI with Email, OIB, JMBG, Password, and Confirm Password |
| `Areas/Identity/Pages/Account/Register.cshtml.cs` | Implemented OIB/JMBG validation and saves those fields on `AppUser` |
| `Views/Shared/_LoginPartial.cshtml` | Implemented login/register links for anonymous users and manage/logout links for signed-in users |
| `Views/Shared/_Layout.cshtml` | Implemented login partial rendering from the side navigation |
| `Migrations/20260610190429_AddIdentityAppUser.cs` | Implemented Identity/AppUser schema migration |
| `Data/IdentitySeedData.cs` or similar | Planned later only if role seeding is requested |
| `Areas/Identity/Pages/Account/ExternalLogin.cshtml` | Planned later with Google/Facebook login support |
| `Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs` | Planned later with Google/Facebook login support |
| `appsettings.json` / user secrets | Planned later for external provider credentials; no real secrets committed |

Important identity decision:

- Existing `SystemAdmin` remains a NAS domain entity with NAS responsibility/contact metadata.
- `SystemAdmin` API should provide password-safe CRUD where business rules allow.
- `SystemAdmin.Password` must not be used for login.
- `SystemAdmin.Password` must not be exposed by response DTOs or accepted by create/update DTOs.
- ASP.NET Core Identity authentication must use `AppUser` separately.
- If the domain ever needs to connect a NAS `SystemAdmin` record to an authenticated account, add an explicit optional link to `AppUser` in a later scoped design instead of treating `SystemAdmin` as the auth user.
- `dotnet ef database update` was not run during this Identity foundation step. Review migration `20260610190429_AddIdentityAppUser` first, then run `dotnet ef database update` when ready to apply it to the development SQLite database.

## Integration Test Project/Files Likely Added Later

Current status: the integration test foundation exists and covers the implemented `FileTag`, `NasServer`, `ScanJob`, `DirectoryItem`, `FileItem`, read-only `FileChangeLog`, and `SystemAdmin` API slices. Auth and upload tests remain planned later.

| File/folder | Purpose |
|---|---|
| `Labosi-ASP.NET.Tests/Labosi-ASP.NET.Tests.csproj` | Implemented xUnit integration test project |
| `Labosi-ASP.NET.Tests/CustomWebApplicationFactory.cs` | Implemented `WebApplicationFactory` infrastructure using `FileTagsApiController` as the public entry assembly marker and replacing the development DB with SQLite in-memory |
| `Labosi-ASP.NET.Tests/TestDataFactory.cs` | Implemented helper for isolated FileTag, NasServer, ScanJob, DirectoryItem, FileItem, FileChangeLog, and SystemAdmin test data, including relationship data for delete guards and filters |
| `Labosi-ASP.NET.Tests/Api/FileTagsApiTests.cs` | Implemented FileTag API coverage for list/search/get/create/update/delete, invalid input, missing IDs, ID mismatch, and assigned-tag delete conflict |
| `Labosi-ASP.NET.Tests/Api/NasServersApiTests.cs` | Implemented NasServer API coverage for list/search/get/create/update/delete, invalid input, missing IDs, ID mismatch, dependent delete conflict, and password omission/preservation |
| `Labosi-ASP.NET.Tests/Api/ScanJobsApiTests.cs` | Implemented ScanJob API coverage for list/search/status/NAS server filters, get/create/update/delete, invalid NAS server, time/progress validation, missing IDs, ID mismatch, and scanned-directory delete conflict |
| `Labosi-ASP.NET.Tests/Api/DirectoriesApiTests.cs` | Implemented DirectoryItem API coverage for list/search/scan job/parent filters, get/create/update/delete, invalid required fields, missing scan job/parent IDs, invalid dates, self-parent, descendant cycle, missing IDs, ID mismatch, and child/file delete conflicts |
| `Labosi-ASP.NET.Tests/Api/FileItemsApiTests.cs` | Implemented FileItem API coverage for list/search/directory/tag/extension filters, get/create/update/delete, invalid required fields, missing directory ID, invalid tag IDs, negative size, invalid dates, missing IDs, ID mismatch, tag replacement, and change-log delete conflict |
| `Labosi-ASP.NET.Tests/Api/FileChangeLogsApiTests.cs` | Implemented read-only FileChangeLog API coverage for list/query/changeType/fileId filters, details, missing ID, and unavailable POST/PUT/DELETE |
| `Labosi-ASP.NET.Tests/Api/SystemAdminsApiTests.cs` | Implemented SystemAdmin API coverage for list/query/nasServerId filters, get/create/update/delete, invalid input, invalid managed server IDs, ID mismatch, missing IDs, password omission/raw-secret absence, ignored submitted password, password preservation, managed server replacement, and managed-server delete conflict |
| `Labosi-ASP.NET.Tests/TestAuthHandler.cs` | Planned later for protected API tests |
| `Labosi-ASP.NET.Tests/Api/FileAttachmentsApiTests.cs` | Planned later |

Likely test packages:

- `Microsoft.AspNetCore.Mvc.Testing`
- `Microsoft.EntityFrameworkCore.InMemory` or SQLite in-memory for relational behavior
- `xunit`
- `xunit.runner.visualstudio`
- `FluentAssertions` optional

Preferred database for tests:

- Use SQLite in-memory if relationship/delete behavior matters.
- EF InMemory is acceptable for simple endpoint behavior but does not enforce all relational constraints.

Minimum test coverage per API controller:

- `GET all/search` success
- `GET by id` success
- `GET by id` missing returns `404`
- `POST` success returns `201 Created`
- `POST` invalid model returns `400`
- `PUT` success
- `PUT` ID mismatch/missing returns `400`/`404`
- `DELETE` success when allowed
- `DELETE` missing returns `404`
- `DELETE` blocked by business rules returns `409 Conflict` or `422 Unprocessable Entity`
- protected writes return `401`/`403` for unauthorized users

## Staged Implementation Plan

### Checkpoint 1 - API Foundation

- Status: implemented for the first `FileTag` vertical slice.
- Added DTO folder and `FileTag` DTOs.
- Added first API controller for `FileTag` because it is simple and low-risk.
- Added consistent status codes: `200`, `201`, `204`, `400`, `404`, `409`.
- Build passed; manual HTTP smoke requests are listed in `lab-1/agent_log.txt`.

### Checkpoint 2 - Core Metadata API CRUD

- Status: implemented for `NasServer`, `ScanJob`, `DirectoryItem`, and `FileItem`.
- Added `NasServersApiController` and NasServer DTOs.
- Added `ScanJobsApiController` and ScanJob DTOs.
- Added `DirectoriesApiController` and DirectoryItem DTOs.
- Added `FileItemsApiController` and FileItem DTOs.
- Preserved existing Lab 4 NasServer, ScanJob, DirectoryItem, and FileItem business rules in API validation and delete guards.
- Verified `NasServer.Password` is not exposed or accepted by DTOs; tests assert JSON does not contain a password property or raw password value.
- Build and integration tests pass for implemented API slices.

### Checkpoint 3 - Audit And Admin API

- Status: implemented for read-only `FileChangeLog` and password-safe NAS domain `SystemAdmin`.
- Added `FileChangeLogsApiController` with read-only GET/filter/details endpoints; integration tests prove write endpoints are absent or method-not-allowed.
- Added `SystemAdminsApiController` with password-safe NAS domain DTOs, `query`/`nasServerId` filters, managed NAS server validation/replacement, existing managed-server delete guard, API create placeholder password, and update behavior that preserves existing legacy password.
- Integration tests prove SystemAdmin JSON responses do not expose a password property or raw password values, and submitted password fields are ignored.

### Checkpoint 4 - Attachment Upload

- Add `FileAttachment` model and DbSet.
- Add migration for attachments only.
- Add disk storage service or scoped helper.
- Add `FileAttachmentsApiController` and MVC attachment UI on `FileItems/Edit` or `FileItems/Details`.
- Use Dropzone or maintained equivalent for async upload.
- Add AJAX attachment list and delete behavior.

### Checkpoint 5 - Local Identity And Roles

- Status: local Identity account foundation implemented; roles/authorization remain deferred.
- Added `AppUser : IdentityUser` with required OIB and JMBG validation.
- Integrated Identity into `NasIndexerDbContext` through `IdentityDbContext<AppUser>`.
- Added local register/login/logout support through Identity UI and a custom registration page for OIB/JMBG.
- Added `_LoginPartial`, rendered it from the shared layout, and configured `UseAuthentication()` before `UseAuthorization()`.
- Generated migration `20260610190429_AddIdentityAppUser`; `dotnet ef database update` was not run.
- Later step: register roles `Admin` and `Manager`, seed roles if needed, and apply `[Authorize]`, `[AllowAnonymous]`, and role requirements to MVC and API writes.

### Checkpoint 6 - Third-Party Login

- Add Google or Facebook authentication package.
- Configure through user secrets/environment variables only.
- Verify external login registration captures `OIB` and `JMBG`.

### Checkpoint 7 - Integration Tests

- Status: API integration foundation implemented for `FileTag`, `NasServer`, `ScanJob`, `DirectoryItem`, `FileItem`, read-only `FileChangeLog`, and `SystemAdmin`.
- Added test project and WebApplicationFactory-based factory.
- Proved implemented vertical endpoints end-to-end through real `HttpClient` calls and SQLite in-memory data isolation.
- Next later step: replicate the pattern across remaining API controllers only after those APIs exist.
- Auth role coverage and attachment upload/list/delete tests remain deferred.

### Checkpoint 8 - Final Audit

- Run `dotnet build`.
- Run `dotnet test`.
- Run grep checks for password exposure and forbidden FileChangeLog write endpoints.
- Run smoke checks for representative API routes.
- Update this checklist and `lab-1/agent_log.txt`.

## Audit Matrix

| Requirement | Planned implementation | Files likely affected | Verification method | Risk |
|---|---|---|---|---|
| Complete API support for all entities where business rules allow CRUD | Add API controllers and DTOs for NAS Indexer entities, with full CRUD for `NasServer`, `ScanJob`, `DirectoryItem`, `FileItem`, `FileTag`, `SystemAdmin`; read-only API for `FileChangeLog` | `Controllers/*ApiController.cs`, `Dtos/*`, `Repositories/*` or direct DbContext queries | HTTP smoke tests; integration tests per CRUD method; grep for `[ApiController]` routes | Medium: must preserve delete guards and avoid direct entity serialization |
| `GET` all with search/query options | Add `GET api/...` endpoints with `query` and entity-specific filters | API controllers, repository query methods/DTO mapping | Tests for empty and filtered results | Low/medium: search behavior can diverge from MVC search |
| `GET` one by ID | Add `GET api/.../{id}` endpoints | API controllers, DTOs | Tests for success and `404` | Low |
| `POST` create | Add create DTOs and map to entities with validation | API controllers, create DTOs, repositories | Tests for `201 Created`, invalid `400` | Medium: relationship IDs and date rules need explicit validation |
| `PUT` update | Add update DTOs and manual allowed-field mapping | API controllers, update DTOs, repositories | Tests for success, missing ID, ID mismatch | Medium: overposting and sensitive fields |
| `DELETE` delete | Add delete endpoints with existing business-rule blocks | API controllers, repositories | Tests for success, `404`, blocked `409`/`422` | Medium: EF cascade rules must not bypass intended blocks |
| API must not expose internal fields | DTOs omit `NasServer.Password`, `SystemAdmin.Password`, EF navigation cycles, unnecessary internals | DTOs, API controllers, tests | JSON assertions; grep for password fields in DTOs/API responses | High: current models contain plain password strings |
| Nested DTOs for related data | Use summary DTOs for server, scan job, directory, file, tags, admins | DTOs and mappers | JSON shape tests | Medium: avoid deep recursive graphs |
| Upload tied to concrete entity | Translate quiz upload to `FileItem` attachments | `Models/FileAttachment.cs`, `Data/NasIndexerDbContext.cs`, migration, `FileAttachmentsApiController`, `Views/FileItems/*`, JS/CSS | Upload/list/delete integration tests and browser check | Medium/high: storage path safety and auth |
| Async Dropzone or maintained alternative | Add Dropzone/alternative on `FileItems/Edit` or `Details` | `wwwroot/lib` or package/static assets, `Views/FileItems/Edit.cshtml`, JS | Browser upload test; network tab shows async multipart call | Medium: package/static asset management |
| Store uploaded files on disk | Save under controlled uploads path with generated stored filename | Storage service/helper, attachment API | File exists after upload; path traversal tests | High: path sanitization and cleanup |
| Store metadata/path in DB | Add `FileAttachment` table with file name, path, content type, size, created at, uploader | model, DbContext, migration | Migration review; DB assertion in tests | Medium |
| AJAX load attachment list | Add list endpoint and render partial/JSON list | `FileAttachmentsApiController`, `Views/FileItems/_AttachmentList.cshtml`, JS | Browser/AJAX test and integration GET test | Low |
| Delete existing uploaded files | Add authorized delete endpoint that removes DB row and disk file safely | attachment API/storage helper | Integration test and disk assertion | Medium: orphaned files if DB/delete fails |
| Local auth through ASP.NET Core Identity | Add `AppUser`, Identity packages/config, register/login UI | csproj, `Models/AppUser.cs`, `Data/NasIndexerDbContext.cs`, `Program.cs`, `Areas/Identity/*` | Register/login manual tests; auth integration tests | High: changes DbContext/migrations and app pipeline |
| Extend `AppUser` fields | Add `OIB` and `JMBG` with required fixed-length numeric validation | `Models/AppUser.cs`, Identity pages, migration | Validation tests and registration manual test | Medium |
| Authorization rules | Anonymous list/search; authenticated details; Admin or Manager create/edit; Admin-only delete; no FileChangeLog writes | `Program.cs`, controllers/API controllers, role seed | `401`/`403` tests per role and endpoint type | High: consistent MVC and API protection |
| Third-party Google/Facebook login | Add one external provider configured through user secrets/env vars | csproj, `Program.cs`, Identity external login pages, config docs | Manual external login; no secrets grep | Medium/high: provider setup outside repo |
| Integration tests for all API CRUD endpoints | Add test project with `WebApplicationFactory`, test DB, fake auth | `Labosi-ASP.NET.Tests/*`, solution file | `dotnet test` | High: test setup must isolate DB and auth |
| Test successful scenarios | Per-controller success tests for list/get/create/update/delete | test files | `dotnet test` | Medium |
| Test nonexistent IDs | Per-controller `404` tests | test files | `dotnet test` | Low |
| Test validation errors | Invalid DTO tests for required fields, dates, relation IDs, ranges, color, delete blocks | test files | `dotnet test` | Medium |
| Preserve NAS Indexer domain | Do not add quiz teaching domain artifacts | Entire repo | `rg "Quiz|Category|Question"` after implementation except docs if needed | Low |
| Keep `FileChangeLog` read-only | API exposes only `GET`/search/details or forbids writes | `FileChangeLogsApiController`, tests | grep for write endpoints; tests for no write access | Medium: conflicts with literal "all CRUD" wording |
| Preserve current MVC behavior | New API/Auth/Upload should not break Lab 4 MVC pages | `Program.cs`, controllers, views, JS | Existing route smoke checks plus build/test | Medium |

## Current Risks And Open Decisions

| Risk/decision | Recommendation |
|---|---|
| Identity integration could disturb the existing SQLite schema and migrations | Add Identity in a dedicated checkpoint; review generated migration before database update |
| Existing `SystemAdmin.Password` is not Identity-compatible | Do not use it for login; omit from DTOs; plan an explicit relationship to `AppUser` if needed |
| `FileChangeLog` read-only exception may need explanation during grading | Keep clear docs and tests proving the audit business rule |
| EF InMemory does not enforce relational behavior | Prefer SQLite in-memory tests for delete-block and FK scenarios |
| Uploaded file paths can be unsafe | Generate stored names, validate extensions/content size if required, never trust client file names for paths |
| External login requires provider credentials | Use user secrets/environment variables; never commit real secrets |

## Planning Verification History

- Inspected repository files with `rg --files`.
- Inspected `Lab5.md` from `C:\Users\Domagoj\Downloads`.
- Inspected current `DbContext`, models, controllers, view models, repository interface, business rules, layout, JavaScript/AJAX markers, migrations, project file, and auth/upload-related references.
- Confirmed current branch is `lab-5`.
- Initial planning confirmed the app had no Identity setup and no attachment/upload model at that time.
- Current implementation now includes local ASP.NET Core Identity/AppUser support. Upload support is still not implemented.
- The original planning step changed only `docs/lab5-checklist.md` and `lab-1/agent_log.txt`; later implementation steps changed the files listed in the implemented status sections above.
