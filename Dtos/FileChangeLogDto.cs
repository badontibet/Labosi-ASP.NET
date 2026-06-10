using NasIndexer.Model;

namespace NasIndexer.Dtos
{
    public class FileChangeLogDto
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public FileChangeLogFileSummaryDto? File { get; set; }
        public ChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; }
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }

    public class FileChangeLogFileSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public int DirectoryId { get; set; }
    }
}
