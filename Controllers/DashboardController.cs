using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Admin,Bursar,Teacher,Student")]
    public class DashboardController : Controller
    {
        private readonly SchoolPortalDbContext _context;

        public DashboardController(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = await BuildDashboardModel();

            if (User.IsInRole("Admin"))
                return View("Admin", model);

            if (User.IsInRole("Bursar"))
                return View("Bursar", model);

            if (User.IsInRole("Teacher"))
                return View("Teacher", model);

            if (User.IsInRole("Student"))
                return View("Student", model);

            return RedirectToAction("AccessDenied", "Account");
        }

        private async Task<DashboardViewModel> BuildDashboardModel()
        {
            var model = new DashboardViewModel();

            model.TotalIncome = await _context.Payments.SumAsync(p => p.Amount);
            model.TotalExpenses = await _context.Expenses.SumAsync(e => e.Amount);
            model.Balance = model.TotalIncome - model.TotalExpenses;

            int year = DateTime.Now.Year;
            var months = Enumerable.Range(1, 12);

            var income = await _context.Payments
                .Where(p => p.DatePaid.Year == year)
                .GroupBy(p => p.DatePaid.Month)
                .Select(g => new MonthlyIncomeDto
                {
                    Year = year,
                    Month = g.Key,
                    TotalIncome = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.Date.Year == year)
                .GroupBy(e => e.Date.Month)
                .Select(g => new MonthlyExpenseDto
                {
                    Year = year,
                    Month = g.Key,
                    TotalExpense = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            model.MonthlyIncome = months
                .Select(m => income.FirstOrDefault(x => x.Month == m)
                    ?? new MonthlyIncomeDto { Year = year, Month = m, TotalIncome = 0 })
                .ToList();

            model.MonthlyExpenses = months
                .Select(m => expenses.FirstOrDefault(x => x.Month == m)
                    ?? new MonthlyExpenseDto { Year = year, Month = m, TotalExpense = 0 })
                .ToList();

            return model;
        }
    }
}
