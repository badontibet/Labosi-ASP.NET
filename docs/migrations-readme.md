# EF Core Migrations

This project uses a local SQLite database named `nas-indexer.db`.

## Commands Used For Lab 3 Step 08

Global EF tool check failed because `dotnet ef` was not available on PATH:

```powershell
dotnet ef --version
```

A local EF tool manifest and EF Core 8 tool were created:

```powershell
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 8.*
dotnet tool run dotnet-ef --version
```

The initial migration was created with the local tool:

```powershell
dotnet tool run dotnet-ef migrations add InitialCreate
```

The local SQLite database was updated with the local tool:

```powershell
dotnet tool run dotnet-ef database update
```

The project was built:

```powershell
dotnet build
```

The applied migration was verified:

```powershell
dotnet tool run dotnet-ef migrations list
```

The app startup was verified on a temporary local URL:

```powershell
dotnet run --no-build --urls http://127.0.0.1:4232
```

## Notes

- Keep generated files in `Migrations/` in source control.
- Do not commit `nas-indexer.db`, `nas-indexer.db-shm`, or `nas-indexer.db-wal`.
- The local EF tool is recorded in `dotnet-tools.json`.
