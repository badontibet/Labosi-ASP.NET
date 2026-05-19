using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

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

        public IActionResult Details(int id)
        {
            var file = repository.GetFileById(id);

            if (file == null)
            {
                return NotFound();
            }

            return View(file);
        }
    }
}
