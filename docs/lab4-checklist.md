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
- Etapa 2 implementation status: `FileTag` CRUD implemented with AJAX search, form view model, server-side POST validation, blur client validation, hex color validation, and delete blocking when connected files exist.
- Etapa 3 implementation status: `DirectoryItem` CRUD implemented with AJAX search, form view model, optional ScanJob and parent directory autocomplete, text DateTime partials, server-side hierarchy validation, and delete blocking when child directories/files exist.
- Etapa 4 implementation status: `FileItem` metadata CRUD implemented with AJAX search, form view model, required DirectoryItem autocomplete, checkbox FileTag selection, text DateTime partials, server-side validation, and delete blocking when change logs exist.
- Etapa 5 implementation status: `FileChangeLog` read-only Index/Details/AJAX search implemented with ChangeType filter and no Create/Edit/Delete UI or POST CRUD endpoints.
- Etapa 6 implementation status: `NasServer` restricted CRUD implemented with AJAX search, form view model, text LastScan partial, protected stored secret handling, and delete blocking when scan jobs or managed admins exist.
- Etapa 7 implementation status: `SystemAdmin` restricted CRUD implemented with AJAX search, form view model, text CreatedDate/LastLogin partials, checkbox ManagedServers selection, protected stored secret handling, and delete blocking when managed servers exist.

## Entity Checklist

| Entity | Existing controller | Existing views | CRUD status | AJAX search status | Validation status | Dropdown/autocomplete need | DateTime fields | Risk |
|---|---|---|---|---|---|---|---|---|
| `NasServer` | `NasServersController` | `Views/NasServers/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_NasServerForm.cshtml`, `_NasServerRows.cshtml` | Restricted/Implemented CRUD; Delete Restricted when scan jobs or managed admins exist | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur client validation for name, IP address, port, username length, and LastScan text | Managed admin assignment remains out of scope; scan job selection is handled by ScanJob CRUD | `LastScan` via shared text DateTime partial | Restricted: stored secret field is not shown or edited; delete remains blocked when relationships exist |
| `ScanJob` | `ScanJobsController` | `Views/ScanJobs/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_ScanJobForm.cshtml`, `_ScanJobRows.cshtml` | Allowed/Implemented full CRUD as Labos 4 gold example; Delete Restricted when directories exist | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur client validation for NAS server, DateTime, counts, and path | Implemented `NasServer` autocomplete and `ScanStatus` dropdown | `StartTime`, nullable `EndTime` via shared text DateTime partial | Restricted: delete remains blocked when scanned directories exist; continue using this as pattern for later entities |
| `DirectoryItem` | `DirectoriesController` | `Views/Directories/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_DirectoryForm.cshtml`, `_DirectoryRows.cshtml` | Allowed/Implemented full CRUD; Delete Restricted when child directories/files exist | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur validation for name/path/date fields; server-side self-reference and cycle checks | Implemented optional `ScanJob` and parent directory autocomplete/dropdown | `CreatedDate`, `ModifiedDate` via shared text DateTime partial | Restricted: self-reference/cycle validation enforced; delete remains blocked when child directories or files exist |
| `FileItem` | `FileItemsController` | `Views/FileItems/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_FileForm.cshtml`, `_FileRows.cshtml` | Allowed/Implemented metadata CRUD; Delete Restricted when change logs exist | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur validation for name/path/size/date/directory fields; tag IDs validated server-side | Implemented required `DirectoryItem` autocomplete and checkbox FileTag selection | `CreatedDate`, `ModifiedDate` via shared text DateTime partial | Restricted: change logs remain read-only and block delete |
| `FileTag` | `TagsController` | `Views/Tags/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_TagForm.cshtml`, `_TagRows.cshtml` | Allowed/Implemented full CRUD; Delete Restricted when connected to files | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur client validation for name, description length, and hex color | Planned optional use in future `FileItem` tag assignment | None | Restricted: many-to-many with files; delete remains blocked when any files use the tag |
| `FileChangeLog` | `FileChangeLogsController` | `Views/FileChangeLogs/Index.cshtml`, `Details.cshtml`, `_FileChangeLogRows.cshtml` | Read-only Implemented: Index/Details/AJAX search only; Create/Edit/Delete forbidden | Implemented with debounce, partial row refresh, and ChangeType dropdown filter | Implemented for search/filter inputs only; no POST CRUD validation because no writes exist | Implemented `ChangeType` dropdown filter; FileItem filter handled through text search by file name/path | `Timestamp` display-only | Read-only: audit/history record; normal CRUD would weaken audit trail |
| `SystemAdmin` | `AdminsController` | `Views/Admins/Index.cshtml`, `Details.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`, `_AdminForm.cshtml`, `_AdminRows.cshtml` | Restricted/Implemented CRUD; Delete Restricted when managed servers exist; no raw password workflow | Implemented with debounce and partial row refresh | Implemented server-side POST validation and blur client validation for username, email, role, CreatedDate, LastLogin, and selected server IDs | Implemented checkbox ManagedServers selection | `CreatedDate`, `LastLogin` via shared text DateTime partial | Restricted: credential-like field is not shown or edited; many-to-many server responsibility relationship blocks delete |

