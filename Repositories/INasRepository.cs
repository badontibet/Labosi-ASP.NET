using NasIndexer.Model;

namespace NasIndexer.Repositories
{
    public interface INasRepository
    {
        List<NasServer> GetAllNasServers();
        NasServer? GetNasServerById(int id);
        List<ScanJob> GetAllScanJobs();
        ScanJob? GetScanJobById(int id);
        List<DirectoryItem> GetAllDirectories();
        DirectoryItem? GetDirectoryById(int id);
        List<FileItem> GetAllFiles();
        FileItem? GetFileById(int id);
        List<FileTag> GetAllTags();
        FileTag? GetTagById(int id);
        List<SystemAdmin> GetAllAdmins();
        SystemAdmin? GetAdminById(int id);
    }
}
