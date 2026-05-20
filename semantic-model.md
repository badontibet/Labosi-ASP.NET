# Semantic Model

## Project Description

NAS Indexer is an ASP.NET Core MVC lab application that displays NAS servers, scan jobs, indexed directories, indexed files, tags, file change history, and system administrators. Lab 3 stores the domain model with Entity Framework Core and SQLite through `NasIndexerDbContext`.

## Entity And Table List

| Entity | EF table |
|---|---|
| `NasServer` | `NasServers` |
| `ScanJob` | `ScanJobs` |
| `DirectoryItem` | `DirectoryItems` |
| `FileItem` | `FileItems` |
| `FileTag` | `FileTags` |
| `FileChangeLog` | `FileChangeLogs` |
| `SystemAdmin` | `SystemAdmins` |

EF also creates join tables for skip-navigation many-to-many relationships:

| Join table | Connects |
|---|---|
| `FileItemTags` | `FileItem` N-N `FileTag` |
| `SystemAdminNasServers` | `SystemAdmin` N-N `NasServer` |

## Entities

### NasServer

- Primary key: `Id`
- Main scalar properties: `Name`, `IpAddress`, `Port`, `Username`, `Password`, `IsActive`, `LastScan`
- Foreign keys: none
- Navigation properties:
  - `ScanJobs`: `ICollection<ScanJob>`
  - `ManagedAdmins`: `ICollection<SystemAdmin>`
- Relationship types:
  - 1-N with `ScanJob`
  - N-N with `SystemAdmin` through `SystemAdminNasServers`

### ScanJob

- Primary key: `Id`
- Main scalar properties: `Status`, `StartTime`, `EndTime`, `RootPath`, `TotalFiles`, `ProcessedFiles`
- Foreign keys:
  - `NasServerId`
- Navigation properties:
  - `NasServer`: `NasServer`
  - `ScannedDirectories`: `ICollection<DirectoryItem>`
- Relationship types:
  - N-1 to `NasServer`
  - 1-N with `DirectoryItem`

### DirectoryItem

- Primary key: `Id`
- Main scalar properties: `Name`, `Path`, `CreatedDate`, `ModifiedDate`
- Foreign keys:
  - `ScanJobId`
  - `ParentId`
- Navigation properties:
  - `ScanJob`: `ScanJob?`
  - `Parent`: `DirectoryItem?`
  - `SubDirectories`: `ICollection<DirectoryItem>`
  - `Files`: `ICollection<FileItem>`
- Relationship types:
  - N-1 to `ScanJob`
  - self-reference: parent `DirectoryItem` 1-N child `DirectoryItem`
  - 1-N with `FileItem`

### FileItem

- Primary key: `Id`
- Main scalar properties: `Name`, `Path`, `Size`, `Extension`, `CreatedDate`, `ModifiedDate`
- Foreign keys:
  - `DirectoryId`
- Navigation properties:
  - `Directory`: `DirectoryItem`
  - `Tags`: `ICollection<FileTag>`
  - `ChangeLogs`: `ICollection<FileChangeLog>`
- Relationship types:
  - N-1 to `DirectoryItem`
  - N-N with `FileTag` through `FileItemTags`
  - 1-N with `FileChangeLog`

### FileTag

- Primary key: `Id`
- Main scalar properties: `Name`, `Description`, `Color`
- Foreign keys: none
- Navigation properties:
  - `Files`: `ICollection<FileItem>`
- Relationship types:
  - N-N with `FileItem` through `FileItemTags`

### FileChangeLog

- Primary key: `Id`
- Main scalar properties: `ChangeType`, `Timestamp`, `OldValue`, `NewValue`, `User`
- Foreign keys:
  - `FileId`
- Navigation properties:
  - `File`: `FileItem`
- Relationship types:
  - N-1 to `FileItem`

### SystemAdmin

- Primary key: `Id`
- Main scalar properties: `Username`, `Password`, `Email`, `Role`, `CreatedDate`, `LastLogin`
- Foreign keys: none
- Navigation properties:
  - `ManagedServers`: `ICollection<NasServer>`
- Relationship types:
  - N-N with `NasServer` through `SystemAdminNasServers`

## Relationship Summary

```text
NasServer 1 -- N ScanJob
NasServer N -- N SystemAdmin
ScanJob 1 -- N DirectoryItem
DirectoryItem 1 -- N DirectoryItem
DirectoryItem 1 -- N FileItem
FileItem N -- N FileTag
FileItem 1 -- N FileChangeLog
```

## EF Implementation Notes

- DbContext: `NasIndexer.Data.NasIndexerDbContext`
- DbSet properties:
  - `NasServers`
  - `ScanJobs`
  - `DirectoryItems`
  - `FileItems`
  - `FileTags`
  - `FileChangeLogs`
  - `SystemAdmins`
- Database provider: SQLite through `Microsoft.EntityFrameworkCore.Sqlite`
- Connection string name: `NasIndexerDbContext`
- Connection string value in `appsettings.json`: `Data Source=nas-indexer.db`
- Active repository implementation: `Repositories.EfNasRepository`
- Repository abstraction: `Repositories.INasRepository`
- Previous comparison repository still present: `Repositories.MockNasRepository`
- Startup database flow:
  - `Program.cs` registers `NasIndexerDbContext` with `UseSqlite`
  - `Program.cs` calls `db.Database.Migrate()`
  - `Program.cs` calls `NasIndexerDbInitializer.Seed(db)`
- Initial migration:
  - Migration name: `InitialCreate`
  - Migration id/file prefix: `20260519182421_InitialCreate`
  - Files:
    - `Migrations/20260519182421_InitialCreate.cs`
    - `Migrations/20260519182421_InitialCreate.Designer.cs`
    - `Migrations/NasIndexerDbContextModelSnapshot.cs`

## Delete Behavior From Fluent API

```text
NasServer -> ScanJob: Cascade
ScanJob -> DirectoryItem: SetNull
DirectoryItem -> DirectoryItem parent/children: Restrict
DirectoryItem -> FileItem: Cascade
FileItem -> FileChangeLog: Cascade
FileItem <-> FileTag: EF skip-navigation defaults
SystemAdmin <-> NasServer: EF skip-navigation defaults
```
