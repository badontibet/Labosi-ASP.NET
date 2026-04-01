using System;
using System.Collections.Generic;
using System.Linq;
using NasIndexer.Model;

namespace NasIndexer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Kreiranje podataka - barem 3 NAS servera s razgranatom strukturom
            var servers = CreateSampleData();

            // LINQ upiti nad objektima
            ExecuteLinqQueries(servers);

            Console.WriteLine("Pritisnite Enter za izlaz...");
            Console.ReadLine();
        }

        static List<NasServer> CreateSampleData()
        {
            var servers = new List<NasServer>();

            // Server 1: Glavni poslužitelj
            var server1 = new NasServer
            {
                Id = 1,
                Name = "Glavni NAS Server",
                IpAddress = "192.168.1.100",
                Port = 445,
                Username = "admin",
                Password = "password123",
                IsActive = true,
                LastScan = DateTime.Now.AddDays(-1)
            };

            // Scan job za server 1
            var scanJob1 = new ScanJob
            {
                Id = 1,
                NasServerId = 1,
                NasServer = server1,
                Status = ScanStatus.Completed,
                StartTime = DateTime.Now.AddHours(-2),
                EndTime = DateTime.Now.AddHours(-1),
                RootPath = "/shares/documents",
                TotalFiles = 150,
                ProcessedFiles = 150
            };

            // Direktoriji za scan job 1
            var rootDir1 = new DirectoryItem
            {
                Id = 1,
                Name = "Documents",
                Path = "/shares/documents",
                CreatedDate = DateTime.Now.AddMonths(-6),
                ModifiedDate = DateTime.Now.AddDays(-1)
            };

            var subDir1 = new DirectoryItem
            {
                Id = 2,
                Name = "Reports",
                Path = "/shares/documents/reports",
                ParentId = 1,
                Parent = rootDir1,
                CreatedDate = DateTime.Now.AddMonths(-4),
                ModifiedDate = DateTime.Now.AddDays(-2)
            };

            rootDir1.SubDirectories.Add(subDir1);

            // Datoteke
            var file1 = new FileItem
            {
                Id = 1,
                Name = "Q1_Report.pdf",
                Path = "/shares/documents/reports/Q1_Report.pdf",
                Size = 2048000, // 2MB
                Extension = ".pdf",
                CreatedDate = DateTime.Now.AddMonths(-3),
                ModifiedDate = DateTime.Now.AddDays(-5),
                DirectoryId = 2,
                Directory = subDir1
            };

            var file2 = new FileItem
            {
                Id = 2,
                Name = "Q2_Report.pdf",
                Path = "/shares/documents/reports/Q2_Report.pdf",
                Size = 1536000, // 1.5MB
                Extension = ".pdf",
                CreatedDate = DateTime.Now.AddMonths(-2),
                ModifiedDate = DateTime.Now.AddDays(-10),
                DirectoryId = 2,
                Directory = subDir1
            };

            subDir1.Files.AddRange(new[] { file1, file2 });

            // Tagovi
            var tag1 = new FileTag
            {
                Id = 1,
                Name = "Važno",
                Description = "Važni dokumenti",
                Color = "#FF0000"
            };

            file1.Tags.Add(tag1);
            tag1.Files.Add(file1);

            // Change log
            var changeLog1 = new FileChangeLog
            {
                Id = 1,
                FileId = 1,
                File = file1,
                ChangeType = ChangeType.Created,
                Timestamp = DateTime.Now.AddMonths(-3),
                OldValue = null,
                NewValue = "Q1_Report.pdf",
                User = "admin"
            };

            var changeLog2 = new FileChangeLog
            {
                Id = 2,
                FileId = 2,
                File = file2,
                ChangeType = ChangeType.Modified,
                Timestamp = DateTime.Now.AddDays(-5),
                OldValue = "Q2_Report_v1.pdf",
                NewValue = "Q2_Report.pdf",
                User = "admin"
            };

            file1.ChangeLogs.Add(changeLog1);
            file2.ChangeLogs.Add(changeLog2);

            scanJob1.ScannedDirectories.Add(rootDir1);
            server1.ScanJobs.Add(scanJob1);

            // Administrator
            var admin1 = new SystemAdmin
            {
                Id = 1,
                Username = "admin",
                Password = "hashed_password",
                Email = "admin@company.com",
                Role = "SuperAdmin",
                CreatedDate = DateTime.Now.AddYears(-1),
                LastLogin = DateTime.Now.AddHours(-1)
            };

            admin1.ManagedServers.Add(server1);

            servers.Add(server1);

            // Server 2: Backup server
            var server2 = new NasServer
            {
                Id = 2,
                Name = "Backup NAS Server",
                IpAddress = "192.168.1.101",
                Port = 445,
                Username = "backup_admin",
                Password = "backup123",
                IsActive = true,
                LastScan = DateTime.Now.AddHours(-6)
            };

            var scanJob2 = new ScanJob
            {
                Id = 2,
                NasServerId = 2,
                NasServer = server2,
                Status = ScanStatus.Running,
                StartTime = DateTime.Now.AddHours(-1),
                RootPath = "/backup/archives",
                TotalFiles = 500,
                ProcessedFiles = 320
            };

            var rootDir2 = new DirectoryItem
            {
                Id = 3,
                Name = "Archives",
                Path = "/backup/archives",
                CreatedDate = DateTime.Now.AddMonths(-12),
                ModifiedDate = DateTime.Now.AddHours(-2)
            };

            var file3 = new FileItem
            {
                Id = 3,
                Name = "backup_2024.zip",
                Path = "/backup/archives/backup_2024.zip",
                Size = 1073741824, // 1GB
                Extension = ".zip",
                CreatedDate = DateTime.Now.AddMonths(-1),
                ModifiedDate = DateTime.Now.AddDays(-1),
                DirectoryId = 3,
                Directory = rootDir2
            };

            rootDir2.Files.Add(file3);

            var tag2 = new FileTag
            {
                Id = 2,
                Name = "Backup",
                Description = "Sigurnosne kopije",
                Color = "#00FF00"
            };

            file3.Tags.Add(tag2);
            tag2.Files.Add(file3);

            scanJob2.ScannedDirectories.Add(rootDir2);
            server2.ScanJobs.Add(scanJob2);

            servers.Add(server2);

            // Server 3: Development server
            var server3 = new NasServer
            {
                Id = 3,
                Name = "Development NAS Server",
                IpAddress = "192.168.1.102",
                Port = 445,
                Username = "dev_admin",
                Password = "dev123",
                IsActive = false,
                LastScan = DateTime.Now.AddDays(-7)
            };

            var scanJob3 = new ScanJob
            {
                Id = 3,
                NasServerId = 3,
                NasServer = server3,
                Status = ScanStatus.Failed,
                StartTime = DateTime.Now.AddDays(-7),
                EndTime = DateTime.Now.AddDays(-7).AddMinutes(30),
                RootPath = "/dev/projects",
                TotalFiles = 0,
                ProcessedFiles = 0
            };

            var rootDir3 = new DirectoryItem
            {
                Id = 4,
                Name = "Projects",
                Path = "/dev/projects",
                CreatedDate = DateTime.Now.AddMonths(-8),
                ModifiedDate = DateTime.Now.AddDays(-7)
            };

            var subDir2 = new DirectoryItem
            {
                Id = 5,
                Name = "WebApp",
                Path = "/dev/projects/WebApp",
                ParentId = 4,
                Parent = rootDir3,
                CreatedDate = DateTime.Now.AddMonths(-6),
                ModifiedDate = DateTime.Now.AddDays(-3)
            };

            rootDir3.SubDirectories.Add(subDir2);

            var file4 = new FileItem
            {
                Id = 4,
                Name = "app.js",
                Path = "/dev/projects/WebApp/app.js",
                Size = 51200, // 50KB
                Extension = ".js",
                CreatedDate = DateTime.Now.AddMonths(-5),
                ModifiedDate = DateTime.Now.AddDays(-3),
                DirectoryId = 5,
                Directory = subDir2
            };

            var file5 = new FileItem
            {
                Id = 5,
                Name = "styles.css",
                Path = "/dev/projects/WebApp/styles.css",
                Size = 25600, // 25KB
                Extension = ".css",
                CreatedDate = DateTime.Now.AddMonths(-5),
                ModifiedDate = DateTime.Now.AddDays(-2),
                DirectoryId = 5,
                Directory = subDir2
            };

            subDir2.Files.AddRange(new[] { file4, file5 });

            var tag3 = new FileTag
            {
                Id = 3,
                Name = "Development",
                Description = "Razvojni projekti",
                Color = "#0000FF"
            };

            file4.Tags.Add(tag3);
            file5.Tags.Add(tag3);
            tag3.Files.AddRange(new[] { file4, file5 });

            var changeLog3 = new FileChangeLog
            {
                Id = 3,
                FileId = 4,
                File = file4,
                ChangeType = ChangeType.Created,
                Timestamp = DateTime.Now.AddDays(-3),
                OldValue = null,
                NewValue = "app.js",
                User = "dev_admin"
            };

            file4.ChangeLogs.Add(changeLog3);

            scanJob3.ScannedDirectories.Add(rootDir3);
            server3.ScanJobs.Add(scanJob3);

            servers.Add(server3);

            return servers;
        }

        static void ExecuteLinqQueries(List<NasServer> servers)
        {
            Console.WriteLine("=== LINQ Upiti nad NAS Indexer modelom ===\n");

            // 1. Dohvati sve aktivne servere
            var activeServers = servers.Where(s => s.IsActive);
            Console.WriteLine("1. Aktivni serveri:");
            foreach (var server in activeServers)
            {
                Console.WriteLine($"   - {server.Name} ({server.IpAddress})");
            }
            Console.WriteLine();

            // 2. Dohvati sve završene scan jobove s više od 100 datoteka
            var completedLargeScans = servers
                .SelectMany(s => s.ScanJobs)
                .Where(j => j.Status == ScanStatus.Completed && j.TotalFiles > 100);
            Console.WriteLine("2. Završeni scan jobovi s >100 datoteka:");
            foreach (var job in completedLargeScans)
            {
                Console.WriteLine($"   - Job {job.Id} na serveru {job.NasServer.Name}: {job.TotalFiles} datoteka");
            }
            Console.WriteLine();

            // 3. Grupiraj datoteke po ekstenziji
            var filesByExtension = servers
                .SelectMany(s => s.ScanJobs)
                .SelectMany(j => j.ScannedDirectories)
                .SelectMany(d => d.Files)
                .GroupBy(f => f.Extension)
                .Select(g => new { Extension = g.Key, Count = g.Count(), TotalSize = g.Sum(f => f.Size) });
            Console.WriteLine("3. Datoteke grupirane po ekstenziji:");
            foreach (var group in filesByExtension)
            {
                Console.WriteLine($"   - {group.Extension}: {group.Count} datoteka, ukupno {group.TotalSize / 1024 / 1024} MB");
            }
            Console.WriteLine();

            // 4. Pronađi najveće datoteke
            var largestFiles = servers
                .SelectMany(s => s.ScanJobs)
                .SelectMany(j => j.ScannedDirectories)
                .SelectMany(d => d.Files)
                .OrderByDescending(f => f.Size)
                .Take(3);
            Console.WriteLine("4. Najveće datoteke:");
            foreach (var file in largestFiles)
            {
                Console.WriteLine($"   - {file.Name}: {file.Size / 1024 / 1024} MB");
            }
            Console.WriteLine();

            // 5. Dohvati sve tagove i broj datoteka po tagu
            var tagsWithCounts = servers
                .SelectMany(s => s.ScanJobs)
                .SelectMany(j => j.ScannedDirectories)
                .SelectMany(d => d.Files)
                .SelectMany(f => f.Tags)
                .GroupBy(t => t.Name)
                .Select(g => new { TagName = g.Key, FileCount = g.Count(), Color = g.First().Color });
            Console.WriteLine("5. Tagovi s brojem datoteka:");
            foreach (var tag in tagsWithCounts)
            {
                Console.WriteLine($"   - {tag.TagName} ({tag.Color}): {tag.FileCount} datoteka");
            }
            Console.WriteLine();

            // 6. Pronađi server s najviše skeniranih datoteka
            var serverWithMostFiles = servers
                .Select(s => new
                {
                    Server = s,
                    TotalFiles = s.ScanJobs.Sum(j => j.TotalFiles)
                })
                .OrderByDescending(x => x.TotalFiles)
                .FirstOrDefault();
            if (serverWithMostFiles != null)
            {
                Console.WriteLine("6. Server s najviše datoteka:");
                Console.WriteLine($"   - {serverWithMostFiles.Server.Name}: {serverWithMostFiles.TotalFiles} datoteka");
            }
            Console.WriteLine();

            // 7. Dohvati sve promjene u zadnjih 30 dana
            var recentChanges = servers
                .SelectMany(s => s.ScanJobs)
                .SelectMany(j => j.ScannedDirectories)
                .SelectMany(d => d.Files)
                .SelectMany(f => f.ChangeLogs)
                .Where(c => c.Timestamp > DateTime.Now.AddDays(-30))
                .OrderByDescending(c => c.Timestamp);
            Console.WriteLine("7. Nedavne promjene datoteka:");
            foreach (var change in recentChanges.Take(5))
            {
                Console.WriteLine($"   - {change.ChangeType} na {change.File.Name} od {change.User} ({change.Timestamp})");
            }
            Console.WriteLine();

            // 8. Izračunaj statistiku po serverima
            var serverStats = servers.Select(s => new
            {
                ServerName = s.Name,
                TotalScans = s.ScanJobs.Count,
                CompletedScans = s.ScanJobs.Count(j => j.Status == ScanStatus.Completed),
                TotalFiles = s.ScanJobs.Sum(j => j.TotalFiles),
                AvgScanDuration = s.ScanJobs
                    .Where(j => j.EndTime.HasValue)
                    .Any() ? s.ScanJobs
                        .Where(j => j.EndTime.HasValue)
                        .Average(j => (j.EndTime.Value - j.StartTime).TotalMinutes) : 0
            });
            Console.WriteLine("8. Statistika po serverima:");
            foreach (var stat in serverStats)
            {
                Console.WriteLine($"   - {stat.ServerName}: {stat.TotalScans} skeniranja, {stat.CompletedScans} završenih, {stat.TotalFiles} datoteka, prosječno trajanje: {stat.AvgScanDuration:F1} min");
            }
        }
    }
}
