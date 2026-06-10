using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NasIndexer.Dtos;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class AuthorizationTests
    {
        [Fact]
        public async Task AnonymousApiListAndSearchRequestsStillWork()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N");
            var tag = await TestDataFactory.CreateTagAsync(factory, $"AuthList-{token}");

            var listResponse = await client.GetAsync("/api/tags");
            var searchResponse = await client.GetAsync($"/api/tags?query=AuthList-{token}");

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
            var tags = await searchResponse.Content.ReadFromJsonAsync<List<FileTagDto>>();
            Assert.NotNull(tags);
            Assert.Contains(tags, item => item.Id == tag.Id);
        }

        [Fact]
        public async Task AnonymousApiGetByIdReturnsUnauthorized()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("AnonymousDetails"));

            var response = await client.GetAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedNonRoleUserCanGetById()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("UserDetails"));

            var response = await client.GetAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedNonRoleUserCannotPostPutOrDelete()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAuthenticatedClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("NoRole"));

            var createResponse = await client.PostAsJsonAsync("/api/tags", ValidCreateDto("NoRoleCreate"));
            var updateResponse = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", ValidUpdateDto(tag.Id, "NoRoleUpdate"));
            var deleteResponse = await client.DeleteAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task ManagerCanPostAndPutButCannotDelete()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateManagerClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("Manager"));

            var createResponse = await client.PostAsJsonAsync("/api/tags", ValidCreateDto("ManagerCreate"));
            var updateResponse = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", ValidUpdateDto(tag.Id, "ManagerUpdate"));
            var deleteResponse = await client.DeleteAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task AdminCanPostPutAndDelete()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("Admin"));

            var createResponse = await client.PostAsJsonAsync("/api/tags", ValidCreateDto("AdminCreate"));
            var updateResponse = await client.PutAsJsonAsync($"/api/tags/{tag.Id}", ValidUpdateDto(tag.Id, "AdminUpdate"));
            var deleteResponse = await client.DeleteAsync($"/api/tags/{tag.Id}");

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task FileChangeLogWriteEndpointsRemainUnavailable()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateAdminClient();
            var changeLog = await TestDataFactory.CreateFileChangeLogAsync(factory);

            var postResponse = await client.PostAsJsonAsync("/api/file-change-logs", new { fileId = changeLog.FileId });
            var putResponse = await client.PutAsJsonAsync($"/api/file-change-logs/{changeLog.Id}", new { id = changeLog.Id });
            var deleteResponse = await client.DeleteAsync($"/api/file-change-logs/{changeLog.Id}");

            Assert.Contains(postResponse.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
            Assert.Contains(putResponse.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
            Assert.Contains(deleteResponse.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }

        [Fact]
        public async Task MvcAnonymousListSearchAndHomeRemainPublic()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var homeResponse = await client.GetAsync("/");
            var listResponse = await client.GetAsync("/tags");
            var searchResponse = await client.GetAsync("/Tags/Search?query=auth");

            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        }

        [Fact]
        public async Task MvcAnonymousDetailsRedirectsToLogin()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("MvcDetails"));

            var response = await client.GetAsync($"/tags/{tag.Id}");

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Redirect });
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                Assert.NotNull(response.Headers.Location);
                Assert.Contains("/Identity/Account/Login", response.Headers.Location.ToString());
            }
        }

        [Fact]
        public async Task MvcRoleRulesApplyToWriteScreens()
        {
            using var factory = new CustomWebApplicationFactory();
            using var userClient = factory.CreateAuthenticatedClient();
            using var managerClient = factory.CreateManagerClient();
            using var adminClient = factory.CreateAdminClient();
            var tag = await TestDataFactory.CreateTagAsync(factory, UniqueName("MvcRoles"));

            var userCreateResponse = await userClient.GetAsync("/Tags/Create");
            var managerCreateResponse = await managerClient.GetAsync("/Tags/Create");
            var managerDeleteResponse = await managerClient.GetAsync($"/Tags/Delete/{tag.Id}");
            var adminDeleteResponse = await adminClient.GetAsync($"/Tags/Delete/{tag.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, userCreateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, managerCreateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, managerDeleteResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, adminDeleteResponse.StatusCode);
        }

        private static CreateFileTagDto ValidCreateDto(string prefix)
        {
            return new CreateFileTagDto
            {
                Name = UniqueName(prefix),
                Description = "Authorization test tag",
                Color = "#AABBCC"
            };
        }

        private static UpdateFileTagDto ValidUpdateDto(int id, string prefix)
        {
            return new UpdateFileTagDto
            {
                Id = id,
                Name = UniqueName(prefix),
                Description = "Authorization test tag update",
                Color = "#BBCCDD"
            };
        }

        private static string UniqueName(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
