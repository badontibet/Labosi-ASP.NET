using Microsoft.AspNetCore.Mvc;
using NasIndexer.Repositories;

namespace NasIndexer.Controllers
{
    [Route("ScanJob")]
    public class ScanJobsController : Controller
    {
        private readonly INasRepository repository;

        public ScanJobsController(INasRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View(repository.GetAllScanJobs());
        }

        [HttpGet("Details/{id:int}")]
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
