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
        List<DirectoryItem> SearchDirectories(string? query);
        DirectoryItem? GetDirectoryById(int id);
        DirectoryItem? GetDirectoryForEdit(int id);
        List<DirectoryItem> SearchDirectoriesForAutocomplete(string? query, int? excludeId = null, int take = 10);
        List<ScanJob> SearchScanJobsForAutocomplete(string? query, int take = 10);
        bool DirectoryHasChildrenOrFiles(int id);
        bool DirectoryParentWouldCreateCycle(int id, int? parentId);
        void AddDirectory(DirectoryItem directory);
        bool UpdateDirectory(DirectoryItem directory);
        bool DeleteDirectory(int id);
        List<FileItem> GetAllFiles();
        List<FileItem> SearchFiles(string? query);
        FileItem? GetFileById(int id);
        FileItem? GetFileForEdit(int id);
        bool FileItemHasChangeLogs(int id);
        void AddFile(FileItem file, IEnumerable<int> selectedTagIds);
        bool UpdateFile(FileItem file, IEnumerable<int> selectedTagIds);
        bool DeleteFile(int id);
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
