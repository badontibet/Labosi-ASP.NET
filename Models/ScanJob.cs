using System;
using System.Collections.Generic;

namespace NasIndexer.Model
{
    public class ScanJob
    {
        public int Id { get; set; }
        public int NasServerId { get; set; }
        public NasServer NasServer { get; set; }
        public ScanStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string RootPath { get; set; }
        public long TotalFiles { get; set; }
        public long ProcessedFiles { get; set; }
        public List<DirectoryItem> ScannedDirectories { get; set; } = new List<DirectoryItem>();
    }
}