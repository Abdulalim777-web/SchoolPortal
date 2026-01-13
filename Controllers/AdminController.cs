using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.Data;
using System.Linq;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SchoolPortalDbContext _context;

        public AdminController(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalStudents = _context.Students.Count();
            ViewBag.TotalStaff = _context.Staffs.Count();
            ViewBag.TotalPayments = _context.Payments.Count();
            ViewBag.PendingApprovals = 5; // Replace with real logic

            return View();
        }
    }
}
