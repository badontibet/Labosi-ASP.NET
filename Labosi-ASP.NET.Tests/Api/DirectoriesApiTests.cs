using System.Net;
using System.Net.Http.Json;
using NasIndexer.Dtos;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class DirectoriesApiTests
    {
        [Fact]
        public async Task GetDirectories_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);

            var response = await client.GetAsync("/api/directories");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var directories = await response.Content.ReadFromJsonAsync<List<DirectoryItemDto>>();
            Assert.NotNull(directories);
            Assert.Contains(directories, item => item.Id == directory.Id);
        }

        [Fact]
        public async Task GetDirectories_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingDirectory = await TestDataFactory.CreateDirectoryAsync(factory, name: $"match-{token}"[..38]);
            var nonMatchingDirectory = await TestDataFactory.CreateDirectoryAsync(factory, name: $"other-{token}"[..38]);

            var response = await client.GetAsync($"/api/directories?query=match-{token[..8]}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var directories = await response.Content.ReadFromJsonAsync<List<DirectoryItemDto>>();
            Assert.NotNull(directories);
            Assert.Contains(directories, item => item.Id == matchingDirectory.Id);
            Assert.DoesNotContain(directories, item => item.Id == nonMatchingDirectory.Id);
        }

        [Fact]
        public async Task GetDirectories_WithScanJobId_FiltersByScanJob()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var firstScanJob = await TestDataFactory.CreateScanJobAsync(factory);
            var secondScanJob = await TestDataFactory.CreateScanJobAsync(factory);
            var firstDirectory = await TestDataFactory.CreateDirectoryAsync(factory, scanJobId: firstScanJob.Id);
            var secondDirectory = await TestDataFactory.CreateDirectoryAsync(factory, scanJobId: secondScanJob.Id);

            var response = await client.GetAsync($"/api/directories?scanJobId={firstScanJob.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var directories = await response.Content.ReadFromJsonAsync<List<DirectoryItemDto>>();
            Assert.NotNull(directories);
            Assert.Contains(directories, item => item.Id == firstDirectory.Id);
            Assert.DoesNotContain(directories, item => item.Id == secondDirectory.Id);
        }

        [Fact]
        public async Task GetDirectories_WithParentId_FiltersByParent()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var firstParent = await TestDataFactory.CreateDirectoryAsync(factory);
            var secondParent = await TestDataFactory.CreateDirectoryAsync(factory);
            var firstChild = await TestDataFactory.CreateDirectoryAsync(factory, parentId: firstParent.Id);
            var secondChild = await TestDataFactory.CreateDirectoryAsync(factory, parentId: secondParent.Id);

            var response = await client.GetAsync($"/api/directories?parentId={firstParent.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var directories = await response.Content.ReadFromJsonAsync<List<DirectoryItemDto>>();
            Assert.NotNull(directories);
            Assert.Contains(directories, item => item.Id == firstChild.Id);
            Assert.DoesNotContain(directories, item => item.Id == secondChild.Id);
        }

        [Fact]
        public async Task GetDirectory_ReturnsOk_WhenDirectoryExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var parent = await TestDataFactory.CreateDirectoryAsync(factory);
            var directory = await TestDataFactory.CreateDirectoryAsync(factory, parentId: parent.Id);

            var response = await client.GetAsync($"/api/directories/{directory.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<DirectoryItemDto>();
            Assert.NotNull(dto);
            Assert.Equal(directory.Id, dto.Id);
            Assert.Equal(parent.Id, dto.ParentId);
            Assert.NotNull(dto.Parent);
        }

        [Fact]
        public async Task GetDirectory_ReturnsNotFound_WhenDirectoryDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/directories/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostDirectory_WithValidData_ReturnsCreatedAndCreatesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory);
            var parent = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = new CreateDirectoryItemDto
            {
                Name = " api-directory ",
                Path = " /api/directories/created ",
                ScanJobId = scanJob.Id,
                ParentId = parent.Id,
                CreatedDate = new DateTime(2026, 6, 10, 20, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 10, 20, 30, 0, DateTimeKind.Utc)
            };

            var response = await client.PostAsJsonAsync("/api/directories", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<DirectoryItemDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name.Trim(), dto.Name);
            Assert.Equal(request.Path.Trim(), dto.Path);
            Assert.Equal(scanJob.Id, dto.ScanJobId);
            Assert.Equal(parent.Id, dto.ParentId);

            var storedDirectory = await TestDataFactory.FindDirectoryAsync(factory, dto.Id);
            Assert.NotNull(storedDirectory);
            Assert.Equal(request.Path.Trim(), storedDirectory.Path);
        }

        [Fact]
        public async Task PostDirectory_WithInvalidOrMissingRequiredFields_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = new CreateDirectoryItemDto
            {
                Name = " ",
                Path = " ",
                CreatedDate = null,
                ModifiedDate = null
            };

            var response = await client.PostAsJsonAsync("/api/directories", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostDirectory_WithMissingScanJobId_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = ValidCreateDto();
            request.ScanJobId = 999999;

            var response = await client.PostAsJsonAsync("/api/directories", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostDirectory_WithMissingParentId_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = ValidCreateDto();
            request.ParentId = 999999;

            var response = await client.PostAsJsonAsync("/api/directories", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostDirectory_WithModifiedDateBeforeCreatedDate_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = ValidCreateDto();
            request.CreatedDate = new DateTime(2026, 6, 10, 22, 0, 0, DateTimeKind.Utc);
            request.ModifiedDate = request.CreatedDate.Value.AddMinutes(-1);

            var response = await client.PostAsJsonAsync("/api/directories", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutDirectory_WithValidData_UpdatesAllowedFields()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var scanJob = await TestDataFactory.CreateScanJobAsync(factory);
            var parent = await TestDataFactory.CreateDirectoryAsync(factory);
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = new UpdateDirectoryItemDto
            {
                Id = directory.Id,
                Name = "updated-directory",
                Path = "/updated/path",
                ScanJobId = scanJob.Id,
                ParentId = parent.Id,
                CreatedDate = new DateTime(2026, 6, 10, 23, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 10, 23, 10, 0, DateTimeKind.Utc)
            };

            var response = await client.PutAsJsonAsync($"/api/directories/{directory.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<DirectoryItemDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name, dto.Name);
            Assert.Equal(request.Path, dto.Path);
            Assert.Equal(scanJob.Id, dto.ScanJobId);
            Assert.Equal(parent.Id, dto.ParentId);

            var storedDirectory = await TestDataFactory.FindDirectoryAsync(factory, directory.Id);
            Assert.NotNull(storedDirectory);
            Assert.Equal(request.Name, storedDirectory.Name);
            Assert.Equal(parent.Id, storedDirectory.ParentId);
        }

        [Fact]
        public async Task PutDirectory_WithIdMismatch_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = ValidUpdateDto(directory.Id + 1);

            var response = await client.PutAsJsonAsync($"/api/directories/{directory.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutDirectory_ForMissingDirectory_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = ValidUpdateDto(999999);

            var response = await client.PutAsJsonAsync("/api/directories/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutDirectory_SettingParentIdToItself_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = ValidUpdateDto(directory.Id);
            request.ParentId = directory.Id;

            var response = await client.PutAsJsonAsync($"/api/directories/{directory.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutDirectory_MovingUnderOwnDescendant_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var parent = await TestDataFactory.CreateDirectoryAsync(factory);
            var child = await TestDataFactory.CreateDirectoryAsync(factory, parentId: parent.Id);
            var request = ValidUpdateDto(parent.Id);
            request.ParentId = child.Id;

            var response = await client.PutAsJsonAsync($"/api/directories/{parent.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteDirectory_ForExistingDirectoryWithoutChildrenOrFiles_DeletesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);

            var response = await client.DeleteAsync($"/api/directories/{directory.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var storedDirectory = await TestDataFactory.FindDirectoryAsync(factory, directory.Id);
            Assert.Null(storedDirectory);
        }

        [Fact]
        public async Task DeleteDirectory_ForMissingDirectory_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.DeleteAsync("/api/directories/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteDirectory_ForDirectoryWithChildDirectories_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryWithChildAsync(factory);

            var response = await client.DeleteAsync($"/api/directories/{directory.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedDirectory = await TestDataFactory.FindDirectoryAsync(factory, directory.Id);
            Assert.NotNull(storedDirectory);
            Assert.Single(storedDirectory.SubDirectories);
        }

        [Fact]
        public async Task DeleteDirectory_ForDirectoryWithFiles_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryWithFileAsync(factory);

            var response = await client.DeleteAsync($"/api/directories/{directory.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedDirectory = await TestDataFactory.FindDirectoryAsync(factory, directory.Id);
            Assert.NotNull(storedDirectory);
            Assert.Single(storedDirectory.Files);
        }

        private static CreateDirectoryItemDto ValidCreateDto()
        {
            return new CreateDirectoryItemDto
            {
                Name = "valid-directory",
                Path = "/valid/directory",
                CreatedDate = new DateTime(2026, 6, 10, 21, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 10, 21, 5, 0, DateTimeKind.Utc)
            };
        }

        private static UpdateDirectoryItemDto ValidUpdateDto(int id)
        {
            return new UpdateDirectoryItemDto
            {
                Id = id,
                Name = "valid-update",
                Path = "/valid/update",
                CreatedDate = new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 11, 8, 5, 0, DateTimeKind.Utc)
            };
        }
    }
}
