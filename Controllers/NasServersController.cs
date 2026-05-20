using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;
using NasIndexer.Utilities;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class NasServersController : Controller
    {
        private readonly INasRepository repository;

        public NasServersController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllNasServers());
        }

        public IActionResult Search(string? query)
        {
            return PartialView("_NasServerRows", repository.SearchNasServers(query, int.MaxValue));
        }

        public IActionResult Details(int id)
        {
            var server = repository.GetNasServerById(id);

            if (server == null)
            {
                return NotFound();
            }

            return View(server);
        }

        public IActionResult Create()
        {
            return View(new NasServerFormViewModel
            {
                LastScanText = DateTimeInputParser.Format(DateTime.Now),
                IsActive = true,
                Port = 445
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(NasServerFormViewModel model)
        {
            if (!TryBuildNasServer(model, out var server))
            {
                return View(model);
            }

            repository.AddNasServer(server);
            TempData["StatusMessage"] = $"NAS server {server.Name} was created.";
            TempData["HighlightNasServerId"] = server.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var server = repository.GetNasServerForEdit(id);

            if (server == null)
            {
                return NotFound();
            }

            return View(ToFormViewModel(server));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, NasServerFormViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!TryBuildNasServer(model, out var server))
            {
                return View(model);
            }

            if (!repository.UpdateNasServer(server))
            {
                return NotFound();
            }

            TempData["StatusMessage"] = $"NAS server {server.Name} was updated.";
            TempData["HighlightNasServerId"] = server.Id;

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var server = repository.GetNasServerById(id);

            if (server == null)
            {
                return NotFound();
            }

            ViewData["DeleteBlocked"] = repository.NasServerHasScanJobsOrManagedAdmins(id);
            return View(server);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var server = repository.GetNasServerById(id);

            if (server == null)
            {
                return NotFound();
            }

            if (repository.NasServerHasScanJobsOrManagedAdmins(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This NAS server cannot be deleted because it has scan jobs or assigned administrators.");
                return View(server);
            }

            if (!repository.DeleteNasServer(id))
            {
                ViewData["DeleteBlocked"] = true;
                ModelState.AddModelError(string.Empty, "This NAS server could not be deleted. Refresh the page and check connected records.");
                return View(server);
            }

            TempData["StatusMessage"] = $"NAS server {server.Name} was deleted.";

            return RedirectToAction(nameof(Index));
        }

        private bool TryBuildNasServer(NasServerFormViewModel model, out NasServer server)
        {
            server = new NasServer();
            var hasLastScan = DateTimeInputParser.TryParse(model.LastScanText, out var lastScan);

            if (!string.IsNullOrWhiteSpace(model.LastScanText) && !hasLastScan)
            {
                ModelState.AddModelError(nameof(model.LastScanText), "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
            }

            if (!ModelState.IsValid)
            {
                return false;
            }

            server = new NasServer
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                IpAddress = model.IpAddress.Trim(),
                Port = model.Port,
                Username = model.Username?.Trim() ?? string.Empty,
                IsActive = model.IsActive,
                LastScan = lastScan
            };

            return true;
        }

        private static NasServerFormViewModel ToFormViewModel(NasServer server)
        {
            return new NasServerFormViewModel
            {
                Id = server.Id,
                Name = server.Name,
                IpAddress = server.IpAddress,
                Port = server.Port,
                Username = server.Username,
                IsActive = server.IsActive,
                LastScanText = DateTimeInputParser.Format(server.LastScan)
            };
        }
    }
}
