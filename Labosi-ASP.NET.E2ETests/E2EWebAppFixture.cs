using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NasIndexer.Data;
using NasIndexer.Model;

namespace Labosi_ASP.NET.E2ETests
{
    public class E2EWebAppFixture : IAsyncLifetime
    {
        private readonly string tempRoot = Path.Combine(Path.GetTempPath(), "nas-indexer-e2e", Guid.NewGuid().ToString("N"));
        private Process? appProcess;

        public string AdminEmail { get; } = $"admin-{Guid.NewGuid():N}@example.test";
        public string AdminPassword { get; } = "E2E-Admin-123!";
        public string BaseUrl { get; private set; } = string.Empty;
        public string DatabasePath => Path.Combine(tempRoot, "nas-indexer-e2e.db");
        public string UploadRoot => Path.Combine(tempRoot, "uploads");
        public string LogRoot => Path.Combine(tempRoot, "logs");

        public async Task InitializeAsync()
        {
            Directory.CreateDirectory(tempRoot);
            await SeedDatabaseAsync();

            var port = GetAvailablePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            StartApplicationProcess();
            await WaitForApplicationAsync();
        }

        public Task DisposeAsync()
        {
            if (appProcess is { HasExited: false })
            {
                appProcess.Kill(entireProcessTree: true);
                appProcess.WaitForExit(10000);
            }

            appProcess?.Dispose();
            SqliteConnection.ClearAllPools();

            for (var attempt = 0; attempt < 10 && Directory.Exists(tempRoot); attempt++)
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch (IOException) when (attempt == 9)
                {
                    return Task.CompletedTask;
                }
                catch (IOException) when (attempt < 9)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(1000);
                }
            }

            return Task.CompletedTask;
        }

        private async Task SeedDatabaseAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddDbContext<NasIndexerDbContext>(options =>
                options.UseSqlite($"Data Source={DatabasePath}"));
            services.AddIdentity<AppUser, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<NasIndexerDbContext>();

            await using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
            await dbContext.Database.MigrateAsync();
            NasIndexerDbInitializer.Seed(dbContext);

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            if (!await roleManager.RoleExistsAsync(IdentitySeedData.AdminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(IdentitySeedData.AdminRole));
            }

            var user = new AppUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                OIB = "12345678901",
                JMBG = "1234567890123"
            };

            var createResult = await userManager.CreateAsync(user, AdminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }

            var roleResult = await userManager.AddToRoleAsync(user, IdentitySeedData.AdminRole);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(error => error.Description)));
            }
        }

        private void StartApplicationProcess()
        {
            var projectPath = Path.Combine(GetRepositoryRoot(), "Labosi-ASP.NET.csproj");
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --project \"{projectPath}\"",
                WorkingDirectory = GetRepositoryRoot(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "E2E";
            startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
            startInfo.Environment["ConnectionStrings__NasIndexerDbContext"] = $"Data Source={DatabasePath}";
            startInfo.Environment["FileAttachmentStorage__RootPath"] = UploadRoot;
            startInfo.Environment["AppFileLogging__RootPath"] = LogRoot;
            startInfo.Environment["IdentitySeed__AdminEmails__0"] = AdminEmail;

            appProcess = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start the NAS Indexer application process.");
        }

        private async Task WaitForApplicationAsync()
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };

            var deadline = DateTimeOffset.UtcNow.AddSeconds(40);
            Exception? lastError = null;

            while (DateTimeOffset.UtcNow < deadline)
            {
                if (appProcess is { HasExited: true })
                {
                    var output = await appProcess.StandardOutput.ReadToEndAsync();
                    var error = await appProcess.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"Application process exited early. Output: {output} Error: {error}");
                }

                try
                {
                    var response = await client.GetAsync(BaseUrl);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return;
                    }
                }
                catch (Exception exception)
                {
                    lastError = exception;
                }

                await Task.Delay(500);
            }

            throw new TimeoutException($"Application did not respond at {BaseUrl}. Last error: {lastError?.Message}");
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string GetRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Labosi-ASP.NET.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new InvalidOperationException("Could not locate the repository root.");
        }
    }
}
