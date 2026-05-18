using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    [Route("FileTag")]
    public class TagsController : Controller
    {
        private readonly INasRepository repository;

        public TagsController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View(repository.GetAllTags());
        }

        [HttpGet("Details/{id:int}")]
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
