using Microsoft.EntityFrameworkCore;
using NasIndexer.Data;
using NasIndexer.Model;

namespace NasIndexer.Repositories
{
    public class EfNasRepository : INasRepository
    {
        private readonly NasIndexerDbContext context;

        public EfNasRepository(NasIndexerDbContext context)
        {
            this.context = context;
        }

        public List<NasServer> GetAllNasServers()
        {
            return context.NasServers
                .AsNoTracking()
                .Include(server => server.ScanJobs)
                .OrderBy(server => server.Name)
                .ToList();
        }

        public NasServer? GetNasServerById(int id)
        {
            return context.NasServers
                .AsNoTracking()
                .Include(server => server.ScanJobs)
                    .ThenInclude(scanJob => scanJob.ScannedDirectories)
                .FirstOrDefault(server => server.Id == id);
        }

        public List<NasServer> SearchNasServers(string? query, int take = 10)
        {
            var servers = context.NasServers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                servers = servers.Where(server =>
                    server.Name.Contains(normalizedQuery) ||
                    server.IpAddress.Contains(normalizedQuery));
            }

            return servers
                .OrderBy(server => server.Name)
                .Take(take)
                .ToList();
        }

        public List<ScanJob> GetAllScanJobs()
        {
            return context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .OrderByDescending(scanJob => scanJob.StartTime)
                .ToList();
        }

        public List<ScanJob> SearchScanJobs(string? query)
        {
            var scanJobs = context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                var statusMatches = Enum.GetValues<ScanStatus>()
                    .Where(status => status.ToString().Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                scanJobs = scanJobs.Where(scanJob =>
                    scanJob.NasServer.Name.Contains(normalizedQuery) ||
                    scanJob.RootPath.Contains(normalizedQuery) ||
                    statusMatches.Contains(scanJob.Status));
            }

            return scanJobs
                .OrderByDescending(scanJob => scanJob.StartTime)
                .ToList();
        }

        public ScanJob? GetScanJobById(int id)
        {
            return context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .Include(scanJob => scanJob.ScannedDirectories)
                    .ThenInclude(directory => directory.Files)
                .Include(scanJob => scanJob.ScannedDirectories)
                    .ThenInclude(directory => directory.SubDirectories)
                        .ThenInclude(childDirectory => childDirectory.Files)
                .FirstOrDefault(scanJob => scanJob.Id == id);
        }

        public ScanJob? GetScanJobForEdit(int id)
        {
            return context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .FirstOrDefault(scanJob => scanJob.Id == id);
        }

        public bool ScanJobHasDirectories(int id)
        {
            return context.DirectoryItems.Any(directory => directory.ScanJobId == id);
        }

        public void AddScanJob(ScanJob scanJob)
        {
            context.ScanJobs.Add(scanJob);
            context.SaveChanges();
        }

        public bool UpdateScanJob(ScanJob scanJob)
        {
            var existingScanJob = context.ScanJobs.FirstOrDefault(job => job.Id == scanJob.Id);

            if (existingScanJob == null)
            {
                return false;
            }

            existingScanJob.NasServerId = scanJob.NasServerId;
            existingScanJob.Status = scanJob.Status;
            existingScanJob.StartTime = scanJob.StartTime;
            existingScanJob.EndTime = scanJob.EndTime;
            existingScanJob.RootPath = scanJob.RootPath;
            existingScanJob.TotalFiles = scanJob.TotalFiles;
            existingScanJob.ProcessedFiles = scanJob.ProcessedFiles;

            context.SaveChanges();
            return true;
        }

        public bool DeleteScanJob(int id)
        {
            var scanJob = context.ScanJobs.FirstOrDefault(job => job.Id == id);

            if (scanJob == null || ScanJobHasDirectories(id))
            {
                return false;
            }

            context.ScanJobs.Remove(scanJob);
            context.SaveChanges();
            return true;
        }

        public List<DirectoryItem> GetAllDirectories()
        {
            return context.DirectoryItems
                .AsNoTracking()
                .Include(directory => directory.Parent)
                .Include(directory => directory.ScanJob)
                .Include(directory => directory.Files)
                .Include(directory => directory.SubDirectories)
                .OrderBy(directory => directory.Path)
                .ToList();
        }

        public List<DirectoryItem> SearchDirectories(string? query)
        {
            var directories = context.DirectoryItems
                .AsNoTracking()
                .Include(directory => directory.Parent)
                .Include(directory => directory.ScanJob)
                .Include(directory => directory.Files)
                .Include(directory => directory.SubDirectories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                directories = directories.Where(directory =>
                    directory.Name.Contains(normalizedQuery) ||
                    directory.Path.Contains(normalizedQuery) ||
                    (directory.Parent != null && (directory.Parent.Name.Contains(normalizedQuery) || directory.Parent.Path.Contains(normalizedQuery))) ||
                    (directory.ScanJob != null && directory.ScanJob.RootPath.Contains(normalizedQuery)));
            }

            return directories
                .OrderBy(directory => directory.Path)
                .ToList();
        }

