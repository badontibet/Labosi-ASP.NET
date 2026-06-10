using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NasIndexer.Data;
using NasIndexer.Model;

namespace Labosi_ASP.NET.Tests
{
    public static class TestDataFactory
    {
        public static async Task<FileTag> CreateTagAsync(
            CustomWebApplicationFactory factory,
            string name,
            string? description = null,
            string? color = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            var tag = new FileTag
            {
                Name = name,
                Description = description ?? string.Empty,
                Color = color ?? string.Empty
            };

            dbContext.FileTags.Add(tag);
            await dbContext.SaveChangesAsync();

            return tag;
        }

        public static async Task<FileTag> CreateAssignedTagAsync(
            CustomWebApplicationFactory factory,
            string name)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

            var tag = new FileTag
            {
                Name = name,
                Description = "Assigned test tag",
                Color = "#AA5500"
            };

            var directory = new DirectoryItem
            {
                Name = $"dir-{Guid.NewGuid():N}",
                Path = $"/tests/{Guid.NewGuid():N}",
                CreatedDate = now,
                ModifiedDate = now
            };

            var file = new FileItem
            {
                Name = $"file-{Guid.NewGuid():N}.txt",
                Path = $"{directory.Path}/file.txt",
                Extension = ".txt",
                Size = 128,
                CreatedDate = now,
                ModifiedDate = now,
                Directory = directory
            };

            file.Tags.Add(tag);

            dbContext.DirectoryItems.Add(directory);
            dbContext.FileItems.Add(file);
            await dbContext.SaveChangesAsync();

            return tag;
        }