## Implementation Order Proposal

1. Planned/complete for this document step: define Labos 4 business rules per entity in `docs/lab4-business-rules.md`.
2. Implemented for `ScanJob`, `FileTag`, `DirectoryItem`, and `FileItem`; still Planned/Needs implementation for other allowed entities: repository write/search/autocomplete APIs and view models for form input, preserving existing read-only methods and routes.
3. Implemented for `ScanJob`, `FileTag`, `DirectoryItem`, and `FileItem`; still Planned/Needs implementation for other allowed entities: MVC Create/Edit/Delete GET/POST endpoints with anti-forgery and server-side `ModelState` validation.
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

## FileTag Manual Test Checklist

1. Open `/tags` and type `Code`, `Documentation`, `Audit`, `#2563eb`, or part of a description in the search box. Results should update without a full page reload and rows should highlight.
2. Open `/Tags/Create`, submit an empty name, and confirm server/client validation shows a required-name error.
3. Enter a long name over 60 characters or description over 220 characters and confirm validation blocks it.
4. Enter invalid color values such as `red`, `#123`, or `123456`; blur/submit should show a hex color error. `#AABBCC` should pass.
5. Open `/Tags/Edit/{id}` and verify existing Name, Description, and Color are redisplayed after invalid POST.
6. Open `/Tags/Delete/{seeded-id}` for a tag assigned to files and confirm delete is blocked with a clear message.
7. Create a new tag with no files, open its Delete page, and confirm POST delete works after browser confirmation.

## DirectoryItem Manual Test Checklist

1. Open `/directories` and search by directory name, path, parent name/path, or scan job root path. Results should update without a full page reload and rows should highlight.
2. Open `/Directories/Create`, submit empty Name/Path, and confirm validation errors appear.
3. Test Created/Modified date formats: `20.05.2026 14:30`, `05/20/2026 14:30`, and `2026-05-20 14:30`; invalid dates should fail.
4. Use Scan Job autocomplete with at least two characters from a server/root path and choose a result.
5. Use Parent Directory autocomplete with at least two characters from a directory name/path and choose a result.
6. Open `/Directories/Edit/{id}` and verify existing parent/scan job labels and date text are shown.
7. Try setting a directory as its own parent or a descendant as parent; server-side validation should block it.
8. Open `/Directories/Delete/{seeded-id}` for a directory with files or child directories and confirm delete is blocked.
9. Create a new directory with no child directories/files and confirm Delete POST works after confirmation.

## FileItem Manual Test Checklist

