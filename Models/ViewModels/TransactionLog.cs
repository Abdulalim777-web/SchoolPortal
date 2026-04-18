using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPortal.Models
{
    public enum TransactionType
    {
        Credit,  // balance added (e.g. admin top-up)
        Debit    // payment approved, balance reduced
    }

    public class TransactionLog
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        // The payment that triggered this log (nullable for manual adjustments)
        public int? PaymentId { get; set; }

        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }

        public TransactionType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>Balance on the student's account after this transaction.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }

        /// <summary>UserId of whoever triggered the transaction (bursar / admin).</summary>
        public string? PerformedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
