using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NasIndexer.Dtos;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class SystemAdminsApiTests
    {
        [Fact]
        public async Task GetSystemAdmins_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var admin = await TestDataFactory.CreateSystemAdminAsync(factory, UniqueName("Collection"));

            var response = await client.GetAsync("/api/system-admins");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var admins = await response.Content.ReadFromJsonAsync<List<SystemAdminDto>>();
            Assert.NotNull(admins);
            Assert.Contains(admins, item => item.Id == admin.Id);
        }

        [Fact]
        public async Task GetSystemAdmins_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingAdmin = await TestDataFactory.CreateSystemAdminAsync(factory, $"Match-{token}");
            var nonMatchingAdmin = await TestDataFactory.CreateSystemAdminAsync(factory, $"Other-{token}");

            var response = await client.GetAsync($"/api/system-admins?query=Match-{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var admins = await response.Content.ReadFromJsonAsync<List<SystemAdminDto>>();
            Assert.NotNull(admins);
            Assert.Contains(admins, item => item.Id == matchingAdmin.Id);
            Assert.DoesNotContain(admins, item => item.Id == nonMatchingAdmin.Id);
        }

        [Fact]
        public async Task GetSystemAdmins_WithNasServerId_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var includedServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("ManagedIncluded"));
            var excludedServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("ManagedExcluded"));
            var includedAdmin = await TestDataFactory.CreateSystemAdminAsync(
                factory,
                UniqueName("IncludedAdmin"),
                managedNasServerIds: new[] { includedServer.Id });
            var excludedAdmin = await TestDataFactory.CreateSystemAdminAsync(
                factory,
                UniqueName("ExcludedAdmin"),
                managedNasServerIds: new[] { excludedServer.Id });

            var response = await client.GetAsync($"/api/system-admins?nasServerId={includedServer.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var admins = await response.Content.ReadFromJsonAsync<List<SystemAdminDto>>();
            Assert.NotNull(admins);
            Assert.Contains(admins, item => item.Id == includedAdmin.Id);
            Assert.DoesNotContain(admins, item => item.Id == excludedAdmin.Id);
        }

        [Fact]
        public async Task GetSystemAdmin_ReturnsOk_WhenAdminExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var admin = await TestDataFactory.CreateSystemAdminAsync(factory, UniqueName("GetById"));

            var response = await client.GetAsync($"/api/system-admins/{admin.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<SystemAdminDto>();
            Assert.NotNull(dto);
            Assert.Equal(admin.Id, dto.Id);
            Assert.Equal(admin.Username, dto.Username);
        }

        [Fact]
        public async Task GetSystemAdmin_ReturnsNotFound_WhenAdminDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/system-admins/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSystemAdmin_ResponseDoesNotContainPasswordProperty()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var admin = await TestDataFactory.CreateSystemAdminAsync(factory, UniqueName("NoSecretProperty"));

            var response = await client.GetAsync($"/api/system-admins/{admin.Id}");
            var json = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(json);
            Assert.False(document.RootElement.TryGetProperty("password", out _));
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetSystemAdmin_ResponseDoesNotContainRawPasswordValue()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            const string rawPassword = "raw-system-admin-secret";
            var admin = await TestDataFactory.CreateSystemAdminAsync(
                factory,
                UniqueName("NoRawPassword"),
                password: rawPassword);

            var response = await client.GetAsync($"/api/system-admins/{admin.Id}");
            var json = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain(rawPassword, json, StringComparison.Ordinal);
        }

        [Fact]
        public async Task PostSystemAdmin_WithValidData_ReturnsCreatedAndIgnoresPassword()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("CreateManaged"));
            const string submittedPassword = "submitted-system-admin-password";
            var request = new
            {
                username = $"  {UniqueName("Created")}  ",
                email = " created-admin@example.test ",
                role = " StorageAdmin ",
                createdDate = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc),
                lastLogin = new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
                managedNasServerIds = new[] { server.Id, server.Id },
                password = submittedPassword
            };

            var response = await client.PostAsJsonAsync("/api/system-admins", request);
            var json = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = JsonSerializer.Deserialize<SystemAdminDto>(json, JsonOptions);
            Assert.NotNull(dto);
            Assert.Equal(request.username.Trim(), dto.Username);
            Assert.Equal(request.email.Trim(), dto.Email);
            Assert.Equal(request.role.Trim(), dto.Role);
            Assert.Equal(new[] { server.Id }, dto.ManagedNasServerIds);
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(submittedPassword, json, StringComparison.Ordinal);

            var storedAdmin = await TestDataFactory.FindSystemAdminAsync(factory, dto.Id);
            Assert.NotNull(storedAdmin);
            Assert.Equal("NotUsedByApi", storedAdmin.Password);
        }

        [Fact]
        public async Task PostSystemAdmin_WithInvalidData_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new CreateSystemAdminDto
            {
                Username = " ",
                Email = "not-an-email",
                Role = " ",
                CreatedDate = null,
                LastLogin = null
            };

            var response = await client.PostAsJsonAsync("/api/system-admins", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostSystemAdmin_WithInvalidManagedServerIds_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = ValidCreateDto();
            request.ManagedNasServerIds = new List<int> { 999999 };

            var response = await client.PostAsJsonAsync("/api/system-admins", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutSystemAdmin_WithValidData_UpdatesAllowedFieldsAndManagedServers()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var originalServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("OriginalManaged"));
            var newServer = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("NewManaged"));
            var admin = await TestDataFactory.CreateSystemAdminAsync(
                factory,
                UniqueName("BeforeUpdate"),
                managedNasServerIds: new[] { originalServer.Id });
            var request = new UpdateSystemAdminDto
            {
                Id = admin.Id,
                Username = UniqueName("AfterUpdate"),
                Email = "updated-admin@example.test",
                Role = "StorageManager",
                CreatedDate = new DateTime(2026, 6, 10, 10, 0, 0, DateTimeKind.Utc),
                LastLogin = new DateTime(2026, 6, 10, 11, 0, 0, DateTimeKind.Utc),
                ManagedNasServerIds = new List<int> { newServer.Id, newServer.Id }
            };

            var response = await client.PutAsJsonAsync($"/api/system-admins/{admin.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<SystemAdminDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Username, dto.Username);
            Assert.Equal(request.Email, dto.Email);
            Assert.Equal(request.Role, dto.Role);
            Assert.Equal(new[] { newServer.Id }, dto.ManagedNasServerIds);

            var storedAdmin = await TestDataFactory.FindSystemAdminAsync(factory, admin.Id);
            Assert.NotNull(storedAdmin);
            Assert.Equal(request.Username, storedAdmin.Username);
            Assert.Single(storedAdmin.ManagedServers);
            Assert.Equal(newServer.Id, storedAdmin.ManagedServers.Single().Id);
        }

        [Fact]
        public async Task PutSystemAdmin_PreservesPasswordAndIgnoresSubmittedPassword()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            const string originalPassword = "preserve-system-admin-secret";
            const string submittedPassword = "attempted-system-admin-secret";
            var admin = await TestDataFactory.CreateSystemAdminAsync(
                factory,
                UniqueName("PreservePassword"),
                password: originalPassword);
            var request = new
            {
                id = admin.Id,
                username = UniqueName("PreservedUpdate"),
                email = "preserved-admin@example.test",
                role = "StorageAuditor",
                createdDate = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc),
                lastLogin = new DateTime(2026, 6, 10, 13, 0, 0, DateTimeKind.Utc),
                managedNasServerIds = Array.Empty<int>(),
                password = submittedPassword
            };

            var response = await client.PutAsJsonAsync($"/api/system-admins/{admin.Id}", request);
            var json = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(originalPassword, json, StringComparison.Ordinal);
            Assert.DoesNotContain(submittedPassword, json, StringComparison.Ordinal);

            var storedAdmin = await TestDataFactory.FindSystemAdminAsync(factory, admin.Id);
            Assert.NotNull(storedAdmin);
            Assert.Equal(originalPassword, storedAdmin.Password);
        }

        [Fact]
        public async Task PutSystemAdmin_WithIdMismatch_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var admin = await TestDataFactory.CreateSystemAdminAsync(factory, UniqueName("Mismatch"));
            var request = ValidUpdateDto(admin.Id + 1);

            var response = await client.PutAsJsonAsync($"/api/system-admins/{admin.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutSystemAdmin_ForMissingAdmin_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = ValidUpdateDto(999999);

            var response = await client.PutAsJsonAsync("/api/system-admins/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutSystemAdmin_WithInvalidManagedServerIds_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var admin = await TestDataFactory.CreateSystemAdminAsync(factory, UniqueName("InvalidManagedUpdate"));
            var request = ValidUpdateDto(admin.Id);
            request.ManagedNasServerIds = new List<int> { 999999 };

            var response = await client.PutAsJsonAsync($"/api/system-admins/{admin.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSystemAdmin_ForExistingUnassignedAdmin_DeletesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var admin = await TestDataFactory.CreateSystemAdminAsync(factory, UniqueName("Delete"));

            var response = await client.DeleteAsync($"/api/system-admins/{admin.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var storedAdmin = await TestDataFactory.FindSystemAdminAsync(factory, admin.Id);
            Assert.Null(storedAdmin);
        }

        [Fact]
        public async Task DeleteSystemAdmin_ForMissingAdmin_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();

            var response = await client.DeleteAsync("/api/system-admins/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSystemAdmin_ForAdminWithManagedServers_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var server = await TestDataFactory.CreateNasServerAsync(factory, UniqueName("BlockedManaged"));
            var admin = await TestDataFactory.CreateSystemAdminAsync(
                factory,
                UniqueName("BlockedDelete"),
                managedNasServerIds: new[] { server.Id });

            var response = await client.DeleteAsync($"/api/system-admins/{admin.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedAdmin = await TestDataFactory.FindSystemAdminAsync(factory, admin.Id);
            Assert.NotNull(storedAdmin);
            Assert.Single(storedAdmin.ManagedServers);
        }

        private static CreateSystemAdminDto ValidCreateDto()
        {
            return new CreateSystemAdminDto
            {
                Username = UniqueName("Created"),
                Email = "created-admin@example.test",
                Role = "StorageAdmin",
                CreatedDate = new DateTime(2026, 6, 10, 14, 0, 0, DateTimeKind.Utc),
                LastLogin = new DateTime(2026, 6, 10, 15, 0, 0, DateTimeKind.Utc)
            };
        }

        private static UpdateSystemAdminDto ValidUpdateDto(int id)
        {
            return new UpdateSystemAdminDto
            {
                Id = id,
                Username = UniqueName("Updated"),
                Email = "updated-admin@example.test",
                Role = "StorageManager",
                CreatedDate = new DateTime(2026, 6, 10, 16, 0, 0, DateTimeKind.Utc),
                LastLogin = new DateTime(2026, 6, 10, 17, 0, 0, DateTimeKind.Utc)
            };
        }

        private static string UniqueName(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    }
}
