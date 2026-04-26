using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPortal.Models
{
    public enum BulkOperationType
    {
        BalanceAllocation = 1,      // Adding balance to students in a class
        ClassStart = 2,             // Starting a new class/term
        ManualAdjustment = 3        // Manual balance adjustments
    }

    public class BulkOperationAudit
    {
        public int Id { get; set; }

        [Required]
        public BulkOperationType OperationType { get; set; }

        [Required]
        [StringLength(100)]
        public string OperationDescription { get; set; } = null!;

        [StringLength(50)]
        public string? AffectedClass { get; set; }

        [StringLength(50)]
        public string? AffectedTerm { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public int RecordsAffected { get; set; }

        // Who performed the operation
        [Required]
        public string PerformedByUserId { get; set; } = null!;

        [ForeignKey("PerformedByUserId")]
        public User? PerformedByUser { get; set; }

        [Required]
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        // Additional details
        [StringLength(500)]
        public string? Notes { get; set; }

        // Status of the operation
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Failed, Pending
    }
}
