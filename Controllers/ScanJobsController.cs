using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    public class ScanJobsController : Controller
    {
        private readonly INasRepository repository;

        public ScanJobsController(INasRepository repository)
        {
            this.repository = repository;
        }

        public IActionResult Index()
        {
            return View(repository.GetAllScanJobs());
        }

        public IActionResult Details(int id)
        {
            var scanJob = repository.GetScanJobById(id);

            if (scanJob == null)
            {
                return NotFound();
            }

            return View(scanJob);
        }
    }
}
