using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NasIndexer.Model
{
    public class FileAttachment
    {
        [Key]
        public int Id { get; set; }

        public int FileItemId { get; set; }

        [ForeignKey(nameof(FileItemId))]
        public virtual FileItem FileItem { get; set; } = null!;

        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UploadedByUserId { get; set; }
    }
}
