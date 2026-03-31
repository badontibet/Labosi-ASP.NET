using System;
using System.Collections.Generic;

namespace NasIndexer.Model
{
    public class FileTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public List<FileItem> Files { get; set; } = new List<FileItem>();
    }
}