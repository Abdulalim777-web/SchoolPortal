using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<User> _userManager;

        public DashboardController(SchoolPortalDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var currentUser = await _userManager.GetUserAsync(User);

            // Common data for all dashboards
            model.TotalIncome = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Approved)
                .SumAsync(p => p.Amount);
            
            model.TotalExpenses = await _context.Expenses.SumAsync(e => e.Amount);
            model.Balance = model.TotalIncome - model.TotalExpenses;

            int year = DateTime.Now.Year;
            var months = Enumerable.Range(1, 12);

            var income = await _context.Payments
                .Where(p => p.DatePaid.Year == year && p.Status == PaymentStatus.Approved)
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

            // Admin Dashboard Data
            if (User.IsInRole("Admin"))
            {
                model.PendingPayments = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Initiated)
                    .CountAsync();

                model.TotalStudents = await _context.Students.CountAsync();
                model.TotalStaff = await _context.Staffs.CountAsync();
            }

            // Bursar Dashboard Data
            if (User.IsInRole("Bursar"))
            {
                model.ExpenseByCategory = await _context.Expenses
                    .GroupBy(e => e.Category)
                    .Select(g => new ExpenseCategoryDto
                    {
                        Category = g.Key,
                        Total = g.Sum(e => e.Amount),
                        Count = g.Count()
                    })
                    .ToListAsync();

                model.InitiatedPaymentsCount = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Initiated)
                    .CountAsync();

                model.TotalSchoolFeesPaid = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Approved && p.Purpose == PaymentPurpose.SchoolFees)
                    .SumAsync(p => p.Amount);

                model.ApprovedPaymentsCount = await _context.Payments
                    .Where(p => p.Status == PaymentStatus.Approved)
                    .CountAsync();

                // Class Fee Breakdown
                model.ClassFeeBreakdown = await _context.Students
                    .GroupBy(s => s.Class)
                    .Select(g => new ClassFeeBreakdownDto
                    {
                        ClassName = g.Key,
                        StudentCount = g.Count(),
                        TotalPaid = g.SelectMany(s => s.Payments!)
                            .Where(p => p != null && p.Status == PaymentStatus.Approved)
                            .Sum(p => p.Amount),
                        TotalBalance = g.Sum(s => s.Balance == null ? 0 : s.Balance)
                    })
                    .ToListAsync();
            }

            // Teacher Dashboard Data
            if (User.IsInRole("Teacher") && currentUser != null)
            {
                var staff = await _context.Staffs
                    .FirstOrDefaultAsync(s => s.Email == currentUser.Email);
                
                if (staff != null)
                {
                    model.TeacherName = staff.FullName;
                    model.TeacherPosition = staff.Position;

                    var salaries = await _context.Salaries
                        .Where(s => s.StaffId == staff.Id && s.Month.Year == year)
                        .OrderByDescending(s => s.Month)
                        .ToListAsync();

                    model.SalaryHistory = salaries
                        .Select(s => new SalaryHistoryDto
                        {
                            Month = s.Month.ToString("MMMM yyyy"),
                            Amount = s.Amount,
                            IsPaid = s.IsPaid
                        })
                        .ToList();

                    model.TeacherTotalSalaryPaid = salaries
                        .Where(s => s.IsPaid)
                        .Sum(s => s.Amount);

                    model.TeacherPendingSalary = salaries
                        .Where(s => !s.IsPaid)
                        .Sum(s => s.Amount);
                }
                else
                {
                    model.TeacherPosition = "Staff record not linked";
                    model.SalaryHistory = new List<SalaryHistoryDto>();
                }
            }

            // Student Dashboard Data
            if (User.IsInRole("Student") && currentUser != null)
            {
                var student = await _context.Students
                    .Include(s => s.Payments)
                    .FirstOrDefaultAsync(s => s.UserId == currentUser.Id);

                if (student != null)
                {
                    model.StudentName = student.FullName;
                    model.StudentClass = student.Class;
                    model.StudentBalance = student.Balance;

                    var payments = student.Payments?
                        .Where(p => p.Status == PaymentStatus.Approved)
                        .ToList() ?? new List<Payment>();

                    model.StudentTotalPaid = payments.Sum(p => p.Amount);
                    model.StudentPayments = payments;
                }
            }

            return model;
        }
    }
}
