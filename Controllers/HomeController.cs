using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class HomeController : Controller
    {
        private readonly INasRepository repository;

        public HomeController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            var servers = repository.GetAllNasServers();
            var scanJobs = repository.GetAllScanJobs();
            var directories = repository.GetAllDirectories();
            var files = repository.GetAllFiles();
            var tags = repository.GetAllTags();

            var viewModel = new DashboardViewModel
            {
                Servers = servers,
                ScanJobs = scanJobs,
                Directories = directories,
                Files = files,
                Tags = tags,
                RecentChanges = files
                    .SelectMany(file => file.ChangeLogs)
                    .OrderByDescending(change => change.Timestamp)
                    .Take(5)
                    .ToList(),
                WarningScanJobs = scanJobs
                    .Where(job => job.Status == ScanStatus.Failed || job.Status == ScanStatus.Running)
                    .OrderByDescending(job => job.StartTime)
                    .ToList(),
                TotalServers = servers.Count,
                ActiveServers = servers.Count(server => server.IsActive),
                InactiveServers = servers.Count(server => !server.IsActive),
                TotalScanJobs = scanJobs.Count,
                CompletedScanJobs = scanJobs.Count(job => job.Status == ScanStatus.Completed),
                RunningScanJobs = scanJobs.Count(job => job.Status == ScanStatus.Running),
                FailedScanJobs = scanJobs.Count(job => job.Status == ScanStatus.Failed),
                TotalDirectories = directories.Count,
                TotalFiles = files.Count,
                TotalTags = tags.Count
            };

            return View(viewModel);
        }
    }
}
