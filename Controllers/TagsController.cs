using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class TagsController : Controller
    {
        private readonly INasRepository repository;

        public TagsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllTags());
        }

        public IActionResult Search(string? query)
        {
            return PartialView("_TagRows", repository.SearchTags(query));
        }

        public IActionResult Details(int id)
        {
            var tag = repository.GetTagById(id);

            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        public IActionResult Create()
        {
            return View(new FileTagFormViewModel
            {
                Color = "#59B8FF"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FileTagFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tag = ToFileTag(model);
            repository.AddTag(tag);
            TempData["StatusMessage"] = $"Tag {tag.Name} was created.";
            TempData["HighlightTagId"] = tag.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var tag = repository.GetTagForEdit(id);

            if (tag == null)
            {
                return NotFound();
            }

            return View(ToFormViewModel(tag));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, FileTagFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!repository.UpdateTag(ToFileTag(model)))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = $"Tag {model.Name} was updated.";
            TempData["HighlightTagId"] = model.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var tag = repository.GetTagById(id);

            if (tag == null)
            {
                return NotFound();
            }

            ViewData["DeleteBlocked"] = repository.FileTagHasFiles(id);
            return View(tag);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var tag = repository.GetTagById(id);

            if (tag == null)
            {
                return NotFound();
            }

            if (repository.FileTagHasFiles(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This tag cannot be deleted because it is assigned to one or more files.");
                return View(tag);
            }

            if (!repository.DeleteTag(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This tag could not be deleted. Refresh the page and check connected files.");
                return View(tag);
            }

            TempData["StatusMessage"] = $"Tag {tag.Name} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        private static FileTag ToFileTag(FileTagFormViewModel model)
        {
            return new FileTag
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? string.Empty,
                Color = string.IsNullOrWhiteSpace(model.Color) ? string.Empty : model.Color.Trim().ToUpperInvariant()
            };
        }

        private static FileTagFormViewModel ToFormViewModel(FileTag tag)
        {
            return new FileTagFormViewModel
            {
                Id = tag.Id,
                Name = tag.Name,
                Description = tag.Description,
                Color = tag.Color
            };
        }
    }
}
