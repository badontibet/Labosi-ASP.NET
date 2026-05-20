using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

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
