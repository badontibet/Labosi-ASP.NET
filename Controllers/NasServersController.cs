using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    [Route("NasServer")]
    public class NasServersController : Controller
    {
        private readonly INasRepository repository;

        public NasServersController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View(repository.GetAllNasServers());
        }

        [HttpGet("Details/{id:int}")]
        public IActionResult Details(int id)
        {
            var server = repository.GetNasServerById(id);

            if (server == null)
            {
                return NotFound();
            }

            return View(server);
        }
    }
}
