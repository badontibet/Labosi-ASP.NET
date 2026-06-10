using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/nas-servers")]
    public class NasServersApiController : ControllerBase
    {
        private readonly INasRepository repository;

        public NasServersApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<NasServerDto>> GetNasServers([FromQuery] string? query)
        {
            var servers = string.IsNullOrWhiteSpace(query)
                ? repository.GetAllNasServers()
                : repository.SearchNasServers(query, int.MaxValue);

            return Ok(servers.Select(ToDto));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<NasServerDto> GetNasServer(int id)
        {
            var server = repository.GetNasServerById(id);

            if (server == null)
            {
                return NotFound();
            }

            return Ok(ToDto(server));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public ActionResult<NasServerDto> CreateNasServer(CreateNasServerDto dto)
        {
            if (!ValidateServerInput(dto.Name, dto.IpAddress, dto.LastScan))
            {
                return ValidationProblem(ModelState);
            }

            var server = new NasServer
            {
                Name = dto.Name.Trim(),
                IpAddress = dto.IpAddress.Trim(),
                Port = dto.Port,
                Username = dto.Username?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
                LastScan = dto.LastScan!.Value
            };

            repository.AddNasServer(server);

            var created = repository.GetNasServerById(server.Id) ?? server;
            return CreatedAtAction(nameof(GetNasServer), new { id = created.Id }, ToDto(created));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}")]
        public ActionResult<NasServerDto> UpdateNasServer(int id, UpdateNasServerDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("Route id must match the DTO id.");
            }

            if (!ValidateServerInput(dto.Name, dto.IpAddress, dto.LastScan))
            {
                return ValidationProblem(ModelState);
            }

            var server = new NasServer
            {
                Id = dto.Id,
                Name = dto.Name.Trim(),
                IpAddress = dto.IpAddress.Trim(),
                Port = dto.Port,
                Username = dto.Username?.Trim() ?? string.Empty,
                IsActive = dto.IsActive,
                LastScan = dto.LastScan!.Value
            };

            if (!repository.UpdateNasServer(server))
            {
                return NotFound();
            }

            var updated = repository.GetNasServerById(id) ?? server;
            return Ok(ToDto(updated));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult DeleteNasServer(int id)
        {
            var server = repository.GetNasServerById(id);

            if (server == null)
            {
                return NotFound();
            }

            if (repository.NasServerHasScanJobsOrManagedAdmins(id))
            {
                return Conflict(new
                {
                    message = "This NAS server cannot be deleted because it has scan jobs or assigned administrators."
                });
            }

            if (!repository.DeleteNasServer(id))
            {
                return Conflict(new
                {
                    message = "This NAS server could not be deleted. Refresh and check connected records."
                });
            }

            return NoContent();
        }

        private static NasServerDto ToDto(NasServer server)
        {
            return new NasServerDto
            {
                Id = server.Id,
                Name = server.Name,
                IpAddress = server.IpAddress,
                Port = server.Port,
                Username = server.Username,
                IsActive = server.IsActive,
                LastScan = server.LastScan,
                ScanJobCount = server.ScanJobs.Count,
                ManagedAdminCount = server.ManagedAdmins.Count
            };
        }

        private bool ValidateServerInput(string? name, string? ipAddress, DateTime? lastScan)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(CreateNasServerDto.Name), "Name is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                ModelState.AddModelError(nameof(CreateNasServerDto.IpAddress), "IP address is required.");
                isValid = false;
            }

            if (!lastScan.HasValue)
            {
                ModelState.AddModelError(nameof(CreateNasServerDto.LastScan), "Last scan is required.");
                isValid = false;
            }

            return isValid;
        }
    }
}
