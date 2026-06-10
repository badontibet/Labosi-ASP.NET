using Microsoft.AspNetCore.Http;

namespace NasIndexer.Services
{
    public class FileAttachmentStorageService
    {
        public const long MaxFileSize = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt",
            ".pdf",
            ".png",
            ".jpg",
            ".jpeg",
            ".docx",
            ".xlsx",
            ".zip"
        };

        private readonly string storageRoot;

        public FileAttachmentStorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            storageRoot = configuration["FileAttachmentStorage:RootPath"]
                ?? Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "file-attachments");
        }

        public async Task<FileAttachmentStorageResult> SaveAsync(IFormFile? file, int fileItemId, CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                return FileAttachmentStorageResult.Invalid("Upload a file.");
            }

            if (file.Length <= 0)
            {
                return FileAttachmentStorageResult.Invalid("Uploaded file must not be empty.");
            }

            if (file.Length > MaxFileSize)
            {
                return FileAttachmentStorageResult.Invalid("Uploaded file must be 10 MB or smaller.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                return FileAttachmentStorageResult.Invalid("File extension is not allowed.");
            }

            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var relativePath = Path.Combine(fileItemId.ToString(), storedFileName).Replace('\\', '/');
            var directory = Path.Combine(storageRoot, fileItemId.ToString());
            Directory.CreateDirectory(directory);

            var physicalPath = Path.Combine(directory, storedFileName);
            await using var stream = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);

            return FileAttachmentStorageResult.Success(new StoredFileAttachment(
                OriginalFileName: Path.GetFileName(file.FileName),
                StoredFileName: storedFileName,
                RelativePath: relativePath,
                ContentType: string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSize: file.Length,
                PhysicalPath: physicalPath));
        }

        public Task DeleteAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return Task.CompletedTask;
            }

            var fullStorageRoot = Path.GetFullPath(storageRoot);
            var physicalPath = Path.GetFullPath(Path.Combine(fullStorageRoot, relativePath));

            if (!physicalPath.StartsWith(fullStorageRoot, StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            return Task.CompletedTask;
        }

        public string GetPhysicalPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(storageRoot, relativePath));
        }
    }

    public record StoredFileAttachment(
        string OriginalFileName,
        string StoredFileName,
        string RelativePath,
        string ContentType,
        long FileSize,
        string PhysicalPath);

    public class FileAttachmentStorageResult
    {
        private FileAttachmentStorageResult(string? errorMessage, StoredFileAttachment? storedFile)
        {
            ErrorMessage = errorMessage;
            StoredFile = storedFile;
        }

        public string? ErrorMessage { get; }
        public StoredFileAttachment? StoredFile { get; }
        public bool IsValid => ErrorMessage == null && StoredFile != null;

        public static FileAttachmentStorageResult Invalid(string errorMessage)
        {
            return new FileAttachmentStorageResult(errorMessage, null);
        }

        public static FileAttachmentStorageResult Success(StoredFileAttachment storedFile)
        {
            return new FileAttachmentStorageResult(null, storedFile);
        }
    }
}
