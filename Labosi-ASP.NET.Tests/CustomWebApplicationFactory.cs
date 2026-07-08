using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NasIndexer.Controllers.Api;
using NasIndexer.Data;

namespace Labosi_ASP.NET.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<FileTagsApiController>
    {
        public string AttachmentStorageRoot { get; } = Path.Combine(Path.GetTempPath(), "nas-indexer-tests", Guid.NewGuid().ToString("N"));
        public string LogStorageRoot { get; } = Path.Combine(Path.GetTempPath(), "nas-indexer-test-logs", Guid.NewGuid().ToString("N"));

        public HttpClient CreateAuthenticatedClient(params string[] roles)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, $"test-user-{Guid.NewGuid():N}");

            if (roles.Length > 0)
            {
                client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
            }

            return client;
        }

        public HttpClient CreateManagerClient()
        {
            return CreateAuthenticatedClient("Manager");
        }

        public HttpClient CreateAdminClient()
        {
            return CreateAuthenticatedClient("Admin");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FileAttachmentStorage:RootPath"] = AttachmentStorageRoot,
                    ["AppFileLogging:RootPath"] = LogStorageRoot
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<NasIndexerDbContext>>();
                services.RemoveAll<NasIndexerDbContext>();

                services.AddSingleton(_ =>
                {
                    var connection = new SqliteConnection("Data Source=:memory:");
                    connection.Open();
                    return connection;
                });

                services.AddDbContext<NasIndexerDbContext>((serviceProvider, options) =>
                {
                    options.UseSqlite(serviceProvider.GetRequiredService<SqliteConnection>());
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && Directory.Exists(AttachmentStorageRoot))
            {
                Directory.Delete(AttachmentStorageRoot, recursive: true);
            }

            if (disposing && Directory.Exists(LogStorageRoot))
            {
                Directory.Delete(LogStorageRoot, recursive: true);
            }
        }
    }
}
