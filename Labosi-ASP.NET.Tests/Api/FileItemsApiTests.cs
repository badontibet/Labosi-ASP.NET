using System.Net;
using System.Net.Http.Json;
using NasIndexer.Dtos;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class FileItemsApiTests
    {
        [Fact]
        public async Task GetFiles_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.GetAsync("/api/files");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var files = await response.Content.ReadFromJsonAsync<List<FileItemDto>>();
            Assert.NotNull(files);
            Assert.Contains(files, item => item.Id == file.Id);
        }

        [Fact]
        public async Task GetFiles_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingFile = await TestDataFactory.CreateFileItemAsync(factory, name: $"match-{token}.txt");
            var nonMatchingFile = await TestDataFactory.CreateFileItemAsync(factory, name: $"other-{token}.txt");

            var response = await client.GetAsync($"/api/files?query=match-{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var files = await response.Content.ReadFromJsonAsync<List<FileItemDto>>();
            Assert.NotNull(files);
            Assert.Contains(files, item => item.Id == matchingFile.Id);
            Assert.DoesNotContain(files, item => item.Id == nonMatchingFile.Id);
        }

        [Fact]
        public async Task GetFiles_WithDirectoryId_FiltersByDirectory()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var firstDirectory = await TestDataFactory.CreateDirectoryAsync(factory);
            var secondDirectory = await TestDataFactory.CreateDirectoryAsync(factory);
            var firstFile = await TestDataFactory.CreateFileItemAsync(factory, firstDirectory.Id);
            var secondFile = await TestDataFactory.CreateFileItemAsync(factory, secondDirectory.Id);

            var response = await client.GetAsync($"/api/files?directoryId={firstDirectory.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var files = await response.Content.ReadFromJsonAsync<List<FileItemDto>>();
            Assert.NotNull(files);
            Assert.Contains(files, item => item.Id == firstFile.Id);
            Assert.DoesNotContain(files, item => item.Id == secondFile.Id);
        }

        [Fact]
        public async Task GetFiles_WithTagId_FiltersByTag()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var firstTag = await TestDataFactory.CreateTagAsync(factory, UniqueName("FirstTag"));
            var secondTag = await TestDataFactory.CreateTagAsync(factory, UniqueName("SecondTag"));
            var firstFile = await TestDataFactory.CreateFileItemAsync(factory, tagIds: new[] { firstTag.Id });
            var secondFile = await TestDataFactory.CreateFileItemAsync(factory, tagIds: new[] { secondTag.Id });

            var response = await client.GetAsync($"/api/files?tagId={firstTag.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var files = await response.Content.ReadFromJsonAsync<List<FileItemDto>>();
            Assert.NotNull(files);
            Assert.Contains(files, item => item.Id == firstFile.Id);
            Assert.DoesNotContain(files, item => item.Id == secondFile.Id);
        }

        [Fact]
        public async Task GetFiles_WithExtension_FiltersByExtension()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var textFile = await TestDataFactory.CreateFileItemAsync(factory, extension: ".txt");
            var csvFile = await TestDataFactory.CreateFileItemAsync(factory, extension: ".csv");

            var response = await client.GetAsync("/api/files?extension=.txt");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var files = await response.Content.ReadFromJsonAsync<List<FileItemDto>>();
            Assert.NotNull(files);
            Assert.Contains(files, item => item.Id == textFile.Id);
            Assert.DoesNotContain(files, item => item.Id == csvFile.Id);
        }

        [Fact]
        public async Task GetFile_ReturnsOk_WhenFileExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("GetTag"));
            var file = await TestDataFactory.CreateFileItemAsync(factory, tagIds: new[] { tag.Id });

            var response = await client.GetAsync($"/api/files/{file.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileItemDto>();
            Assert.NotNull(dto);
            Assert.Equal(file.Id, dto.Id);
            Assert.NotNull(dto.Directory);
            Assert.Contains(dto.Tags, item => item.Id == tag.Id);
        }

        [Fact]
        public async Task GetFile_ReturnsNotFound_WhenFileDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/files/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostFile_WithValidData_ReturnsCreatedAndCreatesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("CreateTag"));
            var request = new CreateFileItemDto
            {
                Name = " api-file.txt ",
                Path = " /api/files/api-file.txt ",
                Extension = " .txt ",
                Size = 2048,
                DirectoryId = directory.Id,
                CreatedDate = new DateTime(2026, 6, 10, 22, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 10, 22, 10, 0, DateTimeKind.Utc),
                TagIds = new List<int> { tag.Id, tag.Id }
            };

            var response = await client.PostAsJsonAsync("/api/files", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileItemDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name.Trim(), dto.Name);
            Assert.Equal(request.Path.Trim(), dto.Path);
            Assert.Equal(request.Extension.Trim(), dto.Extension);
            Assert.Equal(directory.Id, dto.DirectoryId);
            Assert.Single(dto.Tags);

            var storedFile = await TestDataFactory.FindFileItemAsync(factory, dto.Id);
            Assert.NotNull(storedFile);
            Assert.Equal(request.Path.Trim(), storedFile.Path);
            Assert.Single(storedFile.Tags);
        }

        [Fact]
        public async Task PostFile_WithMissingOrInvalidRequiredFields_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = new CreateFileItemDto
            {
                Name = " ",
                Path = " ",
                Extension = " ",
                DirectoryId = null,
                CreatedDate = null,
                ModifiedDate = null
            };

            var response = await client.PostAsJsonAsync("/api/files", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostFile_WithMissingDirectoryId_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var request = ValidCreateDto();
            request.DirectoryId = 999999;

            var response = await client.PostAsJsonAsync("/api/files", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostFile_WithInvalidTagIds_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = ValidCreateDto(directory.Id);
            request.TagIds = new List<int> { 999999 };

            var response = await client.PostAsJsonAsync("/api/files", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostFile_WithNegativeSize_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = ValidCreateDto(directory.Id);
            request.Size = -1;

            var response = await client.PostAsJsonAsync("/api/files", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostFile_WithModifiedDateBeforeCreatedDate_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = ValidCreateDto(directory.Id);
            request.CreatedDate = new DateTime(2026, 6, 10, 23, 0, 0, DateTimeKind.Utc);
            request.ModifiedDate = request.CreatedDate.Value.AddMinutes(-1);

            var response = await client.PostAsJsonAsync("/api/files", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutFile_WithValidData_UpdatesAllowedFieldsAndTags()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var originalTag = await TestDataFactory.CreateTagAsync(factory, UniqueName("OriginalTag"));
            var updatedTag = await TestDataFactory.CreateTagAsync(factory, UniqueName("UpdatedTag"));
            var firstDirectory = await TestDataFactory.CreateDirectoryAsync(factory);
            var secondDirectory = await TestDataFactory.CreateDirectoryAsync(factory);
            var file = await TestDataFactory.CreateFileItemAsync(factory, firstDirectory.Id, new[] { originalTag.Id });
            var request = new UpdateFileItemDto
            {
                Id = file.Id,
                Name = "updated-file.csv",
                Path = "/updated/file.csv",
                Extension = ".csv",
                Size = 4096,
                DirectoryId = secondDirectory.Id,
                CreatedDate = new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 11, 8, 10, 0, DateTimeKind.Utc),
                TagIds = new List<int> { updatedTag.Id, updatedTag.Id }
            };

            var response = await client.PutAsJsonAsync($"/api/files/{file.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileItemDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name, dto.Name);
            Assert.Equal(request.DirectoryId, dto.DirectoryId);
            Assert.Single(dto.Tags);
            Assert.Equal(updatedTag.Id, dto.Tags[0].Id);

            var storedFile = await TestDataFactory.FindFileItemAsync(factory, file.Id);
            Assert.NotNull(storedFile);
            Assert.Equal(secondDirectory.Id, storedFile.DirectoryId);
            Assert.Single(storedFile.Tags);
            Assert.Equal(updatedTag.Id, storedFile.Tags.Single().Id);
        }

        [Fact]
        public async Task PutFile_WithIdMismatch_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var request = ValidUpdateDto(file.Id + 1, file.DirectoryId);

            var response = await client.PutAsJsonAsync($"/api/files/{file.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutFile_ForMissingFile_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var directory = await TestDataFactory.CreateDirectoryAsync(factory);
            var request = ValidUpdateDto(999999, directory.Id);

            var response = await client.PutAsJsonAsync("/api/files/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PutFile_WithInvalidDirectoryId_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var request = ValidUpdateDto(file.Id, 999999);

            var response = await client.PutAsJsonAsync($"/api/files/{file.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutFile_WithInvalidTagIds_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);
            var request = ValidUpdateDto(file.Id, file.DirectoryId);
            request.TagIds = new List<int> { 999999 };

            var response = await client.PutAsJsonAsync($"/api/files/{file.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFile_ForExistingFileWithoutChangeLogs_DeletesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemAsync(factory);

            var response = await client.DeleteAsync($"/api/files/{file.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var storedFile = await TestDataFactory.FindFileItemAsync(factory, file.Id);
            Assert.Null(storedFile);
        }

        [Fact]
        public async Task DeleteFile_ForMissingFile_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.DeleteAsync("/api/files/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFile_ForFileWithChangeLogs_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var file = await TestDataFactory.CreateFileItemWithChangeLogAsync(factory);

            var response = await client.DeleteAsync($"/api/files/{file.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedFile = await TestDataFactory.FindFileItemAsync(factory, file.Id);
            Assert.NotNull(storedFile);
            Assert.Single(storedFile.ChangeLogs);
        }

        private static CreateFileItemDto ValidCreateDto(int? directoryId = null)
        {
            return new CreateFileItemDto
            {
                Name = "valid-file.txt",
                Path = "/valid/file.txt",
                Extension = ".txt",
                Size = 100,
                DirectoryId = directoryId,
                CreatedDate = new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 11, 9, 5, 0, DateTimeKind.Utc)
            };
        }

        private static UpdateFileItemDto ValidUpdateDto(int id, int directoryId)
        {
            return new UpdateFileItemDto
            {
                Id = id,
                Name = "valid-update.txt",
                Path = "/valid/update.txt",
                Extension = ".txt",
                Size = 200,
                DirectoryId = directoryId,
                CreatedDate = new DateTime(2026, 6, 11, 10, 0, 0, DateTimeKind.Utc),
                ModifiedDate = new DateTime(2026, 6, 11, 10, 5, 0, DateTimeKind.Utc)
            };
        }

        private static string UniqueName(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
