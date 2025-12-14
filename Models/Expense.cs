using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [StringLength(200)]
        public string? Note { get; set; }

        // Optional: file path if attaching receipts
        public string? ReceiptPath { get; set; }
    }
}
