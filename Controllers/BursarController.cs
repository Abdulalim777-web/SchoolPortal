using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.Data;
using SchoolPortal.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Bursar")]
    public class BursarController : Controller
    {
        private readonly SchoolPortalDbContext _context;
        private readonly BursarService _bursarService;

        public BursarController(SchoolPortalDbContext context, BursarService bursarService)
        {
            _context = context;
            _bursarService = bursarService;
        }

        public IActionResult Index()
        {
            ViewBag.TotalIncome = _context.Payments.Sum(p => p.Amount);
            ViewBag.TotalExpenses = _context.Expenses.Sum(e => e.Amount);
            ViewBag.Balance = ViewBag.TotalIncome - ViewBag.TotalExpenses;

            return View();
        }

        // GET: Bursar/TermSummary
        [Authorize(Roles = "Bursar")]
        public async Task<IActionResult> TermSummary(int? termYear = null)
        {
            var summary = await _bursarService.GetTermSummaryAsync(termYear);
            return View(summary);
        }
    }
}
