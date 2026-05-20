using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

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

        public IActionResult Details(int id)
        {
            var tag = repository.GetTagById(id);

            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }
    }
}