        public DirectoryItem? GetDirectoryById(int id)
        {
            return context.DirectoryItems
                .AsNoTracking()
                .Include(directory => directory.Parent)
                .Include(directory => directory.ScanJob)
                .Include(directory => directory.SubDirectories)
                .Include(directory => directory.Files)
                .FirstOrDefault(directory => directory.Id == id);
        }

        public DirectoryItem? GetDirectoryForEdit(int id)
        {
            return context.DirectoryItems
                .AsNoTracking()
                .Include(directory => directory.Parent)
                .Include(directory => directory.ScanJob)
                    .ThenInclude(scanJob => scanJob!.NasServer)
                .FirstOrDefault(directory => directory.Id == id);
        }

        public List<DirectoryItem> SearchDirectoriesForAutocomplete(string? query, int? excludeId = null, int take = 10)
        {
            var directories = context.DirectoryItems.AsNoTracking().AsQueryable();

            if (excludeId.HasValue)
            {
                directories = directories.Where(directory => directory.Id != excludeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                directories = directories.Where(directory =>
                    directory.Name.Contains(normalizedQuery) ||
                    directory.Path.Contains(normalizedQuery));
            }

            return directories
                .OrderBy(directory => directory.Path)
                .Take(take)
                .ToList();
        }

        public List<ScanJob> SearchScanJobsForAutocomplete(string? query, int take = 10)
        {
            var scanJobs = context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                scanJobs = scanJobs.Where(scanJob =>
                    scanJob.RootPath.Contains(normalizedQuery) ||
                    scanJob.NasServer.Name.Contains(normalizedQuery));
            }

            return scanJobs
                .OrderByDescending(scanJob => scanJob.StartTime)
                .Take(take)
                .ToList();
        }

        public bool DirectoryHasChildrenOrFiles(int id)
        {
            return context.DirectoryItems.Any(directory => directory.ParentId == id) ||
                context.FileItems.Any(file => file.DirectoryId == id);
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

            var currentParentId = parentId;
            var visited = new HashSet<int>();

            while (currentParentId.HasValue)
            {
                if (!visited.Add(currentParentId.Value))
                {
                    return true;
                }

                if (currentParentId.Value == id)
                {
                    return true;
                }

                currentParentId = context.DirectoryItems
                    .AsNoTracking()
                    .Where(directory => directory.Id == currentParentId.Value)
                    .Select(directory => directory.ParentId)
                    .FirstOrDefault();
            }

            return false;
        }

        public void AddDirectory(DirectoryItem directory)
        {
            context.DirectoryItems.Add(directory);
            context.SaveChanges();
        }

        public bool UpdateDirectory(DirectoryItem directory)
        {
            var existingDirectory = context.DirectoryItems.FirstOrDefault(currentDirectory => currentDirectory.Id == directory.Id);

            if (existingDirectory == null)
            {
                return false;
            }

            existingDirectory.Name = directory.Name;
            existingDirectory.Path = directory.Path;
            existingDirectory.ScanJobId = directory.ScanJobId;
            existingDirectory.ParentId = directory.ParentId;
            existingDirectory.CreatedDate = directory.CreatedDate;
            existingDirectory.ModifiedDate = directory.ModifiedDate;

            context.SaveChanges();
            return true;
        }

        public bool DeleteDirectory(int id)
        {
            var directory = context.DirectoryItems.FirstOrDefault(currentDirectory => currentDirectory.Id == id);

            if (directory == null || DirectoryHasChildrenOrFiles(id))
            {
                return false;
            }

            context.DirectoryItems.Remove(directory);
            context.SaveChanges();
            return true;
        }

        public List<FileItem> GetAllFiles()
        {
            var files = context.FileItems
                .AsNoTracking()
                .Include(file => file.Directory)
                .Include(file => file.Tags)
                .Include(file => file.ChangeLogs)
                .OrderBy(file => file.Name)
                .ToList();

            LinkChangeLogsToFiles(files);

            return files;
        }

        public List<FileItem> SearchFiles(string? query)
        {
            var files = context.FileItems
                .AsNoTracking()
                .Include(file => file.Directory)
                .Include(file => file.Tags)
                .Include(file => file.ChangeLogs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                files = files.Where(file =>
                    file.Name.Contains(normalizedQuery) ||
                    file.Path.Contains(normalizedQuery) ||
                    file.Extension.Contains(normalizedQuery) ||
                    file.Directory.Name.Contains(normalizedQuery) ||
                    file.Directory.Path.Contains(normalizedQuery) ||
                    file.Tags.Any(tag => tag.Name.Contains(normalizedQuery)));
            }

            return files
                .OrderBy(file => file.Name)
                .ToList();
        }