        public static async Task<FileTag?> FindTagAsync(
            CustomWebApplicationFactory factory,
            int id)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.FileTags
                .AsNoTracking()
                .Include(tag => tag.Files)
                .FirstOrDefaultAsync(tag => tag.Id == id);
        }

        public static async Task<NasServer> CreateNasServerAsync(
            CustomWebApplicationFactory factory,
            string name,
            string? ipAddress = null,
            string? username = null,
            string? password = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            var server = new NasServer
            {
                Name = name,
                IpAddress = ipAddress ?? "10.10.0.10",
                Port = 445,
                Username = username ?? "test-reader",
                Password = password ?? string.Empty,
                IsActive = true,
                LastScan = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc)
            };

            dbContext.NasServers.Add(server);
            await dbContext.SaveChangesAsync();

            return server;
        }

        public static async Task<NasServer> CreateNasServerWithScanJobAsync(
            CustomWebApplicationFactory factory,
            string name)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var startTime = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc);

            var server = new NasServer
            {
                Name = name,
                IpAddress = "10.10.0.20",
                Port = 445,
                Username = "scan-reader",
                Password = "dependent-secret",
                IsActive = true,
                LastScan = startTime
            };

            server.ScanJobs.Add(new ScanJob
            {
                Status = ScanStatus.Completed,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(5),
                RootPath = $"/tests/{Guid.NewGuid():N}",
                TotalFiles = 10,
                ProcessedFiles = 10
            });

            dbContext.NasServers.Add(server);
            await dbContext.SaveChangesAsync();

            return server;
        }

        public static async Task<NasServer?> FindNasServerAsync(
            CustomWebApplicationFactory factory,
            int id)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.NasServers
                .AsNoTracking()
                .Include(server => server.ScanJobs)
                .Include(server => server.ManagedAdmins)
                .FirstOrDefaultAsync(server => server.Id == id);
        }

        public static async Task<ScanJob> CreateScanJobAsync(
            CustomWebApplicationFactory factory,
            int? nasServerId = null,
            string? rootPath = null,
            ScanStatus status = ScanStatus.Pending)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var startTime = new DateTime(2026, 6, 10, 15, 0, 0, DateTimeKind.Utc);
            NasServer? server = null;

            if (!nasServerId.HasValue)
            {
                server = new NasServer
                {
                    Name = $"scan-server-{Guid.NewGuid():N}",
                    IpAddress = "10.20.0.10",
                    Port = 445,
                    Username = "scan-reader",
                    Password = string.Empty,
                    IsActive = true,
                    LastScan = startTime
                };

                dbContext.NasServers.Add(server);
                await dbContext.SaveChangesAsync();
                nasServerId = server.Id;
            }

            var scanJob = new ScanJob
            {
                NasServerId = nasServerId.Value,
                Status = status,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(10),
                RootPath = rootPath ?? $"/scan/{Guid.NewGuid():N}",
                TotalFiles = 20,
                ProcessedFiles = 15
            };

            dbContext.ScanJobs.Add(scanJob);
            await dbContext.SaveChangesAsync();

            return scanJob;
        }

        public static async Task<ScanJob> CreateScanJobWithDirectoryAsync(
            CustomWebApplicationFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var startTime = new DateTime(2026, 6, 10, 16, 0, 0, DateTimeKind.Utc);

            var server = new NasServer
            {
                Name = $"blocked-scan-server-{Guid.NewGuid():N}",
                IpAddress = "10.20.0.20",
                Port = 445,
                Username = "blocked-reader",
                Password = string.Empty,
                IsActive = true,
                LastScan = startTime
            };

            var scanJob = new ScanJob
            {
                NasServer = server,
                Status = ScanStatus.Completed,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(12),
                RootPath = $"/blocked/{Guid.NewGuid():N}",
                TotalFiles = 4,
                ProcessedFiles = 4
            };

            scanJob.ScannedDirectories.Add(new DirectoryItem
            {
                Name = $"blocked-dir-{Guid.NewGuid():N}",
                Path = $"{scanJob.RootPath}/dir",
                CreatedDate = startTime,
                ModifiedDate = startTime,
                ScanJob = scanJob
            });

            dbContext.ScanJobs.Add(scanJob);
            await dbContext.SaveChangesAsync();

            return scanJob;
        }

        public static async Task<ScanJob?> FindScanJobAsync(
            CustomWebApplicationFactory factory,
            int id)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .Include(scanJob => scanJob.ScannedDirectories)
                .FirstOrDefaultAsync(scanJob => scanJob.Id == id);
        }

        public static async Task<DirectoryItem> CreateDirectoryAsync(
            CustomWebApplicationFactory factory,
            string? name = null,
            string? path = null,
            int? scanJobId = null,
            int? parentId = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 10, 17, 0, 0, DateTimeKind.Utc);

            var directory = new DirectoryItem
            {
                Name = name ?? $"dir-{Guid.NewGuid():N}",
                Path = path ?? $"/directories/{Guid.NewGuid():N}",
                ScanJobId = scanJobId,
                ParentId = parentId,
                CreatedDate = now,
                ModifiedDate = now
            };

            dbContext.DirectoryItems.Add(directory);
            await dbContext.SaveChangesAsync();

            return directory;
        }

        public static async Task<DirectoryItem> CreateDirectoryWithChildAsync(
            CustomWebApplicationFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc);

            var parent = new DirectoryItem
            {
                Name = $"parent-{Guid.NewGuid():N}"[..20],
                Path = $"/parent/{Guid.NewGuid():N}",
                CreatedDate = now,
                ModifiedDate = now
            };

            parent.SubDirectories.Add(new DirectoryItem
            {
                Name = $"child-{Guid.NewGuid():N}"[..20],
                Path = $"{parent.Path}/child",
                CreatedDate = now,
                ModifiedDate = now
            });

            dbContext.DirectoryItems.Add(parent);
            await dbContext.SaveChangesAsync();

            return parent;
        }

        public static async Task<DirectoryItem> CreateDirectoryWithFileAsync(
            CustomWebApplicationFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 10, 19, 0, 0, DateTimeKind.Utc);

            var directory = new DirectoryItem
            {
                Name = $"files-{Guid.NewGuid():N}"[..20],
                Path = $"/files/{Guid.NewGuid():N}",
                CreatedDate = now,
                ModifiedDate = now
            };

            directory.Files.Add(new FileItem
            {
                Name = $"file-{Guid.NewGuid():N}.txt",
                Path = $"{directory.Path}/file.txt",
                Extension = ".txt",
                Size = 256,
                CreatedDate = now,
                ModifiedDate = now
            });

            dbContext.DirectoryItems.Add(directory);
            await dbContext.SaveChangesAsync();

            return directory;
        }

        public static async Task<DirectoryItem?> FindDirectoryAsync(
            CustomWebApplicationFactory factory,
            int id)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.DirectoryItems
                .AsNoTracking()
                .Include(directory => directory.Parent)
                .Include(directory => directory.ScanJob)
                .Include(directory => directory.SubDirectories)
                .Include(directory => directory.Files)
                .FirstOrDefaultAsync(directory => directory.Id == id);
        }

        public static async Task<FileItem> CreateFileItemAsync(
            CustomWebApplicationFactory factory,
            int? directoryId = null,
            IEnumerable<int>? tagIds = null,
            string? name = null,
            string? path = null,
            string? extension = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 10, 20, 0, 0, DateTimeKind.Utc);

            if (!directoryId.HasValue)
            {
                var directory = new DirectoryItem
                {
                    Name = $"file-dir-{Guid.NewGuid():N}"[..20],
                    Path = $"/file-dir/{Guid.NewGuid():N}",
                    CreatedDate = now,
                    ModifiedDate = now
                };

                dbContext.DirectoryItems.Add(directory);
                await dbContext.SaveChangesAsync();
                directoryId = directory.Id;
            }

            var file = new FileItem
            {
                Name = name ?? $"file-{Guid.NewGuid():N}.txt",
                Path = path ?? $"/files/{Guid.NewGuid():N}/file.txt",
                Extension = extension ?? ".txt",
                Size = 512,
                CreatedDate = now,
                ModifiedDate = now,
                DirectoryId = directoryId.Value
            };

            if (tagIds != null)
            {
                foreach (var tag in dbContext.FileTags.Where(tag => tagIds.Contains(tag.Id)))
                {
                    file.Tags.Add(tag);
                }
            }

            dbContext.FileItems.Add(file);
            await dbContext.SaveChangesAsync();

            return file;
        }

        public static async Task<FileItem> CreateFileItemWithChangeLogAsync(
            CustomWebApplicationFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 10, 21, 0, 0, DateTimeKind.Utc);

            var directory = new DirectoryItem
            {
                Name = $"audit-dir-{Guid.NewGuid():N}"[..20],
                Path = $"/audit-dir/{Guid.NewGuid():N}",
                CreatedDate = now,
                ModifiedDate = now
            };

            var file = new FileItem
            {
                Name = $"audit-{Guid.NewGuid():N}.txt",
                Path = $"{directory.Path}/audit.txt",
                Extension = ".txt",
                Size = 1024,
                CreatedDate = now,
                ModifiedDate = now,
                Directory = directory
            };

            file.ChangeLogs.Add(new FileChangeLog
            {
                ChangeType = ChangeType.Created,
                Timestamp = now,
                OldValue = string.Empty,
                NewValue = file.Path,
                User = "integration-test"
            });

            dbContext.FileItems.Add(file);
            await dbContext.SaveChangesAsync();

            return file;
        }

        public static async Task<FileItem?> FindFileItemAsync(
            CustomWebApplicationFactory factory,
            int id)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();

            return await dbContext.FileItems
                .AsNoTracking()
                .Include(file => file.Directory)
                .Include(file => file.Tags)
                .Include(file => file.ChangeLogs)
                .FirstOrDefaultAsync(file => file.Id == id);
        }

        public static async Task<FileChangeLog> CreateFileChangeLogAsync(
            CustomWebApplicationFactory factory,
            int? fileId = null,
            ChangeType changeType = ChangeType.Modified,
            string? oldValue = null,
            string? newValue = null,
            string? user = null)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            var now = new DateTime(2026, 6, 10, 22, 0, 0, DateTimeKind.Utc);

            if (!fileId.HasValue)
            {
                var directory = new DirectoryItem
                {
                    Name = $"log-dir-{Guid.NewGuid():N}"[..20],
                    Path = $"/log-dir/{Guid.NewGuid():N}",
                    CreatedDate = now,
                    ModifiedDate = now
                };

                var file = new FileItem
                {
                    Name = $"log-{Guid.NewGuid():N}.txt",
                    Path = $"{directory.Path}/log.txt",
                    Extension = ".txt",
                    Size = 512,
                    CreatedDate = now,
                    ModifiedDate = now,
                    Directory = directory
                };

                dbContext.FileItems.Add(file);
                await dbContext.SaveChangesAsync();
                fileId = file.Id;
            }

            var changeLog = new FileChangeLog
            {
                FileId = fileId.Value,
                ChangeType = changeType,
                Timestamp = now,
                OldValue = oldValue ?? "old-value",
                NewValue = newValue ?? "new-value",
                User = user ?? "integration-test"
            };

            dbContext.FileChangeLogs.Add(changeLog);
            await dbContext.SaveChangesAsync();

            return changeLog;
        }
    }
}
