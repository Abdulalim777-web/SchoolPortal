using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;
using System.Globalization;

namespace SchoolPortal.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SchoolPortalDbContext _context;

        public DashboardController(SchoolPortalDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            model.TotalIncome = await _context.Payments.SumAsync(p => p.Amount);
            model.TotalExpenses = await _context.Expenses.SumAsync(e => e.Amount);
            model.Balance = model.TotalIncome - model.TotalExpenses;

            int currentYear = DateTime.Now.Year;

            // Initialize months 1–12
            var allMonths = Enumerable.Range(1, 12).ToList();

            var monthlyIncomeRaw = await _context.Payments
                .Where(p => p.DatePaid.Year == currentYear)
                .GroupBy(p => p.DatePaid.Month)
                .Select(g => new MonthlyIncomeDto
                {
                    Year = currentYear,
                    Month = g.Key,
                    TotalIncome = g.Sum(p => p.Amount)
                })
                .ToListAsync();

            var monthlyExpenseRaw = await _context.Expenses
                .Where(e => e.Date.Year == currentYear)
                .GroupBy(e => e.Date.Month)
                .Select(g => new MonthlyExpenseDto
                {
                    Year = currentYear,
                    Month = g.Key,
                    TotalExpense = g.Sum(e => e.Amount)
                })
                .ToListAsync();

            // Fill missing months with 0
            model.MonthlyIncome = allMonths
                .Select(m => monthlyIncomeRaw.FirstOrDefault(x => x.Month == m) ?? new MonthlyIncomeDto { Year = currentYear, Month = m, TotalIncome = 0 })
                .ToList();

            model.MonthlyExpenses = allMonths
                .Select(m => monthlyExpenseRaw.FirstOrDefault(x => x.Month == m) ?? new MonthlyExpenseDto { Year = currentYear, Month = m, TotalExpense = 0 })
            .ToList();

            return View(model);
    }



        // GET: Dashboard/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

                    var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Dashboard/Create
        public IActionResult Create()
        {
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id");
            return View();
        }

        // POST: Dashboard/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StudentId,Amount,DatePaid,Purpose")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", payment.StudentId);
            return View(payment);
        }

        // GET: Dashboard/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

                    var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", payment.StudentId);
            return View(payment);
        }

        // POST: Dashboard/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,Amount,Date,Purpose")] Payment payment)
        {
            if (id != payment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", payment.StudentId);
            return View(payment);
        }

        // GET: Dashboard/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

                    var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Dashboard/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
                    var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }

    }
}
