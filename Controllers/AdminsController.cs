using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    [Route("SystemAdmin")]
    public class AdminsController : Controller
    {
        private readonly INasRepository repository;

        public AdminsController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View(repository.GetAllAdmins());
        }

        [HttpGet("Details/{id:int}")]
        public IActionResult Details(int id)
        {
            var admin = repository.GetAdminById(id);

            if (admin == null)
            {
                return NotFound();
            }

            return View(admin);
        }
    }
}
