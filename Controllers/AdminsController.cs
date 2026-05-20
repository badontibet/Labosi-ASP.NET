using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.Utilities;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class AdminsController : Controller
    {
        private readonly INasRepository repository;

        public AdminsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllAdmins());
        }

        public IActionResult Search(string? query)
        {
            return PartialView("_AdminRows", repository.SearchAdmins(query));
        }

        public IActionResult Details(int id)
        {
            var admin = repository.GetAdminById(id);

            if (admin == null)
            {
                return NotFound();
            }

            return View(admin);
        }

        public IActionResult Create()
        {
            var now = DateTime.Now;
            return View(PrepareAdminForm(new SystemAdminFormViewModel
            {
                CreatedDateText = DateTimeInputParser.Format(now),
                LastLoginText = DateTimeInputParser.Format(now)
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SystemAdminFormViewModel model)
        {
            if (!TryBuildAdmin(model, out var admin))
            {
                return View(PrepareAdminForm(model));
            }

            repository.AddAdmin(admin, model.SelectedNasServerIds);
            TempData["StatusMessage"] = $"System admin {admin.Username} was created.";
            TempData["HighlightAdminId"] = admin.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var admin = repository.GetAdminForEdit(id);

            if (admin == null)
            {
                return NotFound();
            }

            return View(PrepareAdminForm(ToFormViewModel(admin)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, SystemAdminFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!TryBuildAdmin(model, out var admin))
            {
                return View(PrepareAdminForm(model));
            }

            if (!repository.UpdateAdmin(admin, model.SelectedNasServerIds))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = $"System admin {admin.Username} was updated.";
            TempData["HighlightAdminId"] = admin.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var admin = repository.GetAdminById(id);

            if (admin == null)
            {
                return NotFound();
            }

            ViewData["DeleteBlocked"] = repository.SystemAdminHasManagedServers(id);
            return View(admin);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var admin = repository.GetAdminById(id);

            if (admin == null)
            {
                return NotFound();
            }

            if (repository.SystemAdminHasManagedServers(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This system admin cannot be deleted because managed NAS servers are assigned.");
                return View(admin);
            }

            if (!repository.DeleteAdmin(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This system admin could not be deleted. Refresh the page and check managed servers.");
                return View(admin);
            }

            TempData["StatusMessage"] = $"System admin {admin.Username} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        private bool TryBuildAdmin(SystemAdminFormViewModel model, out SystemAdmin admin)
        {
            admin = new SystemAdmin();
            var hasCreatedDate = DateTimeInputParser.TryParse(model.CreatedDateText, out var createdDate);
            var hasLastLogin = DateTimeInputParser.TryParse(model.LastLoginText, out var lastLogin);

            if (!string.IsNullOrWhiteSpace(model.CreatedDateText) && !hasCreatedDate)
            {
                ModelState.AddModelError(nameof(model.CreatedDateText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (!string.IsNullOrWhiteSpace(model.LastLoginText) && !hasLastLogin)
            {
                ModelState.AddModelError(nameof(model.LastLoginText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (hasCreatedDate && hasLastLogin && lastLogin < createdDate)
            {
                ModelState.AddModelError(nameof(model.LastLoginText), "Last login cannot be before created date.");
            }

            var validServerIds = repository.GetAllNasServers().Select(server => server.Id).ToHashSet();
            var invalidServerIds = model.SelectedNasServerIds.Where(serverId => !validServerIds.Contains(serverId)).ToList();
            if (invalidServerIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedNasServerIds), "One or more selected NAS servers do not exist.");
            }

            if (!ModelState.IsValid)
            {
                return false;
            }

            admin = new SystemAdmin
            {
                Id = model.Id,
                Username = model.Username.Trim(),
                Email = model.Email.Trim(),
                Role = model.Role.Trim(),
                CreatedDate = createdDate,
                LastLogin = lastLogin
            };

            model.SelectedNasServerIds = model.SelectedNasServerIds.Distinct().ToList();
            return true;
        }

        private SystemAdminFormViewModel PrepareAdminForm(SystemAdminFormViewModel model)
        {
            var selectedServerIds = model.SelectedNasServerIds.ToHashSet();
            model.AvailableNasServers = repository.GetAllNasServers()
                .Select(server => new NasServerCheckboxViewModel
                {
                    Id = server.Id,
                    Name = server.Name,
                    Address = $"{server.IpAddress}:{server.Port}",
                    IsSelected = selectedServerIds.Contains(server.Id)
                })
                .ToList();

            return model;
        }

        private static SystemAdminFormViewModel ToFormViewModel(SystemAdmin admin)
        {
            return new SystemAdminFormViewModel
            {
                Id = admin.Id,
                Username = admin.Username,
                Email = admin.Email,
                Role = admin.Role,
                CreatedDateText = DateTimeInputParser.Format(admin.CreatedDate),
                LastLoginText = DateTimeInputParser.Format(admin.LastLogin),
                SelectedNasServerIds = admin.ManagedServers.Select(server => server.Id).ToList()
            };
        }
    }
}