1. Open `/files` and search by file name, path, extension, directory name/path, or tag name. Results should update without a full page reload and rows should highlight.
2. Open `/FileItems/Create`, submit empty Name/Path/Directory, and confirm validation errors appear.
3. Test Created/Modified date formats: `20.05.2026 14:30`, `05/20/2026 14:30`, and `2026-05-20 14:30`; invalid dates should fail.
4. Enter a negative Size and confirm validation blocks it.
5. Use Directory autocomplete with at least two characters from a directory name/path and choose a result.
6. Select one or more FileTag checkboxes and save; Details should show selected tags.
7. Open `/FileItems/Edit/{id}` and verify existing directory label, dates, and tag checkboxes are shown.
8. Open `/FileItems/Delete/{seeded-id}` for a file with change logs and confirm delete is blocked.
9. Create a new file without change logs and confirm Delete POST works after confirmation.

## FileChangeLog Manual Test Checklist

1. Open `/FileChangeLogs` and confirm the page states that records are audit/read-only entries.
2. Search by file name, file path, directory, user, old value, new value, or change type. Results should update without a full page reload and rows should highlight.
3. Use the Change Type dropdown filter and confirm results update without a full page reload.
4. Open `/FileChangeLogs/Details/{id}` from an `Open` row link and confirm file, directory, timestamp, change type, user, old value, and new value are visible.
5. Confirm no Create, Edit, or Delete links/forms appear on FileChangeLog Index or Details.
6. Confirm no native browser datepicker appears.

## NasServer Manual Test Checklist

1. Open `/NasServers` and search by name, IP address, port, username, status, or scan root path; results should update without a full page reload and rows should highlight.
2. Open `/NasServers/Create`, submit empty Name/IP/LastScan or invalid Port/IP, and confirm validation errors appear.
3. Test LastScan date formats: `20.05.2026 14:30`, `05/20/2026 14:30`, and `2026-05-20 14:30`; invalid dates should fail.
4. Open `/NasServers/Edit/{id}` and confirm existing metadata is shown while stored secrets are not shown or editable.
5. Open `/NasServers/Delete/{seeded-id}` for a server with scan jobs or managed admins and confirm delete is blocked.
6. Create a new server with no scan jobs/admins and confirm Delete POST works after confirmation.
7. Confirm no native browser datepicker appears.

## NasServer Restricted CRUD PASS/FAIL

Review timestamp: 2026-05-20 23:45:00 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| NasServer Index AJAX search without full reload | PASS | `/NasServers/Search` returns `_NasServerRows`; Index uses `data-lab4-search`, loading indicator, partial tbody replacement, and row highlight. |
| Search by name/IP/port/status/path | PASS | Repository search checks name, IP address, parsed port, username, active/inactive status, and scan job root path. |
| Details preserved/improved | PASS | Details keeps server metadata, scan jobs, and directories, with Edit/Delete/Back actions. |
| Create GET/POST | PASS | `NasServersController.Create` GET/POST uses `NasServerFormViewModel` and anti-forgery. |
| Edit GET/POST | PASS | `NasServersController.Edit` GET/POST uses `NasServerFormViewModel`; repository updates editable scalar metadata only. |
| Delete GET/POST with confirmation | PASS | Delete view confirms deletion; POST uses `[ValidateAntiForgeryToken]`. |
| Delete blocked when related data exists | PASS | `NasServerHasScanJobsOrManagedAdmins` blocks delete when scan jobs or managed admins exist. |
| Name required/bounded | PASS | Form model uses `[Required]` and `[StringLength(120)]`; client uses required/max length blur validation. |
| IP address required/valid | PASS | Form model uses `IPAddress.TryParse`; client uses `ipaddress` blur validation. |
| Port range 1-65535 | PASS | Form model uses `[Range(1, 65535)]`; client uses `integer-range` blur validation. |
| Username optional/bounded | PASS | Form model uses `[StringLength(80)]`; client uses max length validation. |
| LastScan text DateTime validation | PASS | Shared `_DateTimePicker` is used; form model parses with `DateTimeInputParser`. |
| Stored secret field not exposed | PASS | Form model and views omit the stored secret field; repository edit preserves existing value. |
| No native datepicker | PASS | Verification found no `type="date"` or `type="datetime-local"`. |
| FileChangeLog read-only preserved | PASS | Verification found no FileChangeLog Create/Edit/Delete additions in FileChangeLog controller/views. |
| No mass unrelated CRUD | PASS | Changes are scoped to NasServer plus reusable JS validation support. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors. |

