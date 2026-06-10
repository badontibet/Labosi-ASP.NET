using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/directories")]
    public class DirectoriesApiController : ControllerBase
    {
        private const int NameSoftLimit = 20;
        private const int PathSoftLimit = 50;

        private readonly INasRepository repository;

        public DirectoriesApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<DirectoryItemDto>> GetDirectories(
            [FromQuery] string? query,
            [FromQuery] int? scanJobId,
            [FromQuery] int? parentId)
        {
            var directories = string.IsNullOrWhiteSpace(query)
                ? repository.GetAllDirectories()
                : repository.SearchDirectories(query);

            if (scanJobId.HasValue)
            {
                directories = directories
                    .Where(directory => directory.ScanJobId == scanJobId.Value)
                    .ToList();
            }

            if (parentId.HasValue)
            {
                directories = directories
                    .Where(directory => directory.ParentId == parentId.Value)
                    .ToList();
            }

            return Ok(directories.Select(ToDto));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<DirectoryItemDto> GetDirectory(int id)
        {
            var directory = repository.GetDirectoryById(id);

            if (directory == null)
            {
                return NotFound();
            }

            return Ok(ToDto(directory));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public ActionResult<DirectoryItemDto> CreateDirectory(CreateDirectoryItemDto dto)
        {
            if (!ValidateDirectoryInput(0, dto.Name, dto.Path, dto.ScanJobId, dto.ParentId, dto.CreatedDate, dto.ModifiedDate))
            {
                return ValidationProblem(ModelState);
            }

            var directory = new DirectoryItem
            {
                Name = NormalizeDirectoryName(dto.Name),
                Path = NormalizeDirectoryPath(dto.Path),
                ScanJobId = dto.ScanJobId,
                ParentId = dto.ParentId,
                CreatedDate = dto.CreatedDate!.Value,
                ModifiedDate = dto.ModifiedDate!.Value
            };

            repository.AddDirectory(directory);

            var created = repository.GetDirectoryById(directory.Id) ?? directory;
            return CreatedAtAction(nameof(GetDirectory), new { id = created.Id }, ToDto(created));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}")]
        public ActionResult<DirectoryItemDto> UpdateDirectory(int id, UpdateDirectoryItemDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("Route id must match the DTO id.");
            }

            if (repository.GetDirectoryForEdit(id) == null)
            {
                return NotFound();
            }

            if (!ValidateDirectoryInput(dto.Id, dto.Name, dto.Path, dto.ScanJobId, dto.ParentId, dto.CreatedDate, dto.ModifiedDate))
            {
                return ValidationProblem(ModelState);
            }

            var directory = new DirectoryItem
            {
                Id = dto.Id,
                Name = NormalizeDirectoryName(dto.Name),
                Path = NormalizeDirectoryPath(dto.Path),
                ScanJobId = dto.ScanJobId,
                ParentId = dto.ParentId,
                CreatedDate = dto.CreatedDate!.Value,
                ModifiedDate = dto.ModifiedDate!.Value
            };

            if (!repository.UpdateDirectory(directory))
            {
                return NotFound();
            }

            var updated = repository.GetDirectoryById(id) ?? directory;
            return Ok(ToDto(updated));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult DeleteDirectory(int id)
        {
            var directory = repository.GetDirectoryById(id);

            if (directory == null)
            {
                return NotFound();
            }

            if (repository.DirectoryHasChildrenOrFiles(id))
            {
                return Conflict(new
                {
                    message = "This directory cannot be deleted because it contains child directories or files."
                });
            }

            if (!repository.DeleteDirectory(id))
            {
                return Conflict(new
                {
                    message = "This directory could not be deleted. Refresh and check connected records."
                });
            }

            return NoContent();
        }

        private static DirectoryItemDto ToDto(DirectoryItem directory)
        {
            return new DirectoryItemDto
            {
                Id = directory.Id,
                Name = directory.Name,
                Path = directory.Path,
                ScanJobId = directory.ScanJobId,
                ScanJob = directory.ScanJob == null
                    ? null
                    : new DirectoryScanJobSummaryDto
                    {
                        Id = directory.ScanJob.Id,
                        Status = directory.ScanJob.Status,
                        RootPath = directory.ScanJob.RootPath
                    },
                ParentId = directory.ParentId,
                Parent = directory.Parent == null
                    ? null
                    : new DirectorySummaryDto
                    {
                        Id = directory.Parent.Id,
                        Name = directory.Parent.Name,
                        Path = directory.Parent.Path
                    },
                CreatedDate = directory.CreatedDate,
                ModifiedDate = directory.ModifiedDate,
                ChildDirectoryCount = directory.SubDirectories.Count,
                FileCount = directory.Files.Count
            };
        }

        private bool ValidateDirectoryInput(
            int id,
            string? name,
            string? path,
            int? scanJobId,
            int? parentId,
            DateTime? createdDate,
            DateTime? modifiedDate)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.Name), "Directory name is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.Path), "Path is required.");
                isValid = false;
            }

            if (!createdDate.HasValue)
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.CreatedDate), "Created date is required.");
                isValid = false;
            }

            if (!modifiedDate.HasValue)
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.ModifiedDate), "Modified date is required.");
                isValid = false;
            }

            if (scanJobId.HasValue && repository.GetScanJobForEdit(scanJobId.Value) == null)
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.ScanJobId), "Choose an existing scan job.");
                isValid = false;
            }

            if (parentId.HasValue && repository.GetDirectoryForEdit(parentId.Value) == null)
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.ParentId), "Choose an existing parent directory.");
                isValid = false;
            }

            if (id > 0 && parentId == id)
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.ParentId), "A directory cannot be its own parent.");
                isValid = false;
            }

            if (repository.DirectoryParentWouldCreateCycle(id, parentId))
            {
                ModelState.AddModelError(nameof(CreateDirectoryItemDto.ParentId), "Parent directory cannot be one of this directory's descendants.");
                isValid = false;
            }

            return isValid;
        }

        private static string NormalizeDirectoryName(string name)
        {
            var trimmedName = name.Trim();

            return trimmedName.Length <= NameSoftLimit
                ? trimmedName
                : trimmedName[..(NameSoftLimit - 3)] + "...";
        }

        private static string NormalizeDirectoryPath(string path)
        {
            var trimmedPath = path.Trim();

            return trimmedPath.Length <= PathSoftLimit
                ? trimmedPath
                : trimmedPath[..PathSoftLimit];
        }
    }
}
