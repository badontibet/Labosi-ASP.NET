using NasIndexer.Model;

namespace NasIndexer.ViewModels
{
    public class DashboardViewModel
    {
        public List<NasServer> Servers { get; set; } = new List<NasServer>();
        public List<ScanJob> ScanJobs { get; set; } = new List<ScanJob>();
        public List<DirectoryItem> Directories { get; set; } = new List<DirectoryItem>();
        public List<FileItem> Files { get; set; } = new List<FileItem>();
        public List<FileTag> Tags { get; set; } = new List<FileTag>();
        public List<FileChangeLog> RecentChanges { get; set; } = new List<FileChangeLog>();
        public List<ScanJob> WarningScanJobs { get; set; } = new List<ScanJob>();

        public int TotalServers { get; set; }
        public int ActiveServers { get; set; }
        public int InactiveServers { get; set; }
        public int TotalScanJobs { get; set; }
        public int CompletedScanJobs { get; set; }
        public int RunningScanJobs { get; set; }
        public int FailedScanJobs { get; set; }
        public int TotalDirectories { get; set; }
        public int TotalFiles { get; set; }
        public int TotalTags { get; set; }
    }
}
