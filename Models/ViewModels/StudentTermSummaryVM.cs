using System;
using System.Collections.Generic;

namespace SchoolPortal.Models.ViewModels
{
    public class StudentTermSummaryVM
    {
        public int StudentId { get; set; }
        public string? FullName { get; set; }
        public string? Class { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TermFeesTotal { get; set; }
        public decimal AmountPaid { get; set; }
        public List<PaymentSummaryItem>? PaymentHistory { get; set; }
        public List<PaymentBreakdownItem>? BreakdownByPurpose { get; set; }
    }

    public class PaymentSummaryItem
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DatePaid { get; set; }
        public PaymentPurpose? Purpose { get; set; }
        public PaymentStatus Status { get; set; }
    }

    public class PaymentBreakdownItem
    {
        public PaymentPurpose Purpose { get; set; }
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }
}
