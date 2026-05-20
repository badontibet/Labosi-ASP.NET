# Labos 4 Audit Checklist

Audit timestamp: 2026-05-20 18:35:21 +02:00

Scope: pre-implementation audit only. No Labos 4 application code has been implemented in this step.

## Current State Summary

- Branch: `main`
- Git status before this checklist: `## main...origin/main`, with existing modified `lab-1/agent_log.txt`
- Build: `dotnet build` succeeded with 0 warnings and 0 errors
- Existing MVC surface: read-only `Index` and `Details` actions/views for most main entities
- Missing Labos 4 features: Create/Edit/Delete POST flows, AJAX search, autocomplete dropdown, blur validation, server-side POST validation, advanced app JS, reusable DateTime partial, hr/en date parsing/formatting support
- Browser datepicker restriction: no current `input type="date"` or `input type="datetime-local"` usage found in views
- Business rules status: planned in `docs/lab4-business-rules.md`
- Etapa 1 implementation status: `ScanJob` gold example implemented with CRUD, AJAX search, NasServer autocomplete, text DateTime partial, server-side POST validation, blur client validation, and safe delete blocking.

## Entity Checklist

| Entity | Existing controller | Existing views | CRUD status | AJAX search status | Validation status | Dropdown/autocomplete need | DateTime fields | Risk |
|---|---|---|---|---|---|---|---|---|
| `NasServer` | `NasServersController` | `Views/NasServers/Index.cshtml`, `Details.cshtml` | Allowed/Planned full CRUD; Needs implementation; Delete Restricted when related data exists | Planned; Needs implementation | Planned; Needs implementation for every POST; protect credential-like fields | Planned autocomplete/multi-select for managed admins if relationship editing is included | `LastScan` | Restricted: deleting server could cascade scan jobs; password must not be exposed; port/IP validation needed |
| `ScanJob` | `ScanJobsController` | `Views/ScanJobs/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_ScanJobForm.cshtml`, `_ScanJobRows.cshtml` | Allowed/Implemented full CRUD as Labos 4 gold example; Delete Restricted when directories exist | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur client validation for NAS server, DateTime, counts, and path | Implemented `NasServer` autocomplete and `ScanStatus` dropdown | `StartTime`, nullable `EndTime` via shared text DateTime partial | Restricted: delete remains blocked when scanned directories exist; continue using this as pattern for later entities |
| `DirectoryItem` | `DirectoriesController` | `Views/Directories/Index.cshtml`, `Details.cshtml` | Allowed/Planned full CRUD; Needs implementation; Delete Restricted when child directories/files exist | Planned; Needs implementation | Planned; Needs implementation for path/name/parent and cycle prevention | Planned optional `ScanJob` and parent directory autocomplete/dropdown | `CreatedDate`, `ModifiedDate` | Restricted: self-reference cycles; DbContext restricts parent delete and files cascade from directory |
| `FileItem` | `FileItemsController` | `Views/FileItems/Index.cshtml`, `Details.cshtml` | Allowed/Planned metadata CRUD; Needs implementation; Delete Restricted when change logs exist | Planned; Needs implementation | Planned; Needs implementation for size/path/extension/date/directory rules | Planned required `DirectoryItem` autocomplete and tag multi-select/autocomplete | `CreatedDate`, `ModifiedDate` | Restricted: required directory; many-to-many tags; change logs must not be silently deleted |
| `FileTag` | `TagsController` | `Views/Tags/Index.cshtml`, `Details.cshtml` | Allowed/Planned full CRUD; Needs implementation; Delete Restricted when connected to files | Planned; Needs implementation | Planned; Needs implementation for name/description/color | Planned optional use in `FileItem` tag assignment | None | Restricted: many-to-many with files; color format validation needed |
| `FileChangeLog` | Missing dedicated controller | Missing dedicated views | Read-only Planned: Index/Details/AJAX search only; Create/Edit/Delete forbidden | Planned; Needs implementation for read-only search | Planned for search/filter inputs only; no POST CRUD validation | Planned read-only filter/autocomplete by `FileItem`; `ChangeType` filter dropdown | `Timestamp` | Read-only: audit/history record; normal CRUD would weaken audit trail |
| `SystemAdmin` | `AdminsController` | `Views/Admins/Index.cshtml`, `Details.cshtml` | Restricted/Planned CRUD; Needs implementation; no raw password workflow; Delete Restricted when assigned to servers | Planned; Needs implementation | Planned; Needs implementation for username/email/role/date/server rules; protect password | Planned managed `NasServer` multi-select/autocomplete and role dropdown | `CreatedDate`, `LastLogin` | Restricted: credential-like field and many-to-many server responsibility relationship |

## Implementation Order Proposal

