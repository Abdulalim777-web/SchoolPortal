using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.Data;
using System.Linq;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Bursar")]
    public class BursarController : Controller
    {
        private readonly SchoolPortalDbContext _context;

        public BursarController(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalIncome = _context.Payments.Sum(p => p.Amount);
            ViewBag.TotalExpenses = _context.Expenses.Sum(e => e.Amount);
            ViewBag.Balance = ViewBag.TotalIncome - ViewBag.TotalExpenses;

            return View();
        }
    }
}
