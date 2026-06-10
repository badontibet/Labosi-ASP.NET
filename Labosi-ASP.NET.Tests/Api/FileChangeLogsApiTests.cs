using System.Net;
using System.Net.Http.Json;
using NasIndexer.Dtos;
using NasIndexer.Model;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class FileChangeLogsApiTests
    {
        [Fact]
        public async Task GetFileChangeLogs_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var changeLog = await TestDataFactory.CreateFileChangeLogAsync(factory);

            var response = await client.GetAsync("/api/file-change-logs");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var changeLogs = await response.Content.ReadFromJsonAsync<List<FileChangeLogDto>>();
            Assert.NotNull(changeLogs);
            Assert.Contains(changeLogs, item => item.Id == changeLog.Id);
        }

        [Fact]
        public async Task GetFileChangeLogs_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingChangeLog = await TestDataFactory.CreateFileChangeLogAsync(factory, newValue: $"match-{token}");
            var nonMatchingChangeLog = await TestDataFactory.CreateFileChangeLogAsync(factory, newValue: $"other-{token}");

            var response = await client.GetAsync($"/api/file-change-logs?query=match-{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var changeLogs = await response.Content.ReadFromJsonAsync<List<FileChangeLogDto>>();
            Assert.NotNull(changeLogs);
            Assert.Contains(changeLogs, item => item.Id == matchingChangeLog.Id);
            Assert.DoesNotContain(changeLogs, item => item.Id == nonMatchingChangeLog.Id);
        }

        [Fact]
        public async Task GetFileChangeLogs_WithChangeType_FiltersByChangeType()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var createdLog = await TestDataFactory.CreateFileChangeLogAsync(factory, changeType: ChangeType.Created);
            var deletedLog = await TestDataFactory.CreateFileChangeLogAsync(factory, changeType: ChangeType.Deleted);

            var response = await client.GetAsync("/api/file-change-logs?changeType=Created");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var changeLogs = await response.Content.ReadFromJsonAsync<List<FileChangeLogDto>>();
            Assert.NotNull(changeLogs);
            Assert.Contains(changeLogs, item => item.Id == createdLog.Id);
            Assert.DoesNotContain(changeLogs, item => item.Id == deletedLog.Id);
        }

        [Fact]
        public async Task GetFileChangeLogs_WithFileId_FiltersByFile()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var firstFile = await TestDataFactory.CreateFileItemAsync(factory);
            var secondFile = await TestDataFactory.CreateFileItemAsync(factory);
            var firstLog = await TestDataFactory.CreateFileChangeLogAsync(factory, firstFile.Id);
            var secondLog = await TestDataFactory.CreateFileChangeLogAsync(factory, secondFile.Id);

            var response = await client.GetAsync($"/api/file-change-logs?fileId={firstFile.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var changeLogs = await response.Content.ReadFromJsonAsync<List<FileChangeLogDto>>();
            Assert.NotNull(changeLogs);
            Assert.Contains(changeLogs, item => item.Id == firstLog.Id);
            Assert.DoesNotContain(changeLogs, item => item.Id == secondLog.Id);
        }

        [Fact]
        public async Task GetFileChangeLog_ReturnsOk_WhenChangeLogExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var changeLog = await TestDataFactory.CreateFileChangeLogAsync(factory, user: "auditor");

            var response = await client.GetAsync($"/api/file-change-logs/{changeLog.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileChangeLogDto>();
            Assert.NotNull(dto);
            Assert.Equal(changeLog.Id, dto.Id);
            Assert.Equal("auditor", dto.User);
            Assert.NotNull(dto.File);
        }

        [Fact]
        public async Task GetFileChangeLog_ReturnsNotFound_WhenChangeLogDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/file-change-logs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostFileChangeLog_IsNotAvailable()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/file-change-logs", new
            {
                fileId = 1,
                changeType = "Created"
            });

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }

        [Fact]
        public async Task PutFileChangeLog_IsNotAvailable()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.PutAsJsonAsync("/api/file-change-logs/1", new
            {
                id = 1,
                fileId = 1,
                changeType = "Modified"
            });

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }

        [Fact]
        public async Task DeleteFileChangeLog_IsNotAvailable()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.DeleteAsync("/api/file-change-logs/1");

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }
    }
}
