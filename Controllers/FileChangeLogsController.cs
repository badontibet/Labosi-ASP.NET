using Microsoft.AspNetCore.Mvc;
using NasIndexer.Model;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    public class FileChangeLogsController : Controller
    {
        private readonly INasRepository repository;

        public FileChangeLogsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllFileChangeLogs());
        }

        public IActionResult Search(string? query, ChangeType? changeType)
        {
            return PartialView("_FileChangeLogRows", repository.SearchFileChangeLogs(query, changeType));
        }

        public IActionResult Details(int id)
        {
            var changeLog = repository.GetFileChangeLogById(id);

            if (changeLog == null)
            {
                return NotFound();
            }

            return View(changeLog);
        }
    }
}
