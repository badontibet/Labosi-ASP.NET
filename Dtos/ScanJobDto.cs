using NasIndexer.Model;

namespace NasIndexer.Dtos
{
    public class ScanJobDto
    {
        public int Id { get; set; }
        public int NasServerId { get; set; }
        public NasServerSummaryDto? NasServer { get; set; }
        public ScanStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string RootPath { get; set; } = string.Empty;
        public long TotalFiles { get; set; }
        public long ProcessedFiles { get; set; }
        public int ScannedDirectoryCount { get; set; }
    }

    public class NasServerSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
