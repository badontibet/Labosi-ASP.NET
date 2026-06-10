using System.Net;
using System.Net.Http.Json;
using NasIndexer.Dtos;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class FileTagsApiTests
    {
        [Fact]
        public async Task GetTags_ReturnsOkAndJsonCollection()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("Collection"));

            var response = await client.GetAsync("/api/tags");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var tags = await response.Content.ReadFromJsonAsync<List<FileTagDto>>();
            Assert.NotNull(tags);
            Assert.Contains(tags, item => item.Id == tag.Id);
        }

        [Fact]
        public async Task GetTags_WithQuery_FiltersResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var matchingTag = await TestDataFactory.CreateTagAsync(factory, $"Match-{token}");
            var nonMatchingTag = await TestDataFactory.CreateTagAsync(factory, $"Other-{token}");

            var response = await client.GetAsync($"/api/tags?query=Match-{token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var tags = await response.Content.ReadFromJsonAsync<List<FileTagDto>>();
            Assert.NotNull(tags);
            Assert.Contains(tags, item => item.Id == matchingTag.Id);
            Assert.DoesNotContain(tags, item => item.Id == nonMatchingTag.Id);
        }

        [Fact]
        public async Task GetTag_ReturnsOk_WhenTagExists()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("GetById"));

            var response = await client.GetAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileTagDto>();
            Assert.NotNull(dto);
            Assert.Equal(tag.Id, dto.Id);
            Assert.Equal(tag.Name, dto.Name);
        }

        [Fact]
        public async Task GetTag_ReturnsNotFound_WhenTagDoesNotExist()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();

            var response = await client.GetAsync("/api/tags/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostTag_WithValidData_ReturnsCreatedAndCreatesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new CreateFileTagDto
            {
                Name = $"  {UniqueName("Created")}  ",
                Description = " Created through integration test ",
                Color = "#aabbcc"
            };

            var response = await client.PostAsJsonAsync("/api/tags", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileTagDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name.Trim(), dto.Name);
            Assert.Equal(request.Description.Trim(), dto.Description);
            Assert.Equal("#AABBCC", dto.Color);

            var storedTag = await TestDataFactory.FindTagAsync(factory, dto.Id);
            Assert.NotNull(storedTag);
            Assert.Equal(dto.Name, storedTag.Name);
        }

        [Fact]
        public async Task PostTag_WithInvalidData_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new CreateFileTagDto
            {
                Name = " ",
                Description = "Invalid whitespace name",
                Color = "#AABBCC"
            };

            var response = await client.PostAsJsonAsync("/api/tags", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutTag_WithValidData_UpdatesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("BeforeUpdate"));
            var request = new UpdateFileTagDto
            {
                Id = tag.Id,
                Name = UniqueName("AfterUpdate"),
                Description = "Updated through integration test",
                Color = "#112233"
            };

            var response = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<FileTagDto>();
            Assert.NotNull(dto);
            Assert.Equal(request.Name, dto.Name);
            Assert.Equal(request.Description, dto.Description);
            Assert.Equal(request.Color, dto.Color);

            var storedTag = await TestDataFactory.FindTagAsync(factory, tag.Id);
            Assert.NotNull(storedTag);
            Assert.Equal(request.Name, storedTag.Name);
        }

        [Fact]
        public async Task PutTag_WithIdMismatch_ReturnsBadRequest()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("Mismatch"));
            var request = new UpdateFileTagDto
            {
                Id = tag.Id + 1,
                Name = UniqueName("MismatchUpdate"),
                Description = "Mismatched id",
                Color = "#334455"
            };

            var response = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PutTag_ForMissingTag_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var request = new UpdateFileTagDto
            {
                Id = 999999,
                Name = UniqueName("MissingUpdate"),
                Description = "Missing tag",
                Color = "#334455"
            };

            var response = await client.PutAsJsonAsync("/api/tags/999999", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTag_ForExistingUnassignedTag_DeletesRecord()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("Delete"));

            var response = await client.DeleteAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var storedTag = await TestDataFactory.FindTagAsync(factory, tag.Id);
            Assert.Null(storedTag);
        }

        [Fact]
        public async Task DeleteTag_ForMissingTag_ReturnsNotFound()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();

            var response = await client.DeleteAsync("/api/tags/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTag_ForAssignedTag_ReturnsConflict()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var tag = await TestDataFactory.CreateAssignedTagAsync(factory, UniqueName("AssignedDelete"));

            var response = await client.DeleteAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var storedTag = await TestDataFactory.FindTagAsync(factory, tag.Id);
            Assert.NotNull(storedTag);
            Assert.Single(storedTag.Files);
        }

        private static string UniqueName(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