## SystemAdmin Manual Test Checklist

1. Open `/Admins` and search by username, email, role, or managed server name; results should update without a full page reload and rows should highlight.
2. Open `/Admins/Create`, submit empty Username/Email/Role/CreatedDate/LastLogin or invalid Email, and confirm validation errors appear.
3. Test CreatedDate/LastLogin date formats: `20.05.2026 14:30`, `05/20/2026 14:30`, and `2026-05-20 14:30`; invalid dates should fail.
4. Set LastLogin before CreatedDate and confirm server/client validation blocks submit.
5. Select one or more Managed NAS Server checkboxes and save; Details should show selected servers.
6. Open `/Admins/Edit/{id}` and confirm existing metadata, date text, and selected server checkboxes are shown.
7. Open `/Admins/Delete/{seeded-id}` for an admin with managed servers and confirm delete is blocked.
8. Create a new admin with no managed servers and confirm Delete POST works after confirmation.
9. Confirm no native browser datepicker appears.

## SystemAdmin Restricted CRUD PASS/FAIL

Review timestamp: 2026-05-21 00:15:00 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| SystemAdmin Index AJAX search without full reload | PASS | `/Admins/Search` returns `_AdminRows`; Index uses `data-lab4-search`, loading indicator, partial tbody replacement, and row highlight. |
| Search by username/email/role/server | PASS | Repository search checks username, email, role, and managed server name. |
| Details preserved/improved | PASS | Details keeps profile, managed servers, and managed-server scan jobs, with Edit/Delete/Back actions. |
| Create GET/POST | PASS | `AdminsController.Create` GET/POST uses `SystemAdminFormViewModel` and anti-forgery. |
| Edit GET/POST | PASS | `AdminsController.Edit` GET/POST uses `SystemAdminFormViewModel`; repository updates safe metadata and server assignments. |
| Delete GET/POST with confirmation | PASS | Delete view confirms deletion; POST uses `[ValidateAntiForgeryToken]`. |
| Delete blocked when managed servers exist | PASS | `SystemAdminHasManagedServers` blocks delete when server assignments exist. |
| Username required/bounded | PASS | Form model uses `[Required]` and `[StringLength(80)]`; client uses required/max length blur validation. |
| Email required/valid/bounded | PASS | Form model uses `[Required]`, `[EmailAddress]`, and `[StringLength(160)]`; client uses email/max length blur validation. |
| Role required/bounded | PASS | Form model uses `[Required]` and `[StringLength(80)]`; client uses required/max length blur validation. |
| CreatedDate/LastLogin text DateTime validation | PASS | Shared `_DateTimePicker` is used for both fields; form model parses with `DateTimeInputParser`. |
| LastLogin >= CreatedDate | PASS | Form model and controller validate the range; `lab4.js` checks the same pair on blur/submit. |
| ManagedServers selection | PASS | Form uses checkbox list, Edit redisplays selected servers, and POST validates selected IDs exist. |
| Stored secret field not exposed | PASS | Form model and Admins views omit the stored secret field; repository edit preserves existing value. |
| No native datepicker | PASS | Verification found no `type="date"` or `type="datetime-local"`. |
| FileChangeLog read-only preserved | PASS | Verification found no FileChangeLog Create/Edit/Delete additions in FileChangeLog controller/views. |
| No mass unrelated CRUD | PASS | Changes are scoped to SystemAdmin/Admins plus reusable JS validation support. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors. |

## Final Labos 4 Defense Audit

Audit timestamp: 2026-05-21 00:23:35 +02:00

