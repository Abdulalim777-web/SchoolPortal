using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;
using SchoolPortal.Models.ViewModels;

namespace SchoolPortal.Services
{
    public class BursarService
    {
        private readonly SchoolPortalDbContext _context;

        public BursarService(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public async Task<BursarTermSummaryVM> GetTermSummaryAsync(int? termYear = null)
        {
            termYear ??= DateTime.Now.Year;

            // Get term date range
            var termStart = GetTermStartDate(termYear.Value);
            var termEnd = termStart.AddMonths(3).AddDays(-1);

            // Get expenses
            var expenses = await _context.Expenses
                .Where(e => e.Date >= termStart && e.Date <= termEnd)
                .ToListAsync();

            var expensesByCategory = expenses
                .GroupBy(e => e.Category ?? "Uncategorized")
                .Select(g => new ExpenseChartItem
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(e => e.Amount)
                .ToList();

            // Get payments by class
            var paymentsByClass = await _context.Payments
                .Where(p => p.DatePaid >= termStart 
                    && p.DatePaid <= termEnd 
                    && p.Status == PaymentStatus.Approved)
                .Include(p => p.Student)
                .GroupBy(p => p.Student!.Class)
                .Select(g => new
                {
                    ClassName = g.Key,
                    Payments = g.ToList()
                })
                .ToListAsync();

            var feeBreakdown = new List<FeeBreakdownByClass>();

            foreach (var classGroup in paymentsByClass)
            {
                var students = await _context.Students
                    .Where(s => s.Class == classGroup.ClassName)
                    .ToListAsync();

                var totalPaid = classGroup.Payments.Sum(p => p.Amount);
                var totalBalance = students.Sum(s => s.Balance);

                feeBreakdown.Add(new FeeBreakdownByClass
                {
                    ClassName = classGroup.ClassName,
                    TotalExpected = totalPaid + totalBalance,
                    TotalPaid = totalPaid,
                    OutstandingBalance = totalBalance,
                    StudentCount = students.Count
                });
            }

            var totalExpenses = expenses.Sum(e => e.Amount);
            var totalFeesCollected = paymentsByClass
                .SelectMany(pg => pg.Payments)
                .Sum(p => p.Amount);
            var totalTermBalance = _context.Students.Sum(s => s.Balance);

            return new BursarTermSummaryVM
            {
                TermYear = termYear.Value,
                ExpensesByCategory = expensesByCategory,
                FeeBreakdownByClass = feeBreakdown,
                TotalExpenses = totalExpenses,
                TotalFeesCollected = totalFeesCollected,
                TotalTermBalance = totalTermBalance
            };
        }

        private DateTime GetTermStartDate(int year)
        {
            var currentMonth = DateTime.Now.Month;
            
            // Term structure: Jan-Mar, Apr-Jun, Jul-Sep, Oct-Dec
            if (currentMonth <= 3)
                return new DateTime(year, 1, 1);
            else if (currentMonth <= 6)
                return new DateTime(year, 4, 1);
            else if (currentMonth <= 9)
                return new DateTime(year, 7, 1);
            else
                return new DateTime(year, 10, 1);
        }
    }
}