        public FileItem? GetFileById(int id)
        {
            return context.FileItems
                .AsNoTracking()
                .Include(file => file.Directory)
                .Include(file => file.Tags)
                .Include(file => file.ChangeLogs)
                .FirstOrDefault(file => file.Id == id);
        }

        public FileItem? GetFileForEdit(int id)
        {
            return context.FileItems
                .AsNoTracking()
                .Include(file => file.Directory)
                .Include(file => file.Tags)
                .FirstOrDefault(file => file.Id == id);
        }

        public bool FileItemHasChangeLogs(int id)
        {
            return context.FileChangeLogs.Any(changeLog => changeLog.FileId == id);
        }

        public void AddFile(FileItem file, IEnumerable<int> selectedTagIds)
        {
            foreach (var tag in context.FileTags.Where(tag => selectedTagIds.Contains(tag.Id)))
            {
                file.Tags.Add(tag);
            }

            context.FileItems.Add(file);
            context.SaveChanges();
        }

        public bool UpdateFile(FileItem file, IEnumerable<int> selectedTagIds)
        {
            var existingFile = context.FileItems
                .Include(currentFile => currentFile.Tags)
                .FirstOrDefault(currentFile => currentFile.Id == file.Id);

            if (existingFile == null)
            {
                return false;
            }

            existingFile.Name = file.Name;
            existingFile.Path = file.Path;
            existingFile.Extension = file.Extension;
            existingFile.Size = file.Size;
            existingFile.DirectoryId = file.DirectoryId;
            existingFile.CreatedDate = file.CreatedDate;
            existingFile.ModifiedDate = file.ModifiedDate;

            existingFile.Tags.Clear();
            foreach (var tag in context.FileTags.Where(tag => selectedTagIds.Contains(tag.Id)))
            {
                existingFile.Tags.Add(tag);
            }

            context.SaveChanges();
            return true;
        }

        public bool DeleteFile(int id)
        {
            var file = context.FileItems.FirstOrDefault(currentFile => currentFile.Id == id);

            if (file == null || FileItemHasChangeLogs(id))
            {
                return false;
            }

            context.FileItems.Remove(file);
            context.SaveChanges();
            return true;
        }

        public List<FileTag> GetAllTags()
        {
            return context.FileTags
                .AsNoTracking()
                .Include(tag => tag.Files)
                .OrderBy(tag => tag.Name)
                .ToList();
        }

        public List<FileTag> SearchTags(string? query)
        {
            var tags = context.FileTags
                .AsNoTracking()
                .Include(tag => tag.Files)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalizedQuery = query.Trim();
                tags = tags.Where(tag =>
                    tag.Name.Contains(normalizedQuery) ||
                    tag.Description.Contains(normalizedQuery) ||
                    tag.Color.Contains(normalizedQuery));
            }

            return tags
                .OrderBy(tag => tag.Name)
                .ToList();
        }

        public FileTag? GetTagById(int id)
        {
            return context.FileTags
                .AsNoTracking()
                .Include(tag => tag.Files)
                .FirstOrDefault(tag => tag.Id == id);
        }

        public FileTag? GetTagForEdit(int id)
        {
            return context.FileTags
                .AsNoTracking()
                .FirstOrDefault(tag => tag.Id == id);
        }

        public bool FileTagHasFiles(int id)
        {
            return context.FileTags
                .Any(tag => tag.Id == id && tag.Files.Any());
        }

        public void AddTag(FileTag tag)
        {
            context.FileTags.Add(tag);
            context.SaveChanges();
        }

        public bool UpdateTag(FileTag tag)
        {
            var existingTag = context.FileTags.FirstOrDefault(currentTag => currentTag.Id == tag.Id);

            if (existingTag == null)
            {
                return false;
            }

            existingTag.Name = tag.Name;
            existingTag.Description = tag.Description;
            existingTag.Color = tag.Color;

            context.SaveChanges();
            return true;
        }

        public bool DeleteTag(int id)
        {
            var tag = context.FileTags.FirstOrDefault(currentTag => currentTag.Id == id);

            if (tag == null || FileTagHasFiles(id))
            {
                return false;
            }

            context.FileTags.Remove(tag);
            context.SaveChanges();
            return true;
        }

        public List<SystemAdmin> GetAllAdmins()
        {
            return context.SystemAdmins
                .AsNoTracking()
                .Include(admin => admin.ManagedServers)
                .OrderBy(admin => admin.Username)
                .ToList();
        }

        public SystemAdmin? GetAdminById(int id)
        {
            return context.SystemAdmins
                .AsNoTracking()
                .Include(admin => admin.ManagedServers)
                    .ThenInclude(server => server.ScanJobs)
                .FirstOrDefault(admin => admin.Id == id);
        }

        private static void LinkChangeLogsToFiles(IEnumerable<FileItem> files)
        {
            foreach (var file in files)
            {
                foreach (var changeLog in file.ChangeLogs)
                {
                    changeLog.File = file;
                }
            }
        }
    }
}