| PDF/Labos 4 requirement | Status | Evidence/notes |
|---|---|---|
| CRUD where business rules allow | PASS | Implemented for `ScanJob`, `DirectoryItem`, `FileItem`, `FileTag`, restricted `NasServer`, and restricted `SystemAdmin`. |
| FileChangeLog read-only audit entity | PASS | `FileChangeLogsController` has only `Index`, `Search`, and `Details`; grep found no FileChangeLog Create/Edit/Delete UI or POST CRUD. |
| AJAX search on list pages | PASS | All Labos 4 list pages use `data-lab4-search` and partial row endpoints. |
| Custom reusable AJAX autocomplete | PASS | `_AutocompleteDropdown.cshtml` plus `lab4.js` hidden-ID/visible-label AJAX behavior is used for ScanJob, DirectoryItem, and FileItem relationship selection. |
| Edit forms show relationship labels | PASS | ViewModels preserve `NasServerLabel`, `DirectoryLabel`, `ScanJobLabel`, and `ParentDirectoryLabel` for Edit and invalid POST redisplay. |
| Client-side blur validation | PASS | `lab4.js` attaches `blur` handlers to `data-lab4-validate` fields. |
| Server-side POST validation | PASS | All Create/Edit/Delete POST endpoints validate model state and/or delete business rules before saving. |
| Validation messages visible | PASS | Forms use validation summaries and `asp-validation-for`/`data-valmsg-for` targets styled by Labos 4 CSS. |
| Rich JS | PASS | `lab4.js` includes debounce, loading state, row highlight, autocomplete dropdown animation, stale hidden-ID clearing, and delete confirmation. |
| DateTime partial | PASS | `_DateTimePicker.cshtml` is the shared DateTime UI. |
| Date+time and hr/en support | PASS | `DateTimeInputParser` supports `dd.MM.yyyy HH:mm`, `MM/dd/yyyy HH:mm`, and `yyyy-MM-dd HH:mm`; JS mirrors those formats. |
| No native date inputs | PASS | grep found no `type="date"` or `type="datetime-local"`. |
| Password not exposed in restricted UIs | PASS | grep found no password/credential references in `Views/NasServers` or `Views/Admins`. |
| Delete blocked for related data | PASS | Delete blocks exist for scan directories, directory children/files, file change logs, tag file links, NAS scan jobs/admins, and admin managed servers. |
| Checklist updated | PASS | This checklist contains per-entity and final audit PASS/FAIL tables. |
| Agent log updated | PASS | `lab-1/agent_log.txt` contains chronological prompts, implementation summaries, commands, build results, and checkpoints. |
| Build and hygiene checks | PASS | `dotnet build` passed with 0 warnings/errors; `git diff --check` passed; HTTP smoke test returned 200 for key Labos 4 routes. |

Final audit result: PASS. No code fixes were required during the final defense audit; only documentation and log audit entries were added.

## FileChangeLog Read-only PASS/FAIL

Review timestamp: 2026-05-20 23:03:00 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| Dedicated FileChangeLogs controller | PASS | `FileChangeLogsController` has only `Index`, `Search`, and `Details` GET actions. |
| Index view | PASS | `Views/FileChangeLogs/Index.cshtml` lists audit records and states they are read-only. |
| Details view | PASS | `Views/FileChangeLogs/Details.cshtml` displays audit metadata and links back to file/audit list. |
| AJAX search endpoint | PASS | `FileChangeLogs/Search` returns `_FileChangeLogRows` partial. |
| Search without full page reload | PASS | Index uses `data-lab4-search`; `lab4.js` fetches rows and replaces tbody. |
| Search file name/path | PASS | Repository search checks `File.Name` and `File.Path`. |
| Search change type | PASS | Repository search matches `ChangeType`; Index also has a ChangeType dropdown filter. |
| Search user/actor | PASS | Repository search checks `User`. |
| Search old/new value | PASS | Repository search checks `OldValue` and `NewValue`. |
| ChangeType filter dropdown | PASS | Index has `changeType` select and `SearchFileChangeLogs` filters by enum. |
| FileItem autocomplete/filter | PASS | Stable file filtering is provided through text search by file name/path; no autocomplete added to avoid extra UI complexity. |
| No Create/Edit/Delete links | PASS | FileChangeLog rows expose only `Open`; Details exposes only `Open file` and `Back to audit log`. |
| No POST CRUD endpoints | PASS | Controller contains no `[HttpPost]`, Create, Edit, or Delete actions. |
| Reuse Labos 4 JS/CSS | PASS | Uses existing AJAX search, loading indicator, and row highlight from `lab4.js`/`lab4.css`. |
| Repository methods | PASS | Added `GetAllFileChangeLogs`, `GetFileChangeLogById`, and `SearchFileChangeLogs`. |
| No native date inputs | PASS | Verification found no `type="date"` or `type="datetime-local"`. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors. |

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

