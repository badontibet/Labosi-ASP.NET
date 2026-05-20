using NasIndexer.Model;

namespace NasIndexer.Repositories
{
    public interface INasRepository
    {
        List<NasServer> GetAllNasServers();
        NasServer? GetNasServerById(int id);
        List<NasServer> SearchNasServers(string? query, int take = 10);
        List<ScanJob> GetAllScanJobs();
        List<ScanJob> SearchScanJobs(string? query);
        ScanJob? GetScanJobById(int id);
        ScanJob? GetScanJobForEdit(int id);
        bool ScanJobHasDirectories(int id);
        void AddScanJob(ScanJob scanJob);
        bool UpdateScanJob(ScanJob scanJob);
        bool DeleteScanJob(int id);
        List<DirectoryItem> GetAllDirectories();
        DirectoryItem? GetDirectoryById(int id);
        List<FileItem> GetAllFiles();
        FileItem? GetFileById(int id);
        List<FileTag> GetAllTags();
        List<FileTag> SearchTags(string? query);
        FileTag? GetTagById(int id);
        FileTag? GetTagForEdit(int id);
        bool FileTagHasFiles(int id);
        void AddTag(FileTag tag);
        bool UpdateTag(FileTag tag);
        bool DeleteTag(int id);
        List<SystemAdmin> GetAllAdmins();
        SystemAdmin? GetAdminById(int id);
    }
}
