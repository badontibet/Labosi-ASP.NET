using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NasIndexer.Model
{
    public class FileItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int DirectoryId { get; set; }

        [ForeignKey(nameof(DirectoryId))]
        public virtual DirectoryItem Directory { get; set; } = null!;

        public virtual ICollection<FileTag> Tags { get; set; } = new List<FileTag>();
        public virtual ICollection<FileChangeLog> ChangeLogs { get; set; } = new List<FileChangeLog>();
    }
}
