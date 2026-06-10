using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NasIndexer.Dtos;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class NasServersApiTests
    {
        [Fact]
        public async Task GetNasServers_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("Collection"));

            var response = await client.GetAsync("/api/nas-servers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var servers = await response.Content.ReadFromJsonAsync<List<NasServerDto>>();
            Assert.NotNull(servers);
            Assert.Contains(servers, item => item.Id == server.Id);
        }

        [Fact]
        public async Task GetNasServers_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingServer = await TestDataFactory.CreateNasServerAsync(factory, $"Match-{token}");
            var nonMatchingServer = await TestDataFactory.CreateNasServerAsync(factory, $"Other-{token}");

            var response = await client.GetAsync($"/api/nas-servers?query=Match-{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var servers = await response.Content.ReadFromJsonAsync<List<NasServerDto>>();
            Assert.NotNull(servers);
            Assert.Contains(servers, item => item.Id == matchingServer.Id);
            Assert.DoesNotContain(servers, item => item.Id == nonMatchingServer.Id);
        }

        [Fact]
        public async Task GetNasServer_ReturnsOk_WhenServerExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("GetById"));

            var response = await client.GetAsync($"/api/nas-servers/{server.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<NasServerDto>();
            Assert.NotNull(dto);
            Assert.Equal(server.Id, dto.Id);
            Assert.Equal(server.Name, dto.Name);
        }

        [Fact]
        public async Task GetNasServer_ReturnsNotFound_WhenServerDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/nas-servers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetNasServer_ResponseDoesNotContainPasswordPropertyOrRawPassword()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            const string rawPassword = "raw-nas-password-secret";
            var server = await TestDataFactory.CreateNasServerAsync(
                factory,
                UniqueName("Secret"),
                password: rawPassword);

            var response = await client.GetAsync($"/api/nas-servers/{server.Id}");
            var json = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(json);
            Assert.False(document.RootElement.TryGetProperty("password", out _));
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(rawPassword, json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task PostNasServer_WithValidData_ReturnsCreatedAndIgnoresPassword()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            const string submittedPassword = "submitted-password-must-not-bind";
            var request = new
            {
                name = $"  {UniqueName("Created")}  ",
                ipAddress = "192.168.50.10",
                port = 1445,
                username = " api-reader ",
                isActive = true,
                lastScan = new DateTime(2026, 6, 10, 11, 0, 0, DateTimeKind.Utc),
                password = submittedPassword
            };

            var response = await client.PostAsJsonAsync("/api/nas-servers", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<NasServerDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.name.Trim(), dto.Name);
            Assert.Equal(request.ipAddress, dto.IpAddress);
            Assert.Equal(request.username.Trim(), dto.Username);

            var json = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(submittedPassword, json, StringComparison.Ordinal);

            var storedServer = await TestDataFactory.FindNasServerAsync(factory, dto.Id);
            Assert.NotNull(storedServer);
            Assert.Equal(string.Empty, storedServer.Password);
        }

        [Fact]
        public async Task PostNasServer_WithInvalidData_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new CreateNasServerDto
            {
                Name = " ",
                IpAddress = "not-an-ip-address",
                Port = 0,
                Username = "invalid",
                IsActive = true,
                LastScan = null
            };

            var response = await client.PostAsJsonAsync("/api/nas-servers", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutNasServer_WithValidData_UpdatesAllowedFieldsOnly()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            const string originalPassword = "preserve-this-secret";
            var server = await TestDataFactory.CreateNasServerAsync(
                factory,
                UniqueName("BeforeUpdate"),
                password: originalPassword);
            var request = new UpdateNasServerDto
            {
                Id = server.Id,
                Name = UniqueName("AfterUpdate"),
                IpAddress = "192.168.60.20",
                Port = 2445,
                Username = "updated-reader",
                IsActive = false,
                LastScan = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc)
            };

            var response = await client.PutAsJsonAsync($"/api/nas-servers/{server.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<NasServerDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name, dto.Name);
            Assert.Equal(request.IpAddress, dto.IpAddress);
            Assert.Equal(request.Port, dto.Port);
            Assert.Equal(request.Username, dto.Username);
            Assert.Equal(request.IsActive, dto.IsActive);

            var storedServer = await TestDataFactory.FindNasServerAsync(factory, server.Id);
            Assert.NotNull(storedServer);
            Assert.Equal(request.Name, storedServer.Name);
            Assert.Equal(originalPassword, storedServer.Password);
        }

        [Fact]
        public async Task PutNasServer_WithIdMismatch_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("Mismatch"));
            var request = new UpdateNasServerDto
            {
                Id = server.Id + 1,
                Name = UniqueName("MismatchUpdate"),
                IpAddress = "192.168.70.30",
                Port = 445,
                Username = "mismatch-reader",
                IsActive = true,
                LastScan = new DateTime(2026, 6, 10, 13, 0, 0, DateTimeKind.Utc)
            };

            var response = await client.PutAsJsonAsync($"/api/nas-servers/{server.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutNasServer_ForMissingServer_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new UpdateNasServerDto
            {
                Id = 999999,
                Name = UniqueName("MissingUpdate"),
                IpAddress = "192.168.80.40",
                Port = 445,
                Username = "missing-reader",
                IsActive = true,
                LastScan = new DateTime(2026, 6, 10, 14, 0, 0, DateTimeKind.Utc)
            };

            var response = await client.PutAsJsonAsync("/api/nas-servers/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNasServer_ForExistingServerWithoutDependents_DeletesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("Delete"));

            var response = await client.DeleteAsync($"/api/nas-servers/{server.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var storedServer = await TestDataFactory.FindNasServerAsync(factory, server.Id);
            Assert.Null(storedServer);
        }

        [Fact]
        public async Task DeleteNasServer_ForMissingServer_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();

            var response = await client.DeleteAsync("/api/nas-servers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteNasServer_ForServerWithScanJobs_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var server = await TestDataFactory.CreateNasServerWithScanJobAsync(factory, UniqueName("DependentDelete"));

            var response = await client.DeleteAsync($"/api/nas-servers/{server.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedServer = await TestDataFactory.FindNasServerAsync(factory, server.Id);
            Assert.NotNull(storedServer);
            Assert.Single(storedServer.ScanJobs);
        }

        private static string UniqueName(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
