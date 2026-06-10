using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/file-change-logs")]
    public class FileChangeLogsApiController : ControllerBase
    {
        private readonly INasRepository repository;

        public FileChangeLogsApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<FileChangeLogDto>> GetFileChangeLogs(
            [FromQuery] string? query,
            [FromQuery] ChangeType? changeType,
            [FromQuery] int? fileId)
        {
            var changeLogs = string.IsNullOrWhiteSpace(query) && !changeType.HasValue
                ? repository.GetAllFileChangeLogs()
                : repository.SearchFileChangeLogs(query, changeType);

            if (fileId.HasValue)
            {
                changeLogs = changeLogs
                    .Where(changeLog => changeLog.FileId == fileId.Value)
                    .ToList();
            }

            return Ok(changeLogs.Select(ToDto));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<FileChangeLogDto> GetFileChangeLog(int id)
        {
            var changeLog = repository.GetFileChangeLogById(id);

            if (changeLog == null)
            {
                return NotFound();
            }

            return Ok(ToDto(changeLog));
        }

        private static FileChangeLogDto ToDto(FileChangeLog changeLog)
        {
            return new FileChangeLogDto
            {
                Id = changeLog.Id,
                FileId = changeLog.FileId,
                File = changeLog.File == null
                    ? null
                    : new FileChangeLogFileSummaryDto
                    {
                        Id = changeLog.File.Id,
                        Name = changeLog.File.Name,
                        Path = changeLog.File.Path,
                        Extension = changeLog.File.Extension,
                        DirectoryId = changeLog.File.DirectoryId
                    },
                ChangeType = changeLog.ChangeType,
                Timestamp = changeLog.Timestamp,
                OldValue = changeLog.OldValue,
                NewValue = changeLog.NewValue,
                User = changeLog.User
            };
        }
    }
}
