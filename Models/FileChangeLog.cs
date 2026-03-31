using System;

namespace NasIndexer.Model
{
    public class FileChangeLog
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public FileItem File { get; set; }
        public ChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string User { get; set; }
    }
}