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
    public class StudentService
    {
        private readonly SchoolPortalDbContext _context;

        public StudentService(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public async Task<StudentTermSummaryVM?> GetTermSummaryAsync(int studentId, int? termYear = null)
        {
            termYear ??= DateTime.Now.Year;

            var student = await _context.Students
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return null;

            // Get payments for this term
            var termPayments = student.Payments?
                .Where(p => p.DatePaid.Year == termYear && p.Status == PaymentStatus.Approved)
                .ToList() ?? new List<Payment>();

            var totalPaid = termPayments.Sum(p => p.Amount);
            var breakdown = termPayments
                .GroupBy(p => p.Purpose)
                .Select(g => new PaymentBreakdownItem
                {
                    Purpose = g.Key ?? PaymentPurpose.Other,
                    TotalAmount = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToList();

            return new StudentTermSummaryVM
            {
                StudentId = student.Id,
                FullName = student.FullName,
                Class = student.Class,
                CurrentBalance = student.Balance,
                TermFeesTotal = 0, // You would calculate this based on your school fees structure
                AmountPaid = totalPaid,
                PaymentHistory = termPayments
                    .OrderByDescending(p => p.DatePaid)
                    .Select(p => new PaymentSummaryItem
                    {
                        PaymentId = p.Id,
                        Amount = p.Amount,
                        DatePaid = p.DatePaid,
                        Purpose = p.Purpose,
                        Status = p.Status
                    })
                    .ToList(),
                BreakdownByPurpose = breakdown
            };
        }

        public async Task<List<StudentTermSummaryVM>> GetClassTermSummaryAsync(string className, int? termYear = null)
        {
            termYear ??= DateTime.Now.Year;

            var students = await _context.Students
                .Where(s => s.Class == className)
                .Include(s => s.Payments)
                .ToListAsync();

            var summaries = new List<StudentTermSummaryVM>();

            foreach (var student in students)
            {
                var summary = await GetTermSummaryAsync(student.Id, termYear);
                if (summary != null)
                    summaries.Add(summary);
            }

            return summaries;
        }
    }
}