## FileItem Etapa 4 PASS/FAIL

Review timestamp: 2026-05-20 22:34:16 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| FileItem Index AJAX search without full reload | PASS | `/FileItems/Search` returns `_FileRows`; Index uses `data-lab4-search`, loading indicator, partial tbody replacement, and row highlight. |
| Search by file name/path/extension/directory/tag | PASS | `EfNasRepository.SearchFiles` filters `Name`, `Path`, `Extension`, directory name/path, and tag name. |
| Details preserved/improved | PASS | Details keeps metadata/tags/change-log display and adds Edit/Delete/Back actions. |
| Create GET/POST | PASS | `FileItemsController.Create` GET/POST uses `FileItemFormViewModel` and anti-forgery. |
| Edit GET/POST | PASS | `FileItemsController.Edit` GET/POST uses `FileItemFormViewModel` and updates scalar/FK metadata plus tag joins. |
| Delete GET/POST with confirmation | PASS | Delete view confirms delete; POST uses `[ValidateAntiForgeryToken]`. |
| Delete blocked when ChangeLogs exist | PASS | `FileItemHasChangeLogs` is checked in GET/POST; blocked view does not render POST delete form. |
| Name required/bounded | PASS | Form model uses `[Required]` and `[StringLength(120)]`; client uses required/max length blur validation. |
| Path required/bounded | PASS | Form model uses `[Required]` and `[StringLength(260)]`; client uses required/max length blur validation. |
| Extension optional/bounded | PASS | Form model uses `[StringLength(20)]`; client uses max length validation. |
| Size nonnegative | PASS | Form model uses `[Range(0, long.MaxValue)]`; client uses nonnegative blur validation. |
| Required DirectoryItem autocomplete | PASS | `DirectoryAutocomplete` endpoint returns `{ id, text }`; form requires and redisplays selected directory label. |
| FileTag selection | PASS | Form shows checkbox list from current tags and validates selected tag IDs server-side. |
| CreatedDate/ModifiedDate text DateTime validation | PASS | Shared `_DateTimePicker` is used for both fields; form model parses both through `DateTimeInputParser`. |
| No native datepicker | PASS | Verification found no `type="date"` or `type="datetime-local"`. |
| FileChangeLog no Create/Edit/Delete additions | PASS | Verification found no FileChangeLog CRUD additions; change logs remain display-only. |
| No raw password/credential exposure | PASS | Verification found no password/credential references in FileItem views. |
| No mass unrelated entity CRUD | PASS | Changes are scoped to FileItems/FileItem plus reusable autocomplete message/CSS support. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors. |

## FileTag Etapa 2 PASS/FAIL

