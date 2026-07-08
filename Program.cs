using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NasIndexer.Data;
using NasIndexer.Middleware;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var connectionString = builder.Configuration.GetConnectionString("NasIndexerDbContext")
    ?? "Data Source=nas-indexer.db";

builder.Services.AddDbContext<NasIndexerDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDefaultIdentity<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<NasIndexerDbContext>();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// Configure with:
// dotnet user-secrets set "Authentication:Google:ClientId" "..."
// dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

builder.Services.AddScoped<INasRepository, EfNasRepository>();
builder.Services.AddScoped<FileAttachmentStorageService>();
builder.Services.AddSingleton<AppFileLogger>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NasIndexerDbContext>();
    db.Database.Migrate();
    NasIndexerDbInitializer.Seed(db);
    await IdentitySeedData.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestFileLoggingMiddleware>();

app.MapControllers();
app.MapRazorPages();

app.MapControllerRoute(
    name: "DashboardRoute",
    pattern: "dashboard",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "ServersIndex",
    pattern: "servers",
    defaults: new { controller = "NasServers", action = "Index" });

app.MapControllerRoute(
    name: "ServersDetails",
    pattern: "servers/{id:int}",
    defaults: new { controller = "NasServers", action = "Details" });

app.MapControllerRoute(
    name: "ScanJobsIndex",
    pattern: "scan-jobs",
    defaults: new { controller = "ScanJobs", action = "Index" });

app.MapControllerRoute(
    name: "ScanJobsDetails",
    pattern: "scan-jobs/{id:int}",
    defaults: new { controller = "ScanJobs", action = "Details" });

app.MapControllerRoute(
    name: "DirectoriesIndex",
    pattern: "directories",
    defaults: new { controller = "Directories", action = "Index" });

app.MapControllerRoute(
    name: "DirectoriesDetails",
    pattern: "directories/{id:int}",
    defaults: new { controller = "Directories", action = "Details" });

app.MapControllerRoute(
    name: "FilesIndex",
    pattern: "files",
    defaults: new { controller = "FileItems", action = "Index" });

app.MapControllerRoute(
    name: "FilesDetails",
    pattern: "files/{id:int}",
    defaults: new { controller = "FileItems", action = "Details" });

app.MapControllerRoute(
    name: "TagsIndex",
    pattern: "tags",
    defaults: new { controller = "Tags", action = "Index" });

app.MapControllerRoute(
    name: "TagsDetails",
    pattern: "tags/{id:int}",
    defaults: new { controller = "Tags", action = "Details" });

app.MapControllerRoute(
    name: "AdminsIndex",
    pattern: "admins",
    defaults: new { controller = "Admins", action = "Index" });

app.MapControllerRoute(
    name: "AdminsDetails",
    pattern: "admins/{id:int}",
    defaults: new { controller = "Admins", action = "Details" });

app.MapControllerRoute(
    name: "LegacyNasServersIndex",
    pattern: "NasServer",
    defaults: new { controller = "NasServers", action = "Index" });

app.MapControllerRoute(
    name: "LegacyNasServersDetails",
    pattern: "NasServer/Details/{id:int}",
    defaults: new { controller = "NasServers", action = "Details" });

app.MapControllerRoute(
    name: "LegacyScanJobsIndex",
    pattern: "ScanJob",
    defaults: new { controller = "ScanJobs", action = "Index" });

app.MapControllerRoute(
    name: "LegacyScanJobsDetails",
    pattern: "ScanJob/Details/{id:int}",
    defaults: new { controller = "ScanJobs", action = "Details" });

app.MapControllerRoute(
    name: "LegacyDirectoriesIndex",
    pattern: "DirectoryItem",
    defaults: new { controller = "Directories", action = "Index" });

app.MapControllerRoute(
    name: "LegacyDirectoriesDetails",
    pattern: "DirectoryItem/Details/{id:int}",
    defaults: new { controller = "Directories", action = "Details" });

app.MapControllerRoute(
    name: "LegacyFilesIndex",
    pattern: "FileItem",
    defaults: new { controller = "FileItems", action = "Index" });

app.MapControllerRoute(
    name: "LegacyFilesDetails",
    pattern: "FileItem/Details/{id:int}",
    defaults: new { controller = "FileItems", action = "Details" });

app.MapControllerRoute(
    name: "LegacyTagsIndex",
    pattern: "FileTag",
    defaults: new { controller = "Tags", action = "Index" });

app.MapControllerRoute(
    name: "LegacyTagsDetails",
    pattern: "FileTag/Details/{id:int}",
    defaults: new { controller = "Tags", action = "Details" });

app.MapControllerRoute(
    name: "LegacyAdminsIndex",
    pattern: "SystemAdmin",
    defaults: new { controller = "Admins", action = "Index" });

app.MapControllerRoute(
    name: "LegacyAdminsDetails",
    pattern: "SystemAdmin/Details/{id:int}",
    defaults: new { controller = "Admins", action = "Details" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
