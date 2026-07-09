using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NasIndexer.Data;
using NasIndexer.Model;

namespace NasIndexer.Services
{
    public static class FileChangeLogService
    {
        private const int ValueLimit = 240;
        private const int UserLimit = 120;

        public static void RecordFileCreated(NasIndexerDbContext context, FileItem file, ClaimsPrincipal user)
        {
            AddChangeLog(
                context,
                file.Id,
                ChangeType.Created,
                "Not indexed",
                $"Created file metadata: {SafeName(file.Name)}",
                ResolveAuthor(user));

            context.SaveChanges();
        }

        public static void RecordFileModified(NasIndexerDbContext context, FileItem before, FileItem after, ClaimsPrincipal user)
        {
            AddChangeLog(
                context,
                after.Id,
                ChangeType.Modified,
                FileSnapshot(before),
                FileSnapshot(after),
                ResolveAuthor(user));

            context.SaveChanges();
        }

        public static async Task RecordAttachmentUploadedAsync(
            NasIndexerDbContext context,
            int fileId,
            string originalFileName,
            long fileSize,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            AddChangeLog(
                context,
                fileId,
                ChangeType.Modified,
                "No attachment activity",
                $"Attachment uploaded: {SafeName(originalFileName)} ({fileSize} bytes)",
                ResolveAuthor(user));

            await context.SaveChangesAsync(cancellationToken);
        }

        public static async Task RecordAttachmentDeletedAsync(
            NasIndexerDbContext context,
            int fileId,
            string originalFileName,
            ClaimsPrincipal user,
            CancellationToken cancellationToken)
        {
            AddChangeLog(
                context,
                fileId,
                ChangeType.Modified,
                $"Attachment present: {SafeName(originalFileName)}",
                "Attachment deleted",
                ResolveAuthor(user));

            await context.SaveChangesAsync(cancellationToken);
        }

        public static string ResolveAuthor(ClaimsPrincipal user)
        {
            var author = user.FindFirstValue(ClaimTypes.Email)
                ?? user.Identity?.Name
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            return Normalize(string.IsNullOrWhiteSpace(author) ? "anonymous" : author, UserLimit);
        }

        public static async Task<FileItem?> GetFileSnapshotAsync(
            NasIndexerDbContext context,
            int fileId,
            CancellationToken cancellationToken = default)
        {
            return await context.FileItems
                .AsNoTracking()
                .Include(file => file.Tags)
                .FirstOrDefaultAsync(file => file.Id == fileId, cancellationToken);
        }

        private static void AddChangeLog(
            NasIndexerDbContext context,
            int fileId,
            ChangeType changeType,
            string oldValue,
            string newValue,
            string author)
        {
            context.FileChangeLogs.Add(new FileChangeLog
            {
                FileId = fileId,
                ChangeType = changeType,
                Timestamp = DateTime.UtcNow,
                OldValue = Normalize(oldValue, ValueLimit),
                NewValue = Normalize(newValue, ValueLimit),
                User = Normalize(author, UserLimit)
            });
        }

        private static string FileSnapshot(FileItem file)
        {
            var tagNames = file.Tags
                .OrderBy(tag => tag.Name)
                .Select(tag => tag.Name);

            return Normalize(
                $"{SafeName(file.Name)} | {SafeName(file.Extension)} | {file.Size} bytes | directory {file.DirectoryId} | tags: {string.Join(", ", tagNames)}",
                ValueLimit);
        }

        private static string SafeName(string value)
        {
            return Normalize(value.ReplaceLineEndings(" ").Trim(), 80);
        }

        private static string Normalize(string value, int limit)
        {
            var normalized = value.ReplaceLineEndings(" ").Trim();
            return normalized.Length <= limit
                ? normalized
                : normalized[..(limit - 3)] + "...";
        }
    }
}
