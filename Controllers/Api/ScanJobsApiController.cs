using Microsoft.AspNetCore.Mvc;
using NasIndexer.Dtos;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers.Api
{
    [ApiController]
    [Route("api/scan-jobs")]
    public class ScanJobsApiController : ControllerBase
    {
        private readonly INasRepository repository;

        public ScanJobsApiController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ScanJobDto>> GetScanJobs(
            [FromQuery] string? query,
            [FromQuery] ScanStatus? status,
            [FromQuery] int? nasServerId)
        {
            var scanJobs = string.IsNullOrWhiteSpace(query)
                ? repository.GetAllScanJobs()
                : repository.SearchScanJobs(query);

            if (status.HasValue)
            {
                scanJobs = scanJobs
                    .Where(scanJob => scanJob.Status == status.Value)
                    .ToList();
            }

            if (nasServerId.HasValue)
            {
                scanJobs = scanJobs
                    .Where(scanJob => scanJob.NasServerId == nasServerId.Value)
                    .ToList();
            }

            return Ok(scanJobs.Select(ToDto));
        }

        [HttpGet("{id:int}")]
        public ActionResult<ScanJobDto> GetScanJob(int id)
        {
            var scanJob = repository.GetScanJobById(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            return Ok(ToDto(scanJob));
        }

        [HttpPost]
        public ActionResult<ScanJobDto> CreateScanJob(CreateScanJobDto dto)
        {
            if (!ValidateScanJobInput(dto.NasServerId, dto.StartTime, dto.RootPath))
            {
                return ValidationProblem(ModelState);
            }

            var scanJob = new ScanJob
            {
                NasServerId = dto.NasServerId!.Value,
                Status = dto.Status,
                StartTime = dto.StartTime!.Value,
                EndTime = dto.EndTime,
                RootPath = dto.RootPath.Trim(),
                TotalFiles = dto.TotalFiles,
                ProcessedFiles = dto.ProcessedFiles
            };

            repository.AddScanJob(scanJob);

            var created = repository.GetScanJobById(scanJob.Id) ?? scanJob;
            return CreatedAtAction(nameof(GetScanJob), new { id = created.Id }, ToDto(created));
        }

        [HttpPut("{id:int}")]
        public ActionResult<ScanJobDto> UpdateScanJob(int id, UpdateScanJobDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest("Route id must match the DTO id.");
            }

            if (!ValidateScanJobInput(dto.NasServerId, dto.StartTime, dto.RootPath))
            {
                return ValidationProblem(ModelState);
            }

            var scanJob = new ScanJob
            {
                Id = dto.Id,
                NasServerId = dto.NasServerId!.Value,
                Status = dto.Status,
                StartTime = dto.StartTime!.Value,
                EndTime = dto.EndTime,
                RootPath = dto.RootPath.Trim(),
                TotalFiles = dto.TotalFiles,
                ProcessedFiles = dto.ProcessedFiles
            };

            if (!repository.UpdateScanJob(scanJob))
            {
                return NotFound();
            }

            var updated = repository.GetScanJobById(id) ?? scanJob;
            return Ok(ToDto(updated));
        }

        [HttpDelete("{id:int}")]
        public IActionResult DeleteScanJob(int id)
        {
            var scanJob = repository.GetScanJobById(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            if (repository.ScanJobHasDirectories(id))
            {
                return Conflict(new
                {
                    message = "This scan job cannot be deleted because it has scanned directories."
                });
            }

            if (!repository.DeleteScanJob(id))
            {
                return Conflict(new
                {
                    message = "This scan job could not be deleted. Refresh and check connected directories."
                });
            }

            return NoContent();
        }

        private static ScanJobDto ToDto(ScanJob scanJob)
        {
            return new ScanJobDto
            {
                Id = scanJob.Id,
                NasServerId = scanJob.NasServerId,
                NasServer = scanJob.NasServer == null
                    ? null
                    : new NasServerSummaryDto
                    {
                        Id = scanJob.NasServer.Id,
                        Name = scanJob.NasServer.Name,
                        IpAddress = scanJob.NasServer.IpAddress,
                        Port = scanJob.NasServer.Port
                    },
                Status = scanJob.Status,
                StartTime = scanJob.StartTime,
                EndTime = scanJob.EndTime,
                RootPath = scanJob.RootPath,
                TotalFiles = scanJob.TotalFiles,
                ProcessedFiles = scanJob.ProcessedFiles,
                ScannedDirectoryCount = scanJob.ScannedDirectories.Count
            };
        }

        private bool ValidateScanJobInput(int? nasServerId, DateTime? startTime, string? rootPath)
        {
            var isValid = true;

            if (!nasServerId.HasValue)
            {
                ModelState.AddModelError(nameof(CreateScanJobDto.NasServerId), "NAS server is required.");
                isValid = false;
            }
            else if (repository.GetNasServerById(nasServerId.Value) == null)
            {
                ModelState.AddModelError(nameof(CreateScanJobDto.NasServerId), "Choose an existing NAS server.");
                isValid = false;
            }

            if (!startTime.HasValue)
            {
                ModelState.AddModelError(nameof(CreateScanJobDto.StartTime), "Start time is required.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                ModelState.AddModelError(nameof(CreateScanJobDto.RootPath), "Root path is required.");
                isValid = false;
            }

            return isValid;
        }
    }
}