1. Planned/complete for this document step: define Labos 4 business rules per entity in `docs/lab4-business-rules.md`.
2. Implemented for `ScanJob`; still Planned/Needs implementation for other allowed entities: repository write/search/autocomplete APIs and view models for form input, preserving existing read-only methods and routes.
3. Implemented for `ScanJob`; still Planned/Needs implementation for other allowed entities: MVC Create/Edit/Delete GET/POST endpoints with anti-forgery and server-side `ModelState` validation.
4. Implemented initial shared infrastructure: DateTime partial uses text inputs only, supports documented hr/en/server-safe formats, and reusable validation helpers exist in `wwwroot/js/lab4.js`.
5. Implemented for `ScanJob` and reusable components: AJAX list search and reusable autocomplete dropdown JavaScript/CSS.
6. Build verification complete for `ScanJob` slice; still Planned/Needs implementation: EF/database smoke checks and browser walkthrough for later entity slices.

## ScanJob Manual Test Checklist

1. Open `/scan-jobs` and type `NAS`, `/share`, `Running`, or `Failed` in the search box. Results should update without a full page reload and rows should highlight.
2. Open `/ScanJobs/Create`, type at least two characters in NAS Server, choose a server from the dropdown, enter `2026-05-20 14:30` or `20.05.2026 14:30`, choose a status, and submit.
3. Open `/ScanJobs/Edit/{id}` for an existing job and verify Start/End time text fields accept `dd.MM.yyyy HH:mm`, `MM/dd/yyyy HH:mm`, or `yyyy-MM-dd HH:mm`.
4. Trigger blur validation by leaving NAS Server blank, entering an invalid DateTime, entering negative counts, or making Processed Files greater than Total Files.
5. Open `/ScanJobs/Delete/{id}`. Seeded jobs with scanned directories should show a blocked-delete message. A newly created job with no directories can be deleted after confirmation.
6. Confirm no native browser datepicker appears; DateTime fields are plain text inputs.

## ScanJob Strict Review PASS/FAIL

Review timestamp: 2026-05-20 19:17:58 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| `NasServerId` required server-side | PASS | `ScanJobFormViewModel.NasServerId` has `[Required]`; controller verifies selected server exists. |
| `StartTimeText` required and parseable server-side | PASS | `[Required]`, `IValidatableObject`, and `ScanJobsController.TryBuildScanJob` parse check all guard before mapping. |
| `EndTimeText` optional but parseable if present | PASS | `DateTimeInputParser.TryParseOptional` is used by ViewModel and controller. |
| `EndTime >= StartTime` | PASS | ViewModel validates server-side; `lab4.js` also checks cross-field DateTime order on blur/submit. |
| `TotalFiles >= 0` and `ProcessedFiles >= 0` | PASS | `[Range]` server-side and blur validation for nonnegative values. |
| `ProcessedFiles <= TotalFiles` | PASS | ViewModel server-side validation and blur/submit client validation. |
| `RootPath` required/bounded | PASS | `[Required]` and `[StringLength(180)]`; text input uses required blur validation. |
| Controller does not ignore DateTime parse failure | PASS | `TryBuildScanJob` explicitly checks parser results and adds `ModelState` errors before creating `ScanJob`. |
| Edit update avoids unnecessary navigation/internal overwrite | PASS | `EfNasRepository.UpdateScanJob` loads tracked row and updates scalar/FK metadata only. |
| Invalid POST redisplays autocomplete label and DateTime text | PASS | Create/Edit return the submitted `ScanJobFormViewModel`; partials render `NasServerLabel`, `StartTimeText`, and `EndTimeText`. |
| DateTime partial avoids native browser picker | PASS | `_DateTimePicker.cshtml` uses `type="text"`; grep found no `type="date"` or `type="datetime-local"`. |
| Autocomplete hidden ID behavior | PASS | `_AutocompleteDropdown.cshtml` has hidden ID plus visible label; `lab4.js` clears hidden ID immediately when label changes after selection. |
| AJAX search without full page reload | PASS | Index search uses `fetch` to `ScanJobs/Search` and replaces `_ScanJobRows` target. |
| Search rows include Details/Edit/Delete | PASS | `_ScanJobRows.cshtml` includes Open, Edit, and Delete links. |
| Delete POST anti-forgery and block rule | PASS | Delete POST has `[ValidateAntiForgeryToken]`, checks `ScanJobHasDirectories`, and handles repository delete failure. |
| FileChangeLog has no Create/Edit/Delete UI | PASS | grep found no FileChangeLog Create/Edit/Delete additions. |
| New ScanJob views do not expose raw passwords | PASS | grep found no `Password`, `Username`, or credential references in new ScanJob form/shared partials. |
| `_ValidationScriptsPartial` does not duplicate jQuery | PASS | Partial is a comment only; vanilla `lab4.js` is loaded once from `_Layout.cshtml`. |
| `lab4.js` robust when elements are absent | PASS | Setup functions query optional data elements and return when targets/endpoints are missing. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors. |
