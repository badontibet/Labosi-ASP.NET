namespace NasIndexer.Dtos
{
    public class FileItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int DirectoryId { get; set; }
        public FileDirectorySummaryDto? Directory { get; set; }
        public List<FileItemTagSummaryDto> Tags { get; set; } = new();
        public int ChangeLogCount { get; set; }
    }

    public class FileDirectorySummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public class FileItemTagSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}
