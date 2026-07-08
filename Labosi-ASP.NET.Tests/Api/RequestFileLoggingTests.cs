using System.Net;
using Xunit;

namespace Labosi_ASP.NET.Tests.Api
{
    public class RequestFileLoggingTests
    {
        [Fact]
        public async Task CompletedRequest_WritesSafeRequestLogLine()
        {
            using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/tags?query=logging-safe");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var logContent = await ReadLogContentAsync(factory);
            Assert.Contains("level=INFO", logContent);
            Assert.Contains("method=GET", logContent);
            Assert.Contains("path=/api/tags", logContent);
            Assert.Contains("query=?query=logging-safe", logContent);
            Assert.Contains("status=200", logContent);
            Assert.Contains("user=anonymous", logContent);
        }

        [Fact]
        public async Task CompletedRequest_RedactsSensitiveQueryString()
        {
            using var factory = new CustomWebApplicationFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/tags?password=secret&ClientSecret=secret");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var logContent = await ReadLogContentAsync(factory);
            Assert.Contains("query=[redacted]", logContent);
            Assert.DoesNotContain("password=secret", logContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ClientSecret=secret", logContent, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<string> ReadLogContentAsync(CustomWebApplicationFactory factory)
        {
            var logFile = Assert.Single(Directory.GetFiles(factory.LogStorageRoot, "app-*.log"));
            return await File.ReadAllTextAsync(logFile);
        }
    }
}
