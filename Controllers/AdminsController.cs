using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    public class AdminsController : Controller
    {
        private readonly INasRepository repository;

        public AdminsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllAdmins());
        }

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
