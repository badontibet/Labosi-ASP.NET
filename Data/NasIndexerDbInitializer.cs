using NasIndexer.Model;

namespace NasIndexer.Data
{
    public static class NasIndexerDbInitializer
    {
        public static void Seed(NasIndexerDbContext context)
        {
            if (context.NasServers.Any())
            {
                return;
            }

            var scanStarted = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
            var scanFinished = new DateTime(2026, 1, 10, 9, 24, 0, DateTimeKind.Utc);
            var secondScanStarted = new DateTime(2026, 1, 11, 10, 15, 0, DateTimeKind.Utc);
            var thirdScanStarted = new DateTime(2026, 1, 12, 8, 30, 0, DateTimeKind.Utc);
            var fileCreated = new DateTime(2025, 12, 1, 12, 0, 0, DateTimeKind.Utc);
            var fileModified = new DateTime(2026, 1, 9, 16, 45, 0, DateTimeKind.Utc);
            var adminCreated = new DateTime(2025, 11, 15, 8, 0, 0, DateTimeKind.Utc);

            var primaryServer = new NasServer
            {
                Name = "NAS Alpha",
                IpAddress = "192.168.1.20",
                Port = 445,
                Username = "lab-reader",
                Password = "sample-password",
                IsActive = true,
                LastScan = scanFinished
            };

            var archiveServer = new NasServer
            {
                Name = "NAS Archive",
                IpAddress = "192.168.1.21",
                Port = 445,
                Username = "archive-reader",
                Password = "sample-password",
                IsActive = true,
                LastScan = secondScanStarted
            };

            var admin = new SystemAdmin
            {
                Username = "admin.lab",
                Password = "sample-password",
                Email = "admin.lab@example.local",
                Role = "Administrator",
                CreatedDate = adminCreated,
                LastLogin = new DateTime(2026, 1, 12, 7, 55, 0, DateTimeKind.Utc)
            };

            var auditor = new SystemAdmin
            {
                Username = "auditor.lab",
                Password = "sample-password",
                Email = "auditor.lab@example.local",
                Role = "Auditor",
                CreatedDate = adminCreated.AddDays(1),
                LastLogin = new DateTime(2026, 1, 11, 15, 10, 0, DateTimeKind.Utc)
            };

            admin.ManagedServers.Add(primaryServer);
            admin.ManagedServers.Add(archiveServer);
            auditor.ManagedServers.Add(archiveServer);

            var completedScan = new ScanJob
            {
                NasServer = primaryServer,
                Status = ScanStatus.Completed,
                StartTime = scanStarted,
                EndTime = scanFinished,
                RootPath = "/share/projects",
                TotalFiles = 3,
                ProcessedFiles = 3
            };

            var runningScan = new ScanJob
            {
                NasServer = archiveServer,
                Status = ScanStatus.Running,
                StartTime = secondScanStarted,
                EndTime = null,
                RootPath = "/archive/2026",
                TotalFiles = 2,
                ProcessedFiles = 1
            };

            var failedScan = new ScanJob
            {
                NasServer = primaryServer,
                Status = ScanStatus.Failed,
                StartTime = thirdScanStarted,
                EndTime = thirdScanStarted.AddMinutes(12),
                RootPath = "/share/media",
                TotalFiles = 1,
                ProcessedFiles = 0
            };

            var projectsRoot = new DirectoryItem
            {
                Name = "projects",
                Path = "/share/projects",
                ScanJob = completedScan,
                CreatedDate = fileCreated,
                ModifiedDate = fileModified
            };

            var webAppDirectory = new DirectoryItem
            {
                Name = "web-app",
                Path = "/share/projects/web-app",
                ScanJob = completedScan,
                Parent = projectsRoot,
                CreatedDate = fileCreated.AddDays(2),
                ModifiedDate = fileModified
            };

            var archiveRoot = new DirectoryItem
            {
                Name = "2026",
                Path = "/archive/2026",
                ScanJob = runningScan,
                CreatedDate = fileCreated.AddDays(-10),
                ModifiedDate = secondScanStarted
            };

            var mediaRoot = new DirectoryItem
            {
                Name = "media",
                Path = "/share/media",
                ScanJob = failedScan,
                CreatedDate = fileCreated.AddDays(-20),
                ModifiedDate = thirdScanStarted
            };

            var sourceFile = new FileItem
            {
                Name = "Program.cs",
                Path = "/share/projects/web-app/Program.cs",
                Size = 4096,
                Extension = ".cs",
                CreatedDate = fileCreated.AddDays(2),
                ModifiedDate = fileModified,
                Directory = webAppDirectory
            };

            var readmeFile = new FileItem
            {
                Name = "README.md",
                Path = "/share/projects/README.md",
                Size = 2048,
                Extension = ".md",
                CreatedDate = fileCreated,
                ModifiedDate = fileModified.AddHours(-1),
                Directory = projectsRoot
            };

            var archiveFile = new FileItem
            {
                Name = "audit-jan.csv",
                Path = "/archive/2026/audit-jan.csv",
                Size = 8192,
                Extension = ".csv",
                CreatedDate = fileCreated.AddDays(12),
                ModifiedDate = secondScanStarted,
                Directory = archiveRoot
            };

            var codeTag = new FileTag
            {
                Name = "Code",
                Description = "Source code and project files",
                Color = "#2563eb"
            };

            var docsTag = new FileTag
            {
                Name = "Documentation",
                Description = "Human-readable project documentation",
                Color = "#16a34a"
            };

            var auditTag = new FileTag
            {
                Name = "Audit",
                Description = "Files used for review or compliance checks",
                Color = "#f59e0b"
            };

            sourceFile.Tags.Add(codeTag);
            readmeFile.Tags.Add(docsTag);
            archiveFile.Tags.Add(auditTag);

            sourceFile.ChangeLogs.Add(new FileChangeLog
            {
                File = sourceFile,
                ChangeType = ChangeType.Created,
                Timestamp = fileCreated.AddDays(2),
                OldValue = string.Empty,
                NewValue = "/share/projects/web-app/Program.cs",
                User = "admin.lab"
            });

            sourceFile.ChangeLogs.Add(new FileChangeLog
            {
                File = sourceFile,
                ChangeType = ChangeType.Modified,
                Timestamp = fileModified,
                OldValue = "Initial version",
                NewValue = "Updated routing setup",
                User = "admin.lab"
            });

            readmeFile.ChangeLogs.Add(new FileChangeLog
            {
                File = readmeFile,
                ChangeType = ChangeType.Modified,
                Timestamp = fileModified.AddHours(-1),
                OldValue = "Draft notes",
                NewValue = "Lab setup notes",
                User = "auditor.lab"
            });

            context.SystemAdmins.AddRange(admin, auditor);
            context.NasServers.AddRange(primaryServer, archiveServer);
            context.ScanJobs.AddRange(completedScan, runningScan, failedScan);
            context.DirectoryItems.AddRange(projectsRoot, webAppDirectory, archiveRoot, mediaRoot);
            context.FileItems.AddRange(sourceFile, readmeFile, archiveFile);
            context.FileTags.AddRange(codeTag, docsTag, auditTag);

            context.SaveChanges();
        }
    }
}
