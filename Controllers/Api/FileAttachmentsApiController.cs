using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NasIndexer.Data;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Services;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/files/{fileItemId:int}/attachments")]
    public class FileAttachmentsApiController : ControllerBase
    {
        private readonly NasIndexerDbContext context;
        private readonly FileAttachmentStorageService storageService;

        public FileAttachmentsApiController(
            NasIndexerDbContext context,
            FileAttachmentStorageService storageService)
        {
            this.context = context;
            this.storageService = storageService;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileAttachmentDto>>> GetAttachments(int fileItemId)
        {
            if (!await context.FileItems.AnyAsync(file => file.Id == fileItemId))
            {
                return NotFound();
            }

            var attachments = await context.FileAttachments
                .AsNoTracking()
                .Where(attachment => attachment.FileItemId == fileItemId)
                .OrderByDescending(attachment => attachment.CreatedAt)
                .Select(attachment => ToDto(attachment))
                .ToListAsync();

            return Ok(attachments);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [RequestSizeLimit(FileAttachmentStorageService.MaxFileSize)]
        public async Task<ActionResult<FileAttachmentDto>> UploadAttachment(int fileItemId, IFormFile? file)
        {
            if (!await context.FileItems.AnyAsync(fileItem => fileItem.Id == fileItemId))
            {
                return NotFound();
            }

            var storageResult = await storageService.SaveAsync(file, fileItemId, HttpContext.RequestAborted);
            if (!storageResult.IsValid || storageResult.StoredFile == null)
            {
                return BadRequest(new { message = storageResult.ErrorMessage });
            }

            var storedFile = storageResult.StoredFile;
            var attachment = new FileAttachment
            {
                FileItemId = fileItemId,
                OriginalFileName = storedFile.OriginalFileName,
                StoredFileName = storedFile.StoredFileName,
                RelativePath = storedFile.RelativePath,
                ContentType = storedFile.ContentType,
                FileSize = storedFile.FileSize,
                CreatedAt = DateTime.UtcNow,
                UploadedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            context.FileAttachments.Add(attachment);
            await context.SaveChangesAsync();
            await FileChangeLogService.RecordAttachmentUploadedAsync(
                context,
                fileItemId,
                attachment.OriginalFileName,
                attachment.FileSize,
                User,
                HttpContext.RequestAborted);

            return CreatedAtAction(
                nameof(GetAttachments),
                new { fileItemId },
                ToDto(attachment));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{attachmentId:int}")]
        public async Task<IActionResult> DeleteAttachment(int fileItemId, int attachmentId)
        {
            var attachment = await context.FileAttachments
                .FirstOrDefaultAsync(currentAttachment =>
                    currentAttachment.Id == attachmentId &&
                    currentAttachment.FileItemId == fileItemId);

            if (attachment == null)
            {
                return NotFound();
            }

            await storageService.DeleteAsync(attachment.RelativePath);
            var originalFileName = attachment.OriginalFileName;
            context.FileAttachments.Remove(attachment);
            await context.SaveChangesAsync();
            await FileChangeLogService.RecordAttachmentDeletedAsync(
                context,
                fileItemId,
                originalFileName,
                User,
                HttpContext.RequestAborted);

            return NoContent();
        }

        private static FileAttachmentDto ToDto(FileAttachment attachment)
        {
            return new FileAttachmentDto
            {
                Id = attachment.Id,
                FileItemId = attachment.FileItemId,
                OriginalFileName = attachment.OriginalFileName,
                StoredFileName = attachment.StoredFileName,
                RelativePath = attachment.RelativePath,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                CreatedAt = attachment.CreatedAt,
                UploadedByUserId = attachment.UploadedByUserId
            };
        }
    }
}
