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
                .Include(directory => directory.Files)
                .Include(directory => directory.SubDirectories)
                .OrderBy(directory => directory.Path)
                .ToList();
        }

        public DirectoryItem? GetDirectoryById(int id)
        {
            return context.DirectoryItems
                .AsNoTracking()
                .Include(directory => directory.Parent)
                .Include(directory => directory.SubDirectories)
                .Include(directory => directory.Files)
                .FirstOrDefault(directory => directory.Id == id);
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

        public FileItem? GetFileById(int id)
        {
            return context.FileItems
                .AsNoTracking()
                .Include(file => file.Directory)
                .Include(file => file.Tags)
                .Include(file => file.ChangeLogs)
                .FirstOrDefault(file => file.Id == id);
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
