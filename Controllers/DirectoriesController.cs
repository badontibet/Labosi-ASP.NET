using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    [Route("DirectoryItem")]
    public class DirectoriesController : Controller
    {
        private readonly INasRepository repository;

        public DirectoriesController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View(repository.GetAllDirectories());
        }

        [HttpGet("Details/{id:int}")]
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