Review timestamp: 2026-05-20 21:22:36 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| FileTag Index AJAX search without full reload | PASS | `/Tags/Search` returns `_TagRows`; Index uses `data-lab4-search`, loading indicator, partial tbody replacement, and row highlight. |
| Search by name, description, and color | PASS | `EfNasRepository.SearchTags` filters `Name`, `Description`, and `Color`; empty query returns all tags. |
| Details preserved/improved | PASS | Details keeps files list and adds Edit/Delete/Back actions. |
| Create GET/POST | PASS | `TagsController.Create` GET/POST uses `FileTagFormViewModel` and anti-forgery. |
| Edit GET/POST | PASS | `TagsController.Edit` GET/POST uses `FileTagFormViewModel` and updates scalar fields only. |
| Delete GET/POST with confirmation | PASS | Delete view confirms delete; POST uses `[ValidateAntiForgeryToken]`. |
| Delete blocked when files exist | PASS | `FileTagHasFiles` is checked in GET/POST; blocked view does not render the POST delete form. |
| Server-side validation in POST endpoints | PASS | Create/Edit check `ModelState.IsValid`; form model validates name, lengths, and hex color. |
| Client-side blur/focus-loss validation | PASS | Existing `lab4.js` validates required name, max lengths, and `hexcolor` on blur/submit. |
| Bootstrap-friendly validation messages | PASS | Reuses `validation-summary`, field validation spans, `form-control`, and alert styles. |
| Reuse existing Labos 4 JS/CSS pattern | PASS | Uses `lab4.js`, `lab4.css`, `data-lab4-form`, `data-lab4-search`, row highlight, and shared delete confirmation behavior. |
| FileChangeLog no Create/Edit/Delete additions | PASS | Verification found no FileChangeLog CRUD additions. |
| No native date inputs | PASS | Verification found no `type="date"` or `type="datetime-local"`. |
| No mass unrelated entity CRUD | PASS | Changes are scoped to Tags/FileTag plus reusable JS/CSS support and repository interface methods. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors after stopping a stale local app process that locked the output exe. |

## DirectoryItem Etapa 3 PASS/FAIL

Review timestamp: 2026-05-20 21:44:56 +02:00

| Requirement | Status | Evidence/notes |
|---|---|---|
| DirectoryItem Index AJAX search without full reload | PASS | `/Directories/Search` returns `_DirectoryRows`; Index uses `data-lab4-search`, loading indicator, partial tbody replacement, and row highlight. |
| Search by name/path, parent, and scan job root | PASS | `EfNasRepository.SearchDirectories` filters directory name/path, parent name/path, and scan job root path. |
| Details preserved/improved | PASS | Details keeps parent/date/files/subdirectories and adds scan job plus Edit/Delete/Back actions. |
| Create GET/POST | PASS | `DirectoriesController.Create` GET/POST uses `DirectoryItemFormViewModel` and anti-forgery. |
| Edit GET/POST | PASS | `DirectoriesController.Edit` GET/POST uses `DirectoryItemFormViewModel` and updates scalar/FK metadata only. |
| Delete GET/POST with confirmation | PASS | Delete view confirms delete; POST uses `[ValidateAntiForgeryToken]`. |
| Delete blocked when child directories/files exist | PASS | `DirectoryHasChildrenOrFiles` is checked in GET/POST; blocked view does not render POST delete form. |
| Name required/bounded | PASS | Form model uses `[Required]` and `[StringLength(100)]`; client uses required/max length blur validation. |
| Path required/bounded | PASS | Form model uses `[Required]` and `[StringLength(260)]`; client uses required/max length blur validation. |
| CreatedDate/ModifiedDate text DateTime validation | PASS | Shared `_DateTimePicker` is used for both fields; form model parses both through `DateTimeInputParser`. |
| No native datepicker | PASS | Verification found no `type="date"` or `type="datetime-local"`. |
| Optional ScanJob autocomplete | PASS | `ScanJobAutocomplete` endpoint returns `{ id, text }`; form redisplays selected label on Edit/invalid POST. |
| Optional parent directory autocomplete | PASS | `ParentDirectoryAutocomplete` endpoint returns `{ id, text }`, excludes current directory on Edit, and form redisplays selected label. |
| Self-reference prevention | PASS | ViewModel and controller reject `ParentDirectoryId == Id`. |
| Parent cycle prevention | PASS | Repository walks persisted parent chain and controller rejects descendant parent choices. |
| Server-side validation in POST endpoints | PASS | Create/Edit call `TryBuildDirectory` and return View(model) when `ModelState` is invalid. |
| Client-side blur/focus-loss validation | PASS | Existing `lab4.js` validates required/max length/date fields on blur/submit; hierarchy checks remain server-owned. |
| FileChangeLog no Create/Edit/Delete additions | PASS | Verification found no FileChangeLog CRUD additions. |
| No mass unrelated entity CRUD | PASS | Changes are scoped to Directories/DirectoryItem plus reusable optional autocomplete support. |
| Build | PASS | `dotnet build` succeeds with 0 warnings and 0 errors. |
