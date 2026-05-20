using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.Utilities;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class ScanJobsController : Controller
    {
        private readonly INasRepository repository;

        public ScanJobsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllScanJobs());
        }

        public IActionResult Search(string? query)
        {
            return PartialView("_ScanJobRows", repository.SearchScanJobs(query));
        }

        public IActionResult Details(int id)
        {
            var scanJob = repository.GetScanJobById(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            return View(scanJob);
        }

        public IActionResult Create()
        {
            return View(new ScanJobFormViewModel
            {
                Status = ScanStatus.Pending,
                StartTimeText = DateTimeInputParser.Format(DateTime.Now),
                TotalFiles = 0,
                ProcessedFiles = 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ScanJobFormViewModel model)
        {
            if (!TryBuildScanJob(model, out var scanJob))
            {
                return View(model);
            }

            repository.AddScanJob(scanJob);
            TempData["StatusMessage"] = $"Scan job {scanJob.Id} was created.";
            TempData["HighlightScanJobId"] = scanJob.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var scanJob = repository.GetScanJobForEdit(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            return View(ToFormViewModel(scanJob));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ScanJobFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!TryBuildScanJob(model, out var scanJob))
            {
                return View(model);
            }

            if (!repository.UpdateScanJob(scanJob))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = $"Scan job {scanJob.Id} was updated.";
            TempData["HighlightScanJobId"] = scanJob.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var scanJob = repository.GetScanJobById(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            ViewData["DeleteBlocked"] = repository.ScanJobHasDirectories(id);
            return View(scanJob);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var scanJob = repository.GetScanJobById(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            if (repository.ScanJobHasDirectories(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This scan job cannot be deleted because it has scanned directories.");
                return View(scanJob);
            }

            if (!repository.DeleteScanJob(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This scan job could not be deleted. Refresh the page and check connected directories.");
                return View(scanJob);
            }

            TempData["StatusMessage"] = $"Scan job {id} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        public IActionResult NasServerAutocomplete(string term)
        {
            var servers = string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2
                ? new List<NasServer>()
                : repository.SearchNasServers(term, 10);

            return Json(servers.Select(server => new
            {
                id = server.Id,
                text = server.Name
            }));
        }

        private bool TryBuildScanJob(ScanJobFormViewModel model, out ScanJob scanJob)
        {
            scanJob = new ScanJob();
            var hasStartTime = DateTimeInputParser.TryParse(model.StartTimeText, out var startTime);
            var hasEndTime = DateTimeInputParser.TryParseOptional(model.EndTimeText, out var endTime);

            if (model.NasServerId.HasValue && repository.GetNasServerById(model.NasServerId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.NasServerId), "Choose an existing NAS server from the autocomplete list.");
                ModelState.AddModelError(nameof(model.NasServerLabel), "Choose an existing NAS server from the autocomplete list.");
            }

            if (!string.IsNullOrWhiteSpace(model.StartTimeText) && !hasStartTime)
            {
                ModelState.AddModelError(nameof(model.StartTimeText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (!hasEndTime)
            {
                ModelState.AddModelError(nameof(model.EndTimeText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (!ModelState.IsValid)
            {
                return false;
            }

            scanJob = new ScanJob
            {
                Id = model.Id,
                NasServerId = model.NasServerId!.Value,
                Status = model.Status,
                StartTime = startTime,
                EndTime = endTime,
                RootPath = model.RootPath.Trim(),
                TotalFiles = model.TotalFiles,
                ProcessedFiles = model.ProcessedFiles
            };

            return true;
        }

        private static ScanJobFormViewModel ToFormViewModel(ScanJob scanJob)
        {
            return new ScanJobFormViewModel
            {
                Id = scanJob.Id,
                NasServerId = scanJob.NasServerId,
                NasServerLabel = scanJob.NasServer.Name,
                Status = scanJob.Status,
                StartTimeText = DateTimeInputParser.Format(scanJob.StartTime),
                EndTimeText = DateTimeInputParser.Format(scanJob.EndTime),
                RootPath = scanJob.RootPath,
                TotalFiles = scanJob.TotalFiles,
                ProcessedFiles = scanJob.ProcessedFiles
            };
        }
    }
}
