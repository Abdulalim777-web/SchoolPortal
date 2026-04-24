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
    public class StaffService
    {
        private readonly SchoolPortalDbContext _context;

        public StaffService(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public async Task<TeacherSalarySummaryVM?> GetTermSalarySummaryAsync(int staffId, int? termYear = null)
        {
            termYear ??= DateTime.Now.Year;

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(s => s.Id == staffId);

            if (staff == null) return null;

            // Get salaries for this term (assuming 3 months per term: Jan-Mar, Apr-Jun, Jul-Sep, Oct-Dec)
            var termStart = GetTermStartDate(termYear.Value);
            var termEnd = termStart.AddMonths(3).AddDays(-1);

            var termSalaries = await _context.Salaries
                .Where(s => s.StaffId == staffId 
                    && s.Month >= termStart 
                    && s.Month <= termEnd)
                .OrderBy(s => s.Month)
                .ToListAsync();

            var totalSalary = termSalaries.Sum(s => s.Amount);
            var paidSalary = termSalaries.Where(s => s.IsPaid).Sum(s => s.Amount);

            return new TeacherSalarySummaryVM
            {
                StaffId = staff.Id,
                FullName = staff.FullName,
                Position = staff.Position,
                TermMonthsCount = termSalaries.Count,
                TermSalaryTotal = totalSalary,
                SalaryPaid = paidSalary,
                SalaryBalance = totalSalary - paidSalary,
                PaymentHistory = termSalaries
                    .Select(s => new SalarySummaryItem
                    {
                        SalaryId = s.Id,
                        Amount = s.Amount,
                        Month = s.Month,
                        IsPaid = s.IsPaid
                    })
                    .ToList()
            };
        }

        private DateTime GetTermStartDate(int year)
        {
            var currentMonth = DateTime.Now.Month;
            
            // Adjust based on term structure (Jan-Mar, Apr-Jun, Jul-Sep, Oct-Dec)
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
