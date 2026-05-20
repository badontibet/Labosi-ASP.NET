using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NasIndexer.Model
{
    public class DirectoryItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int? ScanJobId { get; set; }

        [ForeignKey(nameof(ScanJobId))]
        public virtual ScanJob? ScanJob { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public virtual DirectoryItem? Parent { get; set; }

        public virtual ICollection<DirectoryItem> SubDirectories { get; set; } = new List<DirectoryItem>();
        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
