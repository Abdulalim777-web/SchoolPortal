using System;
using System.Collections.Generic;

namespace SchoolPortal.Models.ViewModels
{
    public class BursarTermSummaryVM
    {
        public int TermYear { get; set; }
        public List<ExpenseChartItem>? ExpensesByCategory { get; set; }
        public List<FeeBreakdownByClass>? FeeBreakdownByClass { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalTermBalance { get; set; }
    }

    public class ExpenseChartItem
    {
        public string? Category { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class FeeBreakdownByClass
    {
        public string? ClassName { get; set; }
        public decimal TotalExpected { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal OutstandingBalance { get; set; }
        public int StudentCount { get; set; }
    }
}
