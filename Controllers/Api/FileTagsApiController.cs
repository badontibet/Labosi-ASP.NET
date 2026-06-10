using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/tags")]
    public class FileTagsApiController : ControllerBase
    {
        private readonly INasRepository repository;

        public FileTagsApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<FileTagDto>> GetTags([FromQuery] string? query)
        {
            var tags = string.IsNullOrWhiteSpace(query)
                ? repository.GetAllTags()
                : repository.SearchTags(query);

            return Ok(tags.Select(ToDto));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<FileTagDto> GetTag(int id)
        {
            var tag = repository.GetTagById(id);

            if (tag == null)
            {
                return NotFound();
            }

            return Ok(ToDto(tag));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public ActionResult<FileTagDto> CreateTag(CreateFileTagDto dto)
        {
            if (!ValidateTagInput(dto.Name, nameof(dto.Name)))
            {
                return ValidationProblem(ModelState);
            }

            var tag = new FileTag
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Color = NormalizeColor(dto.Color)
            };

            repository.AddTag(tag);

            var created = repository.GetTagById(tag.Id) ?? tag;
            return CreatedAtAction(nameof(GetTag), new { id = created.Id }, ToDto(created));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}")]
        public ActionResult<FileTagDto> UpdateTag(int id, UpdateFileTagDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("Route id must match the DTO id.");
            }

            if (!ValidateTagInput(dto.Name, nameof(dto.Name)))
            {
                return ValidationProblem(ModelState);
            }

            var tag = new FileTag
            {
                Id = dto.Id,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Color = NormalizeColor(dto.Color)
            };

            if (!repository.UpdateTag(tag))
            {
                return NotFound();
            }

            var updated = repository.GetTagById(id) ?? tag;
            return Ok(ToDto(updated));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult DeleteTag(int id)
        {
            var tag = repository.GetTagById(id);

            if (tag == null)
            {
                return NotFound();
            }

            if (repository.FileTagHasFiles(id))
            {
                return Conflict(new
                {
                    message = "This tag cannot be deleted because it is assigned to one or more files."
                });
            }

            if (!repository.DeleteTag(id))
            {
                return Conflict(new
                {
                    message = "This tag could not be deleted. Refresh and check connected files."
                });
            }

            return NoContent();
        }

        private static FileTagDto ToDto(FileTag tag)
        {
            return new FileTagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Description = tag.Description,
                Color = tag.Color,
                FileCount = tag.Files.Count
            };
        }

        private bool ValidateTagInput(string? name, string fieldName)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return true;
            }

            ModelState.AddModelError(fieldName, "Tag name is required.");
            return false;
        }

        private static string NormalizeColor(string? color)
        {
            return string.IsNullOrWhiteSpace(color)
                ? string.Empty
                : color.Trim().ToUpperInvariant();
        }
    }
}
