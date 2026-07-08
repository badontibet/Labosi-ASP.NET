using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class GlobalSearchTests
    {
        [Fact]
        public async Task EmptyQuery_ReturnsFriendlyMessage()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/Search");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Enter a search term", html);
        }

        [Fact]
        public async Task KnownDataQuery_ReturnsGroupedResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();
            var token = Guid.NewGuid().ToString("N")[..10];
            var tag = await TestDataFactory.CreateTagAsync(factory, $"global-tag-{token}", "Global search tag", "#22AA99");
            var server = await TestDataFactory.CreateNasServerAsync(factory, $"global-server-{token}");
            var directory = await TestDataFactory.CreateDirectoryAsync(factory, $"global-dir-{token}", $"/global/{token}");
            var file = await TestDataFactory.CreateFileItemAsync(
                factory,
                directory.Id,
                new[] { tag.Id },
                $"global-file-{token}.txt",
                $"/global/{token}/global-file-{token}.txt",
                ".txt");
            await TestDataFactory.CreateScanJobAsync(factory, server.Id, $"/global/{token}/scan");

            var response = await client.GetAsync($"/Search?q={token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("NAS servers", html);
            Assert.Contains(server.Name, html);
            Assert.Contains("Directories", html);
            Assert.Contains(directory.Name, html);
            Assert.Contains("Files", html);
            Assert.Contains(file.Name, html);
            Assert.Contains("Tags", html);
            Assert.Contains(tag.Name, html);
            Assert.Contains("Scan jobs", html);
        }

        [Fact]
        public async Task MenuQuery_ReturnsPageResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/Search?q=files");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Pages", html);
            Assert.Contains("Files", html);
            Assert.Contains("/files", html);
        }

        [Fact]
        public async Task SearchResults_DoNotExposePasswordOrProtectedDetailLinksToAnonymousUsers()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var token = Guid.NewGuid().ToString("N")[..10];
            var secret = $"search-secret-{token}";
            var server = await TestDataFactory.CreateNasServerAsync(
                factory,
                $"safe-server-{token}",
                password: secret);

            var response = await client.GetAsync($"/Search?q={token}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains(server.Name, html);
            Assert.DoesNotContain(secret, html);
            Assert.DoesNotContain($"/servers/{server.Id}", html);
            Assert.Contains("/servers", html);
        }
    }
}
