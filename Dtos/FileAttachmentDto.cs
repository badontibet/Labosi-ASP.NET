namespace NasIndexer.Dtos
{
    public class FileAttachmentDto
    {
        public int Id { get; set; }
        public int FileItemId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UploadedByUserId { get; set; }
    }
}
