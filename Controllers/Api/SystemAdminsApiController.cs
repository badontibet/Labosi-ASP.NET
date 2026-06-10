using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/system-admins")]
    public class SystemAdminsApiController : ControllerBase
    {
        private const string ApiPasswordPlaceholder = "NotUsedByApi";

        private readonly INasRepository repository;

        public SystemAdminsApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<SystemAdminDto>> GetSystemAdmins(
            [FromQuery] string? query,
            [FromQuery] int? nasServerId)
        {
            var admins = string.IsNullOrWhiteSpace(query)
                ? repository.GetAllAdmins()
                : repository.SearchAdmins(query);

            if (nasServerId.HasValue)
            {
                admins = admins
                    .Where(admin => admin.ManagedServers.Any(server => server.Id == nasServerId.Value))
                    .ToList();
            }

            return Ok(admins.Select(ToDto));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<SystemAdminDto> GetSystemAdmin(int id)
        {
            var admin = repository.GetAdminById(id);

            if (admin == null)
            {
                return NotFound();
            }

            return Ok(ToDto(admin));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public ActionResult<SystemAdminDto> CreateSystemAdmin(CreateSystemAdminDto dto)
        {
            if (!ValidateAdminInput(dto.Username, dto.Email, dto.Role, dto.CreatedDate, dto.LastLogin, dto.ManagedNasServerIds))
            {
                return ValidationProblem(ModelState);
            }

            var selectedServerIds = (dto.ManagedNasServerIds ?? new List<int>()).Distinct().ToList();
            var admin = new SystemAdmin
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                Role = dto.Role.Trim(),
                CreatedDate = dto.CreatedDate!.Value,
                LastLogin = dto.LastLogin!.Value,
                Password = ApiPasswordPlaceholder
            };

            repository.AddAdmin(admin, selectedServerIds);

            var created = repository.GetAdminById(admin.Id) ?? admin;
            return CreatedAtAction(nameof(GetSystemAdmin), new { id = created.Id }, ToDto(created));
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}")]
        public ActionResult<SystemAdminDto> UpdateSystemAdmin(int id, UpdateSystemAdminDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("Route id must match the DTO id.");
            }

            if (repository.GetAdminForEdit(id) == null)
            {
                return NotFound();
            }

            if (!ValidateAdminInput(dto.Username, dto.Email, dto.Role, dto.CreatedDate, dto.LastLogin, dto.ManagedNasServerIds))
            {
                return ValidationProblem(ModelState);
            }

            var selectedServerIds = (dto.ManagedNasServerIds ?? new List<int>()).Distinct().ToList();
            var admin = new SystemAdmin
            {
                Id = dto.Id,
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                Role = dto.Role.Trim(),
                CreatedDate = dto.CreatedDate!.Value,
                LastLogin = dto.LastLogin!.Value
            };

            if (!repository.UpdateAdmin(admin, selectedServerIds))
            {
                return NotFound();
            }

            var updated = repository.GetAdminById(id) ?? admin;
            return Ok(ToDto(updated));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public IActionResult DeleteSystemAdmin(int id)
        {
            var admin = repository.GetAdminById(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (repository.SystemAdminHasManagedServers(id))
            {
                return Conflict(new
                {
                    message = "This system admin cannot be deleted because managed NAS servers are assigned."
                });
            }

            if (!repository.DeleteAdmin(id))
            {
                return Conflict(new
                {
                    message = "This system admin could not be deleted. Refresh and check managed servers."
                });
            }

            return NoContent();
        }

        private static SystemAdminDto ToDto(SystemAdmin admin)
        {
            var managedServers = admin.ManagedServers
                .OrderBy(server => server.Name)
                .Select(server => new NasServerSummaryDto
                {
                    Id = server.Id,
                    Name = server.Name,
                    IpAddress = server.IpAddress,
                    Port = server.Port
                })
                .ToList();

            return new SystemAdminDto
            {
                Id = admin.Id,
                Username = admin.Username,
                Email = admin.Email,
                Role = admin.Role,
                CreatedDate = admin.CreatedDate,
                LastLogin = admin.LastLogin,
                ManagedNasServerIds = managedServers.Select(server => server.Id).ToList(),
                ManagedNasServers = managedServers
            };
        }

        private bool ValidateAdminInput(
            string? username,
            string? email,
            string? role,
            DateTime? createdDate,
            DateTime? lastLogin,
            IEnumerable<int>? managedNasServerIds)
        {
            var isValid = true;

            if (string.IsNullOrWhiteSpace(username))
            {
                ModelState.AddModelError(nameof(CreateSystemAdminDto.Username), "Username is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(nameof(CreateSystemAdminDto.Email), "Email is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError(nameof(CreateSystemAdminDto.Role), "Role is required.");
                isValid = false;
            }

            if (!createdDate.HasValue)
            {
                ModelState.AddModelError(nameof(CreateSystemAdminDto.CreatedDate), "Created date is required.");
                isValid = false;
            }

            if (!lastLogin.HasValue)
            {
                ModelState.AddModelError(nameof(CreateSystemAdminDto.LastLogin), "Last login is required.");
                isValid = false;
            }

            var selectedServerIds = (managedNasServerIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (selectedServerIds.Count > 0)
            {
                var validServerIds = repository.GetAllNasServers().Select(server => server.Id).ToHashSet();
                var invalidServerIds = selectedServerIds.Where(serverId => !validServerIds.Contains(serverId)).ToList();
                if (invalidServerIds.Any())
                {
                    ModelState.AddModelError(nameof(CreateSystemAdminDto.ManagedNasServerIds), "One or more selected NAS servers do not exist.");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}
