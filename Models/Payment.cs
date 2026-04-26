using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPortal.Models
{
    public enum PaymentStatus
    {
        Initiated,   // receipt uploaded, awaiting bursar
        Approved     // bursar confirmed, balance updated
    }

    public enum PaymentPurpose
    {
        SchoolFees = 1,
        Donations = 2,
        PTALevy = 3,
        Other = 4
    }

    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DatePaid { get; set; }

        public PaymentPurpose? Purpose { get; set; }

        [StringLength(50)]
        public string? Term { get; set; } // e.g., "Term 1", "Term 2", "Term 3"

        [StringLength(50)]
        public string? RrrNumber { get; set; }

        // Who submitted this payment
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ── NEW ──────────────────────────────────────────
        // Path to uploaded receipt (relative to wwwroot)
        [StringLength(500)]
        public string? ReceiptPath { get; set; }

        // Workflow status
        public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

        // Bursar who approved + when
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
