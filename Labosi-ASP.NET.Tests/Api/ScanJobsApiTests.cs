using System.Net;
using System.Net.Http.Json;
using NasIndexer.Dtos;
using NasIndexer.Model;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class ScanJobsApiTests
    {
        [Fact]
        public async Task GetScanJobs_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory);

            var response = await client.GetAsync("/api/scan-jobs");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var scanJobs = await response.Content.ReadFromJsonAsync<List<ScanJobDto>>();
            Assert.NotNull(scanJobs);
            Assert.Contains(scanJobs, item => item.Id == scanJob.Id);
        }

        [Fact]
        public async Task GetScanJobs_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingScanJob = await TestDataFactory.CreateScanJobAsync(factory, rootPath: $"/match/{token}");
            var nonMatchingScanJob = await TestDataFactory.CreateScanJobAsync(factory, rootPath: $"/other/{token}");

            var response = await client.GetAsync($"/api/scan-jobs?query=/match/{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var scanJobs = await response.Content.ReadFromJsonAsync<List<ScanJobDto>>();
            Assert.NotNull(scanJobs);
            Assert.Contains(scanJobs, item => item.Id == matchingScanJob.Id);
            Assert.DoesNotContain(scanJobs, item => item.Id == nonMatchingScanJob.Id);
        }

        [Fact]
        public async Task GetScanJobs_WithStatus_FiltersByStatus()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var completedScanJob = await TestDataFactory.CreateScanJobAsync(factory, status: ScanStatus.Completed);
            var failedScanJob = await TestDataFactory.CreateScanJobAsync(factory, status: ScanStatus.Failed);

            var response = await client.GetAsync("/api/scan-jobs?status=Completed");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var scanJobs = await response.Content.ReadFromJsonAsync<List<ScanJobDto>>();
            Assert.NotNull(scanJobs);
            Assert.Contains(scanJobs, item => item.Id == completedScanJob.Id);
            Assert.DoesNotContain(scanJobs, item => item.Id == failedScanJob.Id);
        }

        [Fact]
        public async Task GetScanJobs_WithNasServerId_FiltersByNasServer()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var firstServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("ServerA"));
            var secondServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("ServerB"));
            var firstScanJob = await TestDataFactory.CreateScanJobAsync(factory, firstServer.Id);
            var secondScanJob = await TestDataFactory.CreateScanJobAsync(factory, secondServer.Id);

            var response = await client.GetAsync($"/api/scan-jobs?nasServerId={firstServer.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var scanJobs = await response.Content.ReadFromJsonAsync<List<ScanJobDto>>();
            Assert.NotNull(scanJobs);
            Assert.Contains(scanJobs, item => item.Id == firstScanJob.Id);
            Assert.DoesNotContain(scanJobs, item => item.Id == secondScanJob.Id);
        }

        [Fact]
        public async Task GetScanJob_ReturnsOk_WhenScanJobExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory);

            var response = await client.GetAsync($"/api/scan-jobs/{scanJob.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<ScanJobDto>();
            Assert.NotNull(dto);
            Assert.Equal(scanJob.Id, dto.Id);
            Assert.Equal(scanJob.NasServerId, dto.NasServerId);
            Assert.NotNull(dto.NasServer);
        }

        [Fact]
        public async Task GetScanJob_ReturnsNotFound_WhenScanJobDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/scan-jobs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostScanJob_WithValidData_ReturnsCreatedAndCreatesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("CreateScanServer"));
            var request = new CreateScanJobDto
            {
                NasServerId = server.Id,
                Status = ScanStatus.Running,
                StartTime = new DateTime(2026, 6, 10, 17, 0, 0, DateTimeKind.Utc),
                EndTime = null,
                RootPath = " /api/create/scan ",
                TotalFiles = 100,
                ProcessedFiles = 20
            };

            var response = await client.PostAsJsonAsync("/api/scan-jobs", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<ScanJobDto>();
            Assert.NotNull(dto);
            Assert.Equal(server.Id, dto.NasServerId);
            Assert.Equal(request.Status, dto.Status);
            Assert.Equal(request.RootPath.Trim(), dto.RootPath);

            var storedScanJob = await TestDataFactory.FindScanJobAsync(factory, dto.Id);
            Assert.NotNull(storedScanJob);
            Assert.Equal(request.RootPath.Trim(), storedScanJob.RootPath);
        }

        [Fact]
        public async Task PostScanJob_WithMissingOrInvalidNasServerId_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new CreateScanJobDto
            {
                NasServerId = 999999,
                Status = ScanStatus.Pending,
                StartTime = new DateTime(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc),
                RootPath = "/invalid/server",
                TotalFiles = 1,
                ProcessedFiles = 0
            };

            var response = await client.PostAsJsonAsync("/api/scan-jobs", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostScanJob_WithEndTimeBeforeStartTime_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("EndBeforeStart"));
            var startTime = new DateTime(2026, 6, 10, 19, 0, 0, DateTimeKind.Utc);
            var request = new CreateScanJobDto
            {
                NasServerId = server.Id,
                Status = ScanStatus.Failed,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(-1),
                RootPath = "/invalid/time",
                TotalFiles = 1,
                ProcessedFiles = 0
            };

            var response = await client.PostAsJsonAsync("/api/scan-jobs", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostScanJob_WithProcessedFilesGreaterThanTotalFiles_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("ProgressInvalid"));
            var request = new CreateScanJobDto
            {
                NasServerId = server.Id,
                Status = ScanStatus.Running,
                StartTime = new DateTime(2026, 6, 10, 20, 0, 0, DateTimeKind.Utc),
                RootPath = "/invalid/progress",
                TotalFiles = 5,
                ProcessedFiles = 6
            };

            var response = await client.PostAsJsonAsync("/api/scan-jobs", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutScanJob_WithValidData_UpdatesAllowedFields()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var firstServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("BeforeUpdateServer"));
            var secondServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("AfterUpdateServer"));
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory, firstServer.Id);
            var request = new UpdateScanJobDto
            {
                Id = scanJob.Id,
                NasServerId = secondServer.Id,
                Status = ScanStatus.Completed,
                StartTime = new DateTime(2026, 6, 10, 21, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 6, 10, 21, 30, 0, DateTimeKind.Utc),
                RootPath = "/updated/root",
                TotalFiles = 30,
                ProcessedFiles = 30
            };

            var response = await client.PutAsJsonAsync($"/api/scan-jobs/{scanJob.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<ScanJobDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.NasServerId, dto.NasServerId);
            Assert.Equal(request.Status, dto.Status);
            Assert.Equal(request.RootPath, dto.RootPath);
            Assert.Equal(request.ProcessedFiles, dto.ProcessedFiles);

            var storedScanJob = await TestDataFactory.FindScanJobAsync(factory, scanJob.Id);
            Assert.NotNull(storedScanJob);
            Assert.Equal(secondServer.Id, storedScanJob.NasServerId);
            Assert.Equal(request.RootPath, storedScanJob.RootPath);
        }

        [Fact]
        public async Task PutScanJob_WithIdMismatch_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory);
            var request = new UpdateScanJobDto
            {
                Id = scanJob.Id + 1,
                NasServerId = scanJob.NasServerId,
                Status = ScanStatus.Completed,
                StartTime = new DateTime(2026, 6, 10, 22, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 6, 10, 22, 15, 0, DateTimeKind.Utc),
                RootPath = "/mismatch/root",
                TotalFiles = 2,
                ProcessedFiles = 2
            };

            var response = await client.PutAsJsonAsync($"/api/scan-jobs/{scanJob.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutScanJob_ForMissingScanJob_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("MissingScanUpdate"));
            var request = new UpdateScanJobDto
            {
                Id = 999999,
                NasServerId = server.Id,
                Status = ScanStatus.Pending,
                StartTime = new DateTime(2026, 6, 10, 23, 0, 0, DateTimeKind.Utc),
                RootPath = "/missing/root",
                TotalFiles = 1,
                ProcessedFiles = 0
            };

            var response = await client.PutAsJsonAsync("/api/scan-jobs/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteScanJob_ForExistingScanJobWithoutDirectories_DeletesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory);

            var response = await client.DeleteAsync($"/api/scan-jobs/{scanJob.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var storedScanJob = await TestDataFactory.FindScanJobAsync(factory, scanJob.Id);
            Assert.Null(storedScanJob);
        }

        [Fact]
        public async Task DeleteScanJob_ForMissingScanJob_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();

            var response = await client.DeleteAsync("/api/scan-jobs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteScanJob_ForScanJobWithScannedDirectories_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var scanJob = await TestDataFactory.CreateScanJobWithDirectoryAsync(factory);

            var response = await client.DeleteAsync($"/api/scan-jobs/{scanJob.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedScanJob = await TestDataFactory.FindScanJobAsync(factory, scanJob.Id);
            Assert.NotNull(storedScanJob);
            Assert.Single(storedScanJob.ScannedDirectories);
        }

        private static string UniqueName(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
