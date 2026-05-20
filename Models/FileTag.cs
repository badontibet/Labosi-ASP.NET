using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NasIndexer.Model
{
    public class FileTag
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
    }
}
