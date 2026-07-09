using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NasIndexer.Data;
using NasIndexer.Model;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class HomeController : Controller
    {
        private readonly NasIndexerDbContext context;

        public HomeController(NasIndexerDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var servers = await context.NasServers
                .AsNoTracking()
                .OrderBy(server => server.Name)
                .ToListAsync();

            var scanJobs = await context.ScanJobs
                .AsNoTracking()
                .Include(scanJob => scanJob.NasServer)
                .OrderByDescending(scanJob => scanJob.StartTime)
                .ToListAsync();

            var recentChanges = await context.FileChangeLogs
                .AsNoTracking()
                .Include(changeLog => changeLog.File)
                .OrderByDescending(changeLog => changeLog.Timestamp)
                .Take(8)
                .ToListAsync();

            var totalDirectories = await context.DirectoryItems.AsNoTracking().CountAsync();
            var totalFiles = await context.FileItems.AsNoTracking().CountAsync();
            var totalTags = await context.FileTags.AsNoTracking().CountAsync();
            var latestFileModified = await context.FileItems
                .AsNoTracking()
                .OrderByDescending(file => file.ModifiedDate)
                .Select(file => (DateTime?)file.ModifiedDate)
                .FirstOrDefaultAsync();

            var viewModel = new DashboardViewModel
            {
                Servers = servers,
                ScanJobs = scanJobs,
                RecentChanges = recentChanges,
                WarningScanJobs = scanJobs
                    .Where(job => job.Status == ScanStatus.Failed || job.Status == ScanStatus.Running)
                    .OrderBy(job => job.Status == ScanStatus.Failed ? 0 : 1)
                    .ThenByDescending(job => job.StartTime)
                    .ToList(),
                TotalServers = servers.Count,
                ActiveServers = servers.Count(server => server.IsActive),
                InactiveServers = servers.Count(server => !server.IsActive),
                TotalScanJobs = scanJobs.Count,
                CompletedScanJobs = scanJobs.Count(job => job.Status == ScanStatus.Completed),
                RunningScanJobs = scanJobs.Count(job => job.Status == ScanStatus.Running),
                FailedScanJobs = scanJobs.Count(job => job.Status == ScanStatus.Failed),
                TotalDirectories = totalDirectories,
                TotalFiles = totalFiles,
                TotalTags = totalTags
            };

            viewModel.ScanWarningCount = viewModel.WarningScanJobs.Count;
            viewModel.LastUpdatedUtc = Latest(
                recentChanges.Select(changeLog => (DateTime?)changeLog.Timestamp)
                    .Concat(scanJobs.Select(job => (DateTime?)(job.EndTime ?? job.StartTime)))
                    .Concat(servers.Select(server => (DateTime?)server.LastScan))
                    .Append(latestFileModified));
            ApplyHealth(viewModel);

            return View(viewModel);
        }

        private static DateTime? Latest(IEnumerable<DateTime?> values)
        {
            var concreteValues = values
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            return concreteValues.Count == 0 ? null : concreteValues.Max();
        }

        private static void ApplyHealth(DashboardViewModel viewModel)
        {
            if (viewModel.FailedScanJobs > 0)
            {
                viewModel.HealthStatus = "Critical";
                viewModel.HealthStatusClass = "status-bad";
                viewModel.HealthSummary = $"{viewModel.FailedScanJobs} failed scan job(s) need review.";
                return;
            }

            if (viewModel.RunningScanJobs > 0 || viewModel.InactiveServers > 0 || viewModel.ActiveServers == 0)
            {
                viewModel.HealthStatus = "Warning";
                viewModel.HealthStatusClass = "status-running";
                viewModel.HealthSummary = $"{viewModel.RunningScanJobs} running scan(s), {viewModel.InactiveServers} inactive server(s).";
                return;
            }

            viewModel.HealthStatus = "Healthy";
            viewModel.HealthStatusClass = "status-good";
            viewModel.HealthSummary = "Active servers are available and no scan failures are recorded.";
        }
    }
}
