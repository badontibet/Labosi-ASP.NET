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

        public List<ScanJob> GetAllScanJobs()
        {
            return context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
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

        public FileTag? GetTagById(int id)
        {
            return context.FileTags
                .AsNoTracking()
                .Include(tag => tag.Files)
                .FirstOrDefault(tag => tag.Id == id);
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
