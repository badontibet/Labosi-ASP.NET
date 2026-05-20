using NasIndexer.Model;

namespace NasIndexer.Repositories
{
    public class MockNasRepository : INasRepository
    {
        private static readonly List<NasServer> Servers = CreateSampleData();

        public List<NasServer> GetAllNasServers()
        {
            return Servers;
        }

        public NasServer? GetNasServerById(int id)
        {
            return Servers.FirstOrDefault(server => server.Id == id);
        }

        public List<NasServer> SearchNasServers(string? query, int take = 10)
        {
            var servers = Servers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                servers = servers.Where(server =>
                    server.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    server.IpAddress.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    server.Port.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    server.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (server.IsActive ? "Active" : "Inactive").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    server.ScanJobs.Any(scanJob => scanJob.RootPath.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            return servers
                .OrderBy(server => server.Name)
                .Take(take)
                .ToList();
        }

        public NasServer? GetNasServerForEdit(int id)
        {
            return GetNasServerById(id);
        }

        public bool NasServerHasScanJobsOrManagedAdmins(int id)
        {
            var server = GetNasServerById(id);
            return server?.ScanJobs.Any() == true || server?.ManagedAdmins.Any() == true;
        }

        public void AddNasServer(NasServer server)
        {
            server.Id = Servers.Select(existingServer => existingServer.Id).DefaultIfEmpty().Max() + 1;
            server.Password = string.Empty;
            Servers.Add(server);
        }

        public bool UpdateNasServer(NasServer server)
        {
            var existingServer = GetNasServerById(server.Id);

            if (existingServer == null)
            {
                return false;
            }

            existingServer.Name = server.Name;
            existingServer.IpAddress = server.IpAddress;
            existingServer.Port = server.Port;
            existingServer.Username = server.Username;
            existingServer.IsActive = server.IsActive;
            existingServer.LastScan = server.LastScan;
            return true;
        }

        public bool DeleteNasServer(int id)
        {
            var server = GetNasServerById(id);

            if (server == null || NasServerHasScanJobsOrManagedAdmins(id))
            {
                return false;
            }

            return Servers.Remove(server);
        }

        public List<ScanJob> GetAllScanJobs()
        {
            return Servers
                .SelectMany(server => server.ScanJobs)
                .ToList();
        }

        public List<ScanJob> SearchScanJobs(string? query)
        {
            var scanJobs = GetAllScanJobs().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                scanJobs = scanJobs.Where(job =>
                    job.NasServer.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    job.RootPath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    job.Status.ToString().Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return scanJobs
                .OrderByDescending(job => job.StartTime)
                .ToList();
        }

        public ScanJob? GetScanJobById(int id)
        {
            return GetAllScanJobs().FirstOrDefault(job => job.Id == id);
        }

        public ScanJob? GetScanJobForEdit(int id)
        {
            return GetScanJobById(id);
        }

        public bool ScanJobHasDirectories(int id)
        {
            return GetScanJobById(id)?.ScannedDirectories.Any() == true;
        }

        public void AddScanJob(ScanJob scanJob)
        {
            scanJob.Id = GetAllScanJobs().Select(job => job.Id).DefaultIfEmpty().Max() + 1;
            var server = GetNasServerById(scanJob.NasServerId);

            if (server == null)
            {
                return;
            }

            scanJob.NasServer = server;
            server.ScanJobs.Add(scanJob);
        }

        public bool UpdateScanJob(ScanJob scanJob)
        {
            var existingScanJob = GetScanJobById(scanJob.Id);
            var server = GetNasServerById(scanJob.NasServerId);

            if (existingScanJob == null || server == null)
            {
                return false;
            }

            existingScanJob.NasServer.ScanJobs.Remove(existingScanJob);
            existingScanJob.NasServerId = scanJob.NasServerId;
            existingScanJob.NasServer = server;
            existingScanJob.Status = scanJob.Status;
            existingScanJob.StartTime = scanJob.StartTime;
            existingScanJob.EndTime = scanJob.EndTime;
            existingScanJob.RootPath = scanJob.RootPath;
            existingScanJob.TotalFiles = scanJob.TotalFiles;
            existingScanJob.ProcessedFiles = scanJob.ProcessedFiles;
            server.ScanJobs.Add(existingScanJob);

            return true;
        }

        public bool DeleteScanJob(int id)
        {
            var scanJob = GetScanJobById(id);

            if (scanJob == null || scanJob.ScannedDirectories.Any())
            {
                return false;
            }

            return scanJob.NasServer.ScanJobs.Remove(scanJob);
        }

        public List<DirectoryItem> GetAllDirectories()
        {
            return GetAllScanJobs()
                .SelectMany(job => job.ScannedDirectories)
                .SelectMany(GetDirectoryAndChildren)
                .ToList();
        }

        public List<DirectoryItem> SearchDirectories(string? query)
        {
            var directories = GetAllDirectories().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                directories = directories.Where(directory =>
                    directory.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    directory.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (directory.Parent != null &&
                        (directory.Parent.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         directory.Parent.Path.Contains(query, StringComparison.OrdinalIgnoreCase))) ||
                    (directory.ScanJob != null && directory.ScanJob.RootPath.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            return directories
                .OrderBy(directory => directory.Path)
                .ToList();
        }

        public DirectoryItem? GetDirectoryById(int id)
        {
            return GetAllDirectories().FirstOrDefault(directory => directory.Id == id);
        }

        public DirectoryItem? GetDirectoryForEdit(int id)
        {
            return GetDirectoryById(id);
        }

        public List<DirectoryItem> SearchDirectoriesForAutocomplete(string? query, int? excludeId = null, int take = 10)
        {
            var directories = GetAllDirectories().AsEnumerable();

            if (excludeId.HasValue)
            {
                directories = directories.Where(directory => directory.Id != excludeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                directories = directories.Where(directory =>
                    directory.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    directory.Path.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return directories
                .OrderBy(directory => directory.Path)
                .Take(take)
                .ToList();
        }

        public List<ScanJob> SearchScanJobsForAutocomplete(string? query, int take = 10)
        {
            var scanJobs = GetAllScanJobs().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                scanJobs = scanJobs.Where(scanJob =>
                    scanJob.RootPath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    scanJob.NasServer.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return scanJobs
                .OrderByDescending(scanJob => scanJob.StartTime)
                .Take(take)
                .ToList();
        }

        public bool DirectoryHasChildrenOrFiles(int id)
        {
            var directory = GetDirectoryById(id);
            return directory?.SubDirectories.Any() == true || directory?.Files.Any() == true;
        }

        public bool DirectoryParentWouldCreateCycle(int id, int? parentId)
        {
            if (id <= 0 || !parentId.HasValue)
            {
                return false;
            }

            if (id == parentId.Value)
            {
                return true;
            }

            var currentParent = GetDirectoryById(parentId.Value);
            var visited = new HashSet<int>();

            while (currentParent != null)
            {
                if (!visited.Add(currentParent.Id))
                {
                    return true;
                }

                if (currentParent.Id == id)
                {
                    return true;
                }

                currentParent = currentParent.ParentId.HasValue ? GetDirectoryById(currentParent.ParentId.Value) : null;
            }

            return false;
        }

        public void AddDirectory(DirectoryItem directory)
        {
            directory.Id = GetAllDirectories().Select(existingDirectory => existingDirectory.Id).DefaultIfEmpty().Max() + 1;
            directory.Parent = directory.ParentId.HasValue ? GetDirectoryById(directory.ParentId.Value) : null;
            directory.ScanJob = directory.ScanJobId.HasValue ? GetScanJobById(directory.ScanJobId.Value) : null;
            directory.Parent?.SubDirectories.Add(directory);
            directory.ScanJob?.ScannedDirectories.Add(directory);
        }

        public bool UpdateDirectory(DirectoryItem directory)
        {
            var existingDirectory = GetDirectoryById(directory.Id);

            if (existingDirectory == null)
            {
                return false;
            }

            existingDirectory.Name = directory.Name;
            existingDirectory.Path = directory.Path;
            existingDirectory.ScanJobId = directory.ScanJobId;
            existingDirectory.ScanJob = directory.ScanJobId.HasValue ? GetScanJobById(directory.ScanJobId.Value) : null;
            existingDirectory.ParentId = directory.ParentId;
            existingDirectory.Parent = directory.ParentId.HasValue ? GetDirectoryById(directory.ParentId.Value) : null;
            existingDirectory.CreatedDate = directory.CreatedDate;
            existingDirectory.ModifiedDate = directory.ModifiedDate;
            return true;
        }

        public bool DeleteDirectory(int id)
        {
            var directory = GetDirectoryById(id);

            if (directory == null || DirectoryHasChildrenOrFiles(id))
            {
                return false;
            }

            directory.Parent?.SubDirectories.Remove(directory);
            directory.ScanJob?.ScannedDirectories.Remove(directory);
            return true;
        }

        public List<FileItem> GetAllFiles()
        {
            return GetAllDirectories()
                .SelectMany(directory => directory.Files)
                .ToList();
        }

        public List<FileItem> SearchFiles(string? query)
        {
            var files = GetAllFiles().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                files = files.Where(file =>
                    file.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.Extension.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.Directory.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.Directory.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.Tags.Any(tag => tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

            return files.OrderBy(file => file.Name).ToList();
        }

        public FileItem? GetFileById(int id)
        {
            return GetAllFiles().FirstOrDefault(file => file.Id == id);
        }

        public FileItem? GetFileForEdit(int id)
        {
            return GetFileById(id);
        }

        public bool FileItemHasChangeLogs(int id)
        {
            return GetFileById(id)?.ChangeLogs.Any() == true;
        }

        public void AddFile(FileItem file, IEnumerable<int> selectedTagIds)
        {
            file.Id = GetAllFiles().Select(existingFile => existingFile.Id).DefaultIfEmpty().Max() + 1;
            var directory = GetDirectoryById(file.DirectoryId);

            if (directory == null)
            {
                return;
            }

            file.Directory = directory;
            directory.Files.Add(file);

            foreach (var tag in GetAllTags().Where(tag => selectedTagIds.Contains(tag.Id)))
            {
                file.Tags.Add(tag);
                tag.Files.Add(file);
            }
        }

        public bool UpdateFile(FileItem file, IEnumerable<int> selectedTagIds)
        {
            var existingFile = GetFileById(file.Id);
            var directory = GetDirectoryById(file.DirectoryId);

            if (existingFile == null || directory == null)
            {
                return false;
            }

            existingFile.Name = file.Name;
            existingFile.Path = file.Path;
            existingFile.Extension = file.Extension;
            existingFile.Size = file.Size;
            existingFile.DirectoryId = file.DirectoryId;
            existingFile.Directory = directory;
            existingFile.CreatedDate = file.CreatedDate;
            existingFile.ModifiedDate = file.ModifiedDate;
            existingFile.Tags.Clear();

            foreach (var tag in GetAllTags().Where(tag => selectedTagIds.Contains(tag.Id)))
            {
                existingFile.Tags.Add(tag);
            }

            return true;
        }

        public bool DeleteFile(int id)
        {
            var file = GetFileById(id);

            if (file == null || file.ChangeLogs.Any())
            {
                return false;
            }

            file.Directory.Files.Remove(file);
            return true;
        }

        public List<FileChangeLog> GetAllFileChangeLogs()
        {
            return GetAllFiles()
                .SelectMany(file => file.ChangeLogs)
                .OrderByDescending(changeLog => changeLog.Timestamp)
                .ToList();
        }

        public List<FileChangeLog> SearchFileChangeLogs(string? query, ChangeType? changeType)
        {
            var changeLogs = GetAllFileChangeLogs().AsEnumerable();

            if (changeType.HasValue)
            {
                changeLogs = changeLogs.Where(changeLog => changeLog.ChangeType == changeType.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                changeLogs = changeLogs.Where(changeLog =>
                    changeLog.File.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.File.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.File.Directory.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.File.Directory.Path.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.ChangeType.ToString().Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.User.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.OldValue.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    changeLog.NewValue.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return changeLogs
                .OrderByDescending(changeLog => changeLog.Timestamp)
                .ToList();
        }

        public FileChangeLog? GetFileChangeLogById(int id)
        {
            return GetAllFileChangeLogs().FirstOrDefault(changeLog => changeLog.Id == id);
        }

        public List<FileTag> GetAllTags()
        {
            return GetAllFiles()
                .SelectMany(file => file.Tags)
                .GroupBy(tag => tag.Id)
                .Select(group => group.First())
                .ToList();
        }

        public List<FileTag> SearchTags(string? query)
        {
            var tags = GetAllTags().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                tags = tags.Where(tag =>
                    tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    tag.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    tag.Color.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return tags
                .OrderBy(tag => tag.Name)
                .ToList();
        }

        public FileTag? GetTagById(int id)
        {
            return GetAllTags().FirstOrDefault(tag => tag.Id == id);
        }

        public FileTag? GetTagForEdit(int id)
        {
            return GetTagById(id);
        }

        public bool FileTagHasFiles(int id)
        {
            return GetTagById(id)?.Files.Any() == true;
        }

        public void AddTag(FileTag tag)
        {
            tag.Id = GetAllTags().Select(existingTag => existingTag.Id).DefaultIfEmpty().Max() + 1;
        }

        public bool UpdateTag(FileTag tag)
        {
            var existingTag = GetTagById(tag.Id);

            if (existingTag == null)
            {
                return false;
            }

            existingTag.Name = tag.Name;
            existingTag.Description = tag.Description;
            existingTag.Color = tag.Color;
            return true;
        }

        public bool DeleteTag(int id)
        {
            var tag = GetTagById(id);

            if (tag == null || tag.Files.Any())
            {
                return false;
            }

            return true;
        }

        public List<SystemAdmin> GetAllAdmins()
        {
            var admins = new List<SystemAdmin>
            {
                new SystemAdmin
                {
                    Id = 1,
                    Username = "admin",
                    Password = "hashed_password",
                    Email = "admin@company.com",
                    Role = "SuperAdmin",
                    CreatedDate = DateTime.Now.AddYears(-1),
                    LastLogin = DateTime.Now.AddHours(-1),
                    ManagedServers = { Servers[0] }
                },
                new SystemAdmin
                {
                    Id = 2,
                    Username = "backup_admin",
                    Password = "hashed_backup_password",
                    Email = "backup.admin@company.com",
                    Role = "BackupOperator",
                    CreatedDate = DateTime.Now.AddMonths(-10),
                    LastLogin = DateTime.Now.AddHours(-5),
                    ManagedServers = { Servers[1] }
                },
                new SystemAdmin
                {
                    Id = 3,
                    Username = "dev_admin",
                    Password = "hashed_dev_password",
                    Email = "dev.admin@company.com",
                    Role = "Developer",
                    CreatedDate = DateTime.Now.AddMonths(-8),
                    LastLogin = DateTime.Now.AddDays(-2),
                    ManagedServers = { Servers[2] }
                }
            };

            return admins;
        }

        public SystemAdmin? GetAdminById(int id)
        {
            return GetAllAdmins().FirstOrDefault(admin => admin.Id == id);
        }

        private static IEnumerable<DirectoryItem> GetDirectoryAndChildren(DirectoryItem directory)
        {
            yield return directory;

            foreach (var child in directory.SubDirectories)
            {
                foreach (var nestedChild in GetDirectoryAndChildren(child))
                {
                    yield return nestedChild;
                }
            }
        }

        private static List<NasServer> CreateSampleData()
        {
            var servers = new List<NasServer>();

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

            var file1 = new FileItem
            {
                Id = 1,
                Name = "Q1_Report.pdf",
                Path = "/shares/documents/reports/Q1_Report.pdf",
                Size = 2048000,
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
                Size = 1536000,
                Extension = ".pdf",
                CreatedDate = DateTime.Now.AddMonths(-2),
                ModifiedDate = DateTime.Now.AddDays(-10),
                DirectoryId = 2,
                Directory = subDir1
            };

            subDir1.Files.Add(file1);
            subDir1.Files.Add(file2);

            var tag1 = new FileTag
            {
                Id = 1,
                Name = "Vazno",
                Description = "Vazni dokumenti",
                Color = "#FF0000"
            };

            file1.Tags.Add(tag1);
            tag1.Files.Add(file1);

            var changeLog1 = new FileChangeLog
            {
                Id = 1,
                FileId = 1,
                File = file1,
                ChangeType = ChangeType.Created,
                Timestamp = DateTime.Now.AddMonths(-3),
                OldValue = string.Empty,
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
            servers.Add(server1);

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
                Size = 1073741824,
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
                Size = 51200,
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
                Size = 25600,
                Extension = ".css",
                CreatedDate = DateTime.Now.AddMonths(-5),
                ModifiedDate = DateTime.Now.AddDays(-2),
                DirectoryId = 5,
                Directory = subDir2
            };

            subDir2.Files.Add(file4);
            subDir2.Files.Add(file5);

            var tag3 = new FileTag
            {
                Id = 3,
                Name = "Development",
                Description = "Razvojni projekti",
                Color = "#0000FF"
            };

            file4.Tags.Add(tag3);
            file5.Tags.Add(tag3);
            tag3.Files.Add(file4);
            tag3.Files.Add(file5);

            var changeLog3 = new FileChangeLog
            {
                Id = 3,
                FileId = 4,
                File = file4,
                ChangeType = ChangeType.Created,
                Timestamp = DateTime.Now.AddDays(-3),
                OldValue = string.Empty,
                NewValue = "app.js",
                User = "dev_admin"
            };

            file4.ChangeLogs.Add(changeLog3);

            scanJob3.ScannedDirectories.Add(rootDir3);
            server3.ScanJobs.Add(scanJob3);
            servers.Add(server3);

            return servers;
        }
    }
}
