using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    [Route("FileItem")]
    public class FileItemsController : Controller
    {
        private readonly INasRepository repository;

        public FileItemsController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View(repository.GetAllFiles());
        }

        [HttpGet("Details/{id:int}")]
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
