using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.Utilities;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class FileItemsController : Controller
    {
        private readonly INasRepository repository;

        public FileItemsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllFiles());
        }

        public IActionResult Search(string? query)
        {
            return PartialView("_FileRows", repository.SearchFiles(query));
        }

        public IActionResult Details(int id)
        {
            var file = repository.GetFileById(id);

            if (file == null)
            {
                return NotFound();
            }

            return View(file);
        }

        public IActionResult Create()
        {
            var now = DateTime.Now;
            return View(PrepareFileForm(new FileItemFormViewModel
            {
                CreatedDateText = DateTimeInputParser.Format(now),
                ModifiedDateText = DateTimeInputParser.Format(now)
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FileItemFormViewModel model)
        {
            if (!TryBuildFile(model, out var file))
            {
                return View(PrepareFileForm(model));
            }

            repository.AddFile(file, model.SelectedTagIds);
            TempData["StatusMessage"] = $"File {file.Name} was created.";
            TempData["HighlightFileId"] = file.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var file = repository.GetFileForEdit(id);

            if (file == null)
            {
                return NotFound();
            }

            return View(PrepareFileForm(ToFormViewModel(file)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, FileItemFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!TryBuildFile(model, out var file))
            {
                return View(PrepareFileForm(model));
            }

            if (!repository.UpdateFile(file, model.SelectedTagIds))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = $"File {file.Name} was updated.";
            TempData["HighlightFileId"] = file.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var file = repository.GetFileById(id);

            if (file == null)
            {
                return NotFound();
            }

            ViewData["DeleteBlocked"] = repository.FileItemHasChangeLogs(id);
            return View(file);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var file = repository.GetFileById(id);

            if (file == null)
            {
                return NotFound();
            }

            if (repository.FileItemHasChangeLogs(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This file cannot be deleted because it has change log entries.");
                return View(file);
            }

            if (!repository.DeleteFile(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This file could not be deleted. Refresh the page and check change logs.");
                return View(file);
            }

            TempData["StatusMessage"] = $"File {file.Name} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        public IActionResult DirectoryAutocomplete(string term)
        {
            var directories = string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2
                ? new List<DirectoryItem>()
                : repository.SearchDirectoriesForAutocomplete(term, null, 10);

            return Json(directories.Select(directory => new
            {
                id = directory.Id,
                text = DirectoryLabel(directory)
            }));
        }

        private bool TryBuildFile(FileItemFormViewModel model, out FileItem file)
        {
            file = new FileItem();
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

            if (model.DirectoryId.HasValue && repository.GetDirectoryForEdit(model.DirectoryId.Value) == null)
            {
                ModelState.AddModelError(nameof(model.DirectoryId), "Choose an existing directory from the autocomplete list.");
                ModelState.AddModelError(nameof(model.DirectoryLabel), "Choose an existing directory from the autocomplete list.");
            }

            var validTagIds = repository.GetAllTags().Select(tag => tag.Id).ToHashSet();
            var invalidTagIds = model.SelectedTagIds.Where(tagId => !validTagIds.Contains(tagId)).ToList();
            if (invalidTagIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedTagIds), "One or more selected tags do not exist.");
            }

            if (!ModelState.IsValid)
            {
                return false;
            }

            file = new FileItem
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                Path = model.Path.Trim(),
                Extension = model.Extension?.Trim() ?? string.Empty,
                Size = model.Size,
                DirectoryId = model.DirectoryId!.Value,
                CreatedDate = createdDate,
                ModifiedDate = modifiedDate
            };

            model.SelectedTagIds = model.SelectedTagIds.Distinct().ToList();
            return true;
        }

        private FileItemFormViewModel PrepareFileForm(FileItemFormViewModel model)
        {
            var selectedTagIds = model.SelectedTagIds.ToHashSet();
            model.AvailableTags = repository.GetAllTags()
                .Select(tag => new TagCheckboxViewModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Color = tag.Color,
                    IsSelected = selectedTagIds.Contains(tag.Id)
                })
                .ToList();

            return model;
        }

        private static FileItemFormViewModel ToFormViewModel(FileItem file)
        {
            return new FileItemFormViewModel
            {
                Id = file.Id,
                Name = file.Name,
                Path = file.Path,
                Extension = file.Extension,
                Size = file.Size,
                DirectoryId = file.DirectoryId,
                DirectoryLabel = DirectoryLabel(file.Directory),
                CreatedDateText = DateTimeInputParser.Format(file.CreatedDate),
                ModifiedDateText = DateTimeInputParser.Format(file.ModifiedDate),
                SelectedTagIds = file.Tags.Select(tag => tag.Id).ToList()
            };
        }

        private static string DirectoryLabel(DirectoryItem directory)
        {
            return $"{directory.Name} - {directory.Path}";
        }
    }
}
