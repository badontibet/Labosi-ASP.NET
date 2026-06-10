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
    }
}
