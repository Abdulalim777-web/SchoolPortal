using System;

namespace SchoolPortal.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string? Category { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Note { get; set; }
    }
}
