using System;
using System.Collections.Generic;

namespace NasIndexer.Model
{
    public class FileItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int DirectoryId { get; set; }
        public DirectoryItem Directory { get; set; }
        public List<FileTag> Tags { get; set; } = new List<FileTag>();
        public List<FileChangeLog> ChangeLogs { get; set; } = new List<FileChangeLog>();
    }
}