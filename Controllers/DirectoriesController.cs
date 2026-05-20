using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.Utilities;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class DirectoriesController : Controller
    {
        private readonly INasRepository repository;

        public DirectoriesController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllDirectories());
        }

        public IActionResult Search(string? query)
        {
            return PartialView("_DirectoryRows", repository.SearchDirectories(query));
        }

        public IActionResult Details(int id)
        {
            var directory = repository.GetDirectoryById(id);

            if (directory == null)
            {
                return NotFound();
            }

            return View(directory);
        }

        public IActionResult Create()
        {
            var now = DateTime.Now;
            return View(new DirectoryItemFormViewModel
            {
                CreatedDateText = DateTimeInputParser.Format(now),
                ModifiedDateText = DateTimeInputParser.Format(now)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(DirectoryItemFormViewModel model)
        {
            if (!TryBuildDirectory(model, out var directory))
            {
                return View(model);
            }

            repository.AddDirectory(directory);
            TempData["StatusMessage"] = $"Directory {directory.Name} was created.";
            TempData["HighlightDirectoryId"] = directory.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var directory = repository.GetDirectoryForEdit(id);

            if (directory == null)
            {
                return NotFound();
            }

            return View(ToFormViewModel(directory));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, DirectoryItemFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!TryBuildDirectory(model, out var directory))
            {
                return View(model);
            }

            if (!repository.UpdateDirectory(directory))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = $"Directory {directory.Name} was updated.";
            TempData["HighlightDirectoryId"] = directory.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var directory = repository.GetDirectoryById(id);

            if (directory == null)
            {
                return NotFound();
            }

            ViewData["DeleteBlocked"] = repository.DirectoryHasChildrenOrFiles(id);
            return View(directory);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var directory = repository.GetDirectoryById(id);

            if (directory == null)
            {
                return NotFound();
            }

            if (repository.DirectoryHasChildrenOrFiles(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This directory cannot be deleted because it contains child directories or files.");
                return View(directory);
            }

            if (!repository.DeleteDirectory(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This directory could not be deleted. Refresh the page and check connected records.");
                return View(directory);
            }

            TempData["StatusMessage"] = $"Directory {directory.Name} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        public IActionResult ParentDirectoryAutocomplete(string term, int? excludeId)
        {
            var directories = string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2
                ? new List<DirectoryItem>()
                : repository.SearchDirectoriesForAutocomplete(term, excludeId, 10);

            return Json(directories.Select(directory => new
            {
                id = directory.Id,
                text = DirectoryLabel(directory)
            }));
        }

        public IActionResult ScanJobAutocomplete(string term)
        {
            var scanJobs = string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2
                ? new List<ScanJob>()
                : repository.SearchScanJobsForAutocomplete(term, 10);

            return Json(scanJobs.Select(scanJob => new
            {
                id = scanJob.Id,
                text = ScanJobLabel(scanJob)
            }));
        }

        private bool TryBuildDirectory(DirectoryItemFormViewModel model, out DirectoryItem directory)
        {
            directory = new DirectoryItem();
            var hasCreatedDate = DateTimeInputParser.TryParse(model.CreatedDateText, out var createdDate);
            var hasModifiedDate = DateTimeInputParser.TryParse(model.ModifiedDateText, out var modifiedDate);

            if (!string.IsNullOrWhiteSpace(model.CreatedDateText) && !hasCreatedDate)
            {
                ModelState.AddModelError(nameof(model.CreatedDateText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (!string.IsNullOrWhiteSpace(model.ModifiedDateText) && !hasModifiedDate)
            {
                ModelState.AddModelError(nameof(model.ModifiedDateText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (hasCreatedDate && hasModifiedDate && modifiedDate < createdDate)
            {
                ModelState.AddModelError(nameof(model.ModifiedDateText), "Modified date cannot be before created date.");
            }

            if (model.ScanJobId.HasValue && repository.GetScanJobForEdit(model.ScanJobId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.ScanJobId), "Choose an existing scan job from the autocomplete list.");
                ModelState.AddModelError(nameof(model.ScanJobLabel), "Choose an existing scan job from the autocomplete list.");
            }
            else if (!model.ScanJobId.HasValue && !string.IsNullOrWhiteSpace(model.ScanJobLabel))
            {
                ModelState.AddModelError(nameof(model.ScanJobId), "Choose an existing scan job from the autocomplete list or leave it blank.");
                ModelState.AddModelError(nameof(model.ScanJobLabel), "Choose an existing scan job from the autocomplete list or leave it blank.");
            }

            if (model.ParentDirectoryId.HasValue && repository.GetDirectoryForEdit(model.ParentDirectoryId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.ParentDirectoryId), "Choose an existing parent directory from the autocomplete list.");
                ModelState.AddModelError(nameof(model.ParentDirectoryLabel), "Choose an existing parent directory from the autocomplete list.");
            }
            else if (!model.ParentDirectoryId.HasValue && !string.IsNullOrWhiteSpace(model.ParentDirectoryLabel))
            {
                ModelState.AddModelError(nameof(model.ParentDirectoryId), "Choose an existing parent directory from the autocomplete list or leave it blank.");
                ModelState.AddModelError(nameof(model.ParentDirectoryLabel), "Choose an existing parent directory from the autocomplete list or leave it blank.");
            }

            if (model.Id > 0 && model.ParentDirectoryId == model.Id)
            {
                ModelState.AddModelError(nameof(model.ParentDirectoryId), "A directory cannot be its own parent.");
                ModelState.AddModelError(nameof(model.ParentDirectoryLabel), "A directory cannot be its own parent.");
            }

            if (repository.DirectoryParentWouldCreateCycle(model.Id, model.ParentDirectoryId))
            {
                ModelState.AddModelError(nameof(model.ParentDirectoryId), "Parent directory cannot be one of this directory's descendants.");
                ModelState.AddModelError(nameof(model.ParentDirectoryLabel), "Parent directory cannot be one of this directory's descendants.");
            }

            if (!ModelState.IsValid)
            {
                return false;
            }

            directory = new DirectoryItem
            {
                Id = model.Id,
                Name = NormalizeDirectoryName(model.Name),
                Path = NormalizeDirectoryPath(model.Path),
                ScanJobId = model.ScanJobId,
                ParentId = model.ParentDirectoryId,
                CreatedDate = createdDate,
                ModifiedDate = modifiedDate
            };

            return true;
        }

        private static DirectoryItemFormViewModel ToFormViewModel(DirectoryItem directory)
        {
            return new DirectoryItemFormViewModel
            {
                Id = directory.Id,
                Name = directory.Name,
                Path = directory.Path,
                ScanJobId = directory.ScanJobId,
                ScanJobLabel = directory.ScanJob == null ? string.Empty : ScanJobLabel(directory.ScanJob),
                ParentDirectoryId = directory.ParentId,
                ParentDirectoryLabel = directory.Parent == null ? string.Empty : DirectoryLabel(directory.Parent),
                CreatedDateText = DateTimeInputParser.Format(directory.CreatedDate),
                ModifiedDateText = DateTimeInputParser.Format(directory.ModifiedDate)
            };
        }

        private static string DirectoryLabel(DirectoryItem directory)
        {
            return $"{directory.Name} - {directory.Path}";
        }

        private static string ScanJobLabel(ScanJob scanJob)
        {
            var serverName = scanJob.NasServer?.Name ?? "Unknown server";
            return $"Job {scanJob.Id} - {serverName} - {scanJob.RootPath}";
        }

        private static string NormalizeDirectoryName(string name)
        {
            var trimmedName = name.Trim();

            if (trimmedName.Length <= DirectoryItemFormViewModel.NameSoftLimit)
            {
                return trimmedName;
            }

            return trimmedName[..(DirectoryItemFormViewModel.NameSoftLimit - 3)] + "...";
        }

        private static string NormalizeDirectoryPath(string path)
        {
            var trimmedPath = path.Trim();

            return trimmedPath.Length <= DirectoryItemFormViewModel.PathSoftLimit
                ? trimmedPath
                : trimmedPath[..DirectoryItemFormViewModel.PathSoftLimit];
        }
    }
}
