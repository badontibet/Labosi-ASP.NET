using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NasIndexer.Model
{
    public class ScanJob
    {
        [Key]
        public int Id { get; set; }
        public int NasServerId { get; set; }

        [ForeignKey(nameof(NasServerId))]
        public virtual NasServer NasServer { get; set; } = null!;

        public ScanStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string RootPath { get; set; } = string.Empty;
        public long TotalFiles { get; set; }
        public long ProcessedFiles { get; set; }
        public virtual ICollection<DirectoryItem> ScannedDirectories { get; set; } = new List<DirectoryItem>();
    }
}
