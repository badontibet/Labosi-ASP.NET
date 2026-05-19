using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

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

        public IActionResult Details(int id)
        {
            var directory = repository.GetDirectoryById(id);

            if (directory == null)
            {
                return NotFound();
            }

            return View(directory);
        }
    }
}
