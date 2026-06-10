using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/files")]
    public class FileItemsApiController : ControllerBase
    {
        private readonly INasRepository repository;

        public FileItemsApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<FileItemDto>> GetFiles(
            [FromQuery] string? query,
            [FromQuery] int? directoryId,
            [FromQuery] int? tagId,
            [FromQuery] string? extension)
        {
            var files = string.IsNullOrWhiteSpace(query)
                ? repository.GetAllFiles()
                : repository.SearchFiles(query);

            if (directoryId.HasValue)
            {
                files = files
                    .Where(file => file.DirectoryId == directoryId.Value)
                    .ToList();
            }

            if (tagId.HasValue)
            {
                files = files
                    .Where(file => file.Tags.Any(tag => tag.Id == tagId.Value))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(extension))
            {
                var normalizedExtension = extension.Trim();
                files = files
                    .Where(file => string.Equals(file.Extension, normalizedExtension, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(files.Select(ToDto));
        }

        [HttpGet("{id:int}")]
        public ActionResult<FileItemDto> GetFile(int id)
        {
            var file = repository.GetFileById(id);

            if (file == null)
            {
                return NotFound();
            }

            return Ok(ToDto(file));
        }

        [HttpPost]
        public ActionResult<FileItemDto> CreateFile(CreateFileItemDto dto)
        {
            if (!ValidateFileInput(dto.Name, dto.Path, dto.Extension, dto.DirectoryId, dto.CreatedDate, dto.ModifiedDate, dto.TagIds))
            {
                return ValidationProblem(ModelState);
            }

            var selectedTagIds = dto.TagIds.Distinct().ToList();
            var file = new FileItem
            {
                Name = dto.Name.Trim(),
                Path = dto.Path.Trim(),
                Extension = dto.Extension.Trim(),
                Size = dto.Size,
                DirectoryId = dto.DirectoryId!.Value,
                CreatedDate = dto.CreatedDate!.Value,
                ModifiedDate = dto.ModifiedDate!.Value
            };

            repository.AddFile(file, selectedTagIds);

            var created = repository.GetFileById(file.Id) ?? file;
            return CreatedAtAction(nameof(GetFile), new { id = created.Id }, ToDto(created));
        }

        [HttpPut("{id:int}")]
        public ActionResult<FileItemDto> UpdateFile(int id, UpdateFileItemDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("Route id must match the DTO id.");
            }

            if (repository.GetFileForEdit(id) == null)
            {
                return NotFound();
            }

            if (!ValidateFileInput(dto.Name, dto.Path, dto.Extension, dto.DirectoryId, dto.CreatedDate, dto.ModifiedDate, dto.TagIds))
            {
                return ValidationProblem(ModelState);
            }

            var selectedTagIds = dto.TagIds.Distinct().ToList();
            var file = new FileItem
            {
                Id = dto.Id,
                Name = dto.Name.Trim(),
                Path = dto.Path.Trim(),
                Extension = dto.Extension.Trim(),
                Size = dto.Size,
                DirectoryId = dto.DirectoryId!.Value,
                CreatedDate = dto.CreatedDate!.Value,
                ModifiedDate = dto.ModifiedDate!.Value
            };

            if (!repository.UpdateFile(file, selectedTagIds))
            {
                return NotFound();
            }

            var updated = repository.GetFileById(id) ?? file;
            return Ok(ToDto(updated));
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteFile(int id)
        {
            var file = repository.GetFileById(id);

            if (file == null)
            {
                return NotFound();
            }

            if (repository.FileItemHasChangeLogs(id))
            {
                return Conflict(new
                {
                    message = "This file cannot be deleted because it has change log entries."
                });
            }

            if (!repository.DeleteFile(id))
            {
                return Conflict(new
                {
                    message = "This file could not be deleted. Refresh and check change logs."
                });
            }

            return NoContent();
        }

        private static FileItemDto ToDto(FileItem file)
        {
            return new FileItemDto
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                Size = file.Size,
                Extension = file.Extension,
                CreatedDate = file.CreatedDate,
                ModifiedDate = file.ModifiedDate,
                DirectoryId = file.DirectoryId,
                Directory = file.Directory == null
                    ? null
                    : new FileDirectorySummaryDto
                    {
                        Id = file.Directory.Id,
                        Name = file.Directory.Name,
                        Path = file.Directory.Path
                    },
                Tags = file.Tags
                    .OrderBy(tag => tag.Name)
                    .Select(tag => new FileItemTagSummaryDto
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        Color = tag.Color
                    })
                    .ToList(),
                ChangeLogCount = file.ChangeLogs.Count
            };
        }

        private bool ValidateFileInput(
            string? name,
            string? path,
            string? extension,
            int? directoryId,
            DateTime? createdDate,
            DateTime? modifiedDate,
            IEnumerable<int> tagIds)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.Name), "File name is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.Path), "Path is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.Extension), "Extension is required.");
                isValid = false;
            }

            if (!createdDate.HasValue)
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.CreatedDate), "Created date is required.");
                isValid = false;
            }

            if (!modifiedDate.HasValue)
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.ModifiedDate), "Modified date is required.");
                isValid = false;
            }

            if (!directoryId.HasValue)
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.DirectoryId), "Directory is required.");
                isValid = false;
            }
            else if (repository.GetDirectoryForEdit(directoryId.Value) == null)
            {
                ModelState.AddModelError(nameof(CreateFileItemDto.DirectoryId), "Choose an existing directory.");
                isValid = false;
            }

            var selectedTagIds = tagIds.Distinct().ToList();
            if (selectedTagIds.Count > 0)
            {
                var validTagIds = repository.GetAllTags().Select(tag => tag.Id).ToHashSet();
                var invalidTagIds = selectedTagIds.Where(tagId => !validTagIds.Contains(tagId)).ToList();
                if (invalidTagIds.Any())
                {
                    ModelState.AddModelError(nameof(CreateFileItemDto.TagIds), "One or more selected tags do not exist.");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
