using NasIndexer.Model;

namespace NasIndexer.Dtos
{
    public class DirectoryItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? ScanJobId { get; set; }
        public DirectoryScanJobSummaryDto? ScanJob { get; set; }
        public int? ParentId { get; set; }
        public DirectorySummaryDto? Parent { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ChildDirectoryCount { get; set; }
        public int FileCount { get; set; }
    }

    public class DirectorySummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public class DirectoryScanJobSummaryDto
    {
        public int Id { get; set; }
        public ScanStatus Status { get; set; }
        public string RootPath { get; set; } = string.Empty;
    }
}
