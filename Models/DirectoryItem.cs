using System;
using System.Collections.Generic;

namespace NasIndexer.Model
{
    public class DirectoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int? ParentId { get; set; }
        public DirectoryItem Parent { get; set; }
        public List<DirectoryItem> SubDirectories { get; set; } = new List<DirectoryItem>();
        public List<FileItem> Files { get; set; } = new List<FileItem>();
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}