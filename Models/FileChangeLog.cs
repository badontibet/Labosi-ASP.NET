using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NasIndexer.Model
{
    public class FileChangeLog
    {
        [Key]
        public int Id { get; set; }
        public int FileId { get; set; }

        [ForeignKey(nameof(FileId))]
        public virtual FileItem File { get; set; } = null!;

        public ChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; }
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
    }
}
