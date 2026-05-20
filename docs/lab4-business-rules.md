# Labos 4 Business Rules

Status: planning document only. No Labos 4 features are implemented by this document.

## Global Rules

- Preserve existing Labos 1/2/3 routes and read-only pages while adding Labos 4 functionality.
- Use server-side validation in every POST action before saving changes.
- Add client-side blur/focus-loss validation for form fields.
- Use text-based DateTime inputs through a reusable partial view; do not use `input type="date"` or `input type="datetime-local"`.
- Support both Croatian and English DateTime entry/display formats in the custom DateTime handling.
- Add AJAX search to every list page.
- Use a custom reusable AJAX autocomplete/dropdown for relationship selection.
- Prefer form-specific ViewModels/FormModels over binding EF entities directly, especially for sensitive fields and relationship IDs.

## Entity Rules

| Entity | Allowed operations | Restricted or forbidden operations | Business rule reason | Delete strategy | Sensitive/protected fields | DateTime fields | Autocomplete/dropdown needed | AJAX search needed | Recommended ViewModel/FormModel |
|---|---|---|---|---|---|---|---|---|---|
| `NasServer` | Index, Details, Create, Edit, Delete | Delete is restricted when related scan jobs or managed admins exist; do not display or edit raw credentials in list/details | Server is a root operational object, but DbContext would cascade scan jobs if deleted and model contains credential-like data | Application-level block before delete if `ScanJobs.Any()` or `ManagedAdmins.Any()`; show explanation instead of deleting | `Username`, `Password`, possibly `IpAddress` depending display context | `LastScan` | Yes: multi-select/autocomplete for managed `SystemAdmin` relationship if relationship editing is included | Yes | `NasServerFormViewModel` with safe fields, `LastScanText`, optional selected admin IDs; exclude raw password from normal display |
| `ScanJob` | Index, Details, Create, Edit, Delete | Delete is restricted when scanned directories exist; `EndTime` cannot be before `StartTime`; processed files cannot exceed total files | Required Labos 4 gold example; scan job depends on NAS server and represents time-based scan state | Application-level block if `ScannedDirectories.Any()`; otherwise allow delete | None credential-like; protect relational integrity for `NasServerId` | `StartTime`, nullable `EndTime` | Yes: `NasServer` autocomplete/dropdown; `ScanStatus` enum dropdown | Yes | `ScanJobFormViewModel` with `NasServerId`, `NasServerLabel`, `Status`, `StartTimeText`, `EndTimeText`, `RootPath`, `TotalFiles`, `ProcessedFiles` |
| `DirectoryItem` | Index, Details, Create, Edit, Delete | Delete is restricted when child directories or files exist; parent cannot be self or descendant; avoid cycles | Self-referencing hierarchy can become invalid and DbContext has restrict delete for parent relationship | Application-level block if `SubDirectories.Any()` or `Files.Any()`; validate parent tree before save | None | `CreatedDate`, `ModifiedDate` | Yes: optional `ScanJob` autocomplete/dropdown and optional parent directory autocomplete/dropdown | Yes | `DirectoryItemFormViewModel` with parent/scan job IDs and labels, `CreatedDateText`, `ModifiedDateText` |
| `FileItem` | Index, Details, Create, Edit, Delete for metadata | Delete is restricted when change logs exist; tag edits should be explicit and validated; directory is required | File metadata can be edited, but audit history must not be silently destroyed and file belongs to a directory | Application-level block if `ChangeLogs.Any()`; otherwise delete metadata and join-table rows carefully | None credential-like | `CreatedDate`, `ModifiedDate` | Yes: required `DirectoryItem` autocomplete/dropdown and tag multi-select/autocomplete | Yes | `FileItemFormViewModel` with `DirectoryId`, `DirectoryLabel`, selected tag IDs, size/extension/path fields, `CreatedDateText`, `ModifiedDateText` |
| `FileTag` | Index, Details, Create, Edit, Delete | Delete is restricted when assigned to any file | Tags are reusable classification data and many-to-many links should not be broken accidentally | Application-level block if `Files.Any()`; otherwise allow delete | None | None | Optional: used by `FileItem` tag multi-select/autocomplete | Yes | `FileTagFormViewModel` with name, description, color; validate color format |
| `FileChangeLog` | Index, Details, AJAX search | Create, Edit, Delete are forbidden for normal Labos 4 CRUD UI | Audit/history records must be append-only/read-only from user UI | No user-facing delete; if cleanup is ever needed, keep it out of Labos 4 CRUD UI | `User` can identify actor; old/new values may contain sensitive paths or names | `Timestamp` | Yes for read-only filtering/search by `FileItem`; `ChangeType` enum dropdown for filters only | Yes | `FileChangeLogSearchViewModel` and optional read-only `FileChangeLogDetailsViewModel`; no edit form model |
| `SystemAdmin` | Index, Details, Create, Edit, Delete with restrictions | Do not expose or edit raw password through normal CRUD; delete restricted when assigned to servers | Admin model contains credential-like field and many-to-many server responsibility relationship | Application-level block if `ManagedServers.Any()`; do not delete assigned admins | `Password`, possibly `Email` depending view context | `CreatedDate`, `LastLogin` | Yes: managed `NasServer` multi-select/autocomplete and role dropdown | Yes | `SystemAdminFormViewModel` with username, email, role, date text fields, selected server IDs; omit raw password or use a separate password workflow if required |

## Enum Rules

| Enum | Usage rule |
|---|---|
| `ScanStatus` | Use a dropdown for `ScanJob` forms and filters with values `Pending`, `Running`, `Completed`, `Failed`. Validate progress/date consistency against selected status. |
| `ChangeType` | Use only for read-only `FileChangeLog` display/search filters with values `Created`, `Modified`, `Deleted`, `Renamed`. Do not expose as an editable audit-log CRUD field. |

## Assumptions And Risks

- These rules intentionally override some database cascade behavior with application-level delete blocks to avoid destructive Labos 4 UI actions.
- `Password` properties currently exist as plain strings in `NasServer` and `SystemAdmin`; Labos 4 UI should not display raw values and should avoid editing them unless a separate safe workflow is explicitly requested.
- `FileChangeLog` is treated as audit data and remains read-only even though EF maps it as a table.
- DateTime parsing/formatting needs a shared implementation so hr/en behavior stays consistent across all forms.
- Full CRUD means full UI workflow only where allowed by these rules; restricted entities still need clear blocked-delete messages and server-side validation.
