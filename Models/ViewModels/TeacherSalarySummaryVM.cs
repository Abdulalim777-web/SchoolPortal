using System;
using System.Collections.Generic;

namespace SchoolPortal.Models.ViewModels
{
    public class TeacherSalarySummaryVM
    {
        public int StaffId { get; set; }
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public int TermMonthsCount { get; set; }
        public decimal TermSalaryTotal { get; set; }
        public decimal SalaryPaid { get; set; }
        public decimal SalaryBalance { get; set; }
        public List<SalarySummaryItem>? PaymentHistory { get; set; }
    }

    public class SalarySummaryItem
    {
        public int SalaryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Month { get; set; }
        public bool IsPaid { get; set; }
    }
}
