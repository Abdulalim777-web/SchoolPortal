using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;
using SchoolPortal.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolPortal.Services
{
    public interface IBulkOperationService
    {
        Task<BulkOperationResult> AllocateBalanceToClassAsync(
            string className,
            string? term,
            string? notes,
            string performedByUserId);

        Task<DuplicatePaymentCheckResult> CheckDuplicatePaymentAsync(
            int studentId,
            PaymentPurpose purpose,
            string? term);

        Task<List<BulkOperationAudit>> GetBulkOperationHistoryAsync(
            int pageNumber = 1,
            int pageSize = 20);

        Task<BulkOperationAudit?> GetBulkOperationByIdAsync(int id);
    }

    public class BulkOperationService : IBulkOperationService
    {
        private readonly SchoolPortalDbContext _context;

        public BulkOperationService(SchoolPortalDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Allocate balance to all students in a specific class
        /// </summary>
        public async Task<BulkOperationResult> AllocateBalanceToClassAsync(
            string className,
            string? term,
            string? notes,
            string performedByUserId)
        {
            var result = new BulkOperationResult();

            try
            {
                // Validate class configuration
                if (!ClassBalanceConfiguration.IsClassConfigured(className))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Class '{className}' is not configured.";
                    return result;
                }

                // Get the balance amount for this class
                var balanceAmount = ClassBalanceConfiguration.GetBalanceForClass(className);

                // Get all students in this class
                var studentsInClass = await _context.Students
                    .Where(s => s.Class == className)
                    .ToListAsync();

                if (studentsInClass.Count == 0)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"No students found in class '{className}'.";
                    return result;
                }

                // Allocate balance to each student
                foreach (var student in studentsInClass)
                {
                    student.Balance += balanceAmount;
                }

                // Create audit log entry
                var auditEntry = new BulkOperationAudit
                {
                    OperationType = BulkOperationType.BalanceAllocation,
                    OperationDescription = $"Allocated balance to class {className}",
                    AffectedClass = className,
                    AffectedTerm = term,
                    Amount = balanceAmount,
                    RecordsAffected = studentsInClass.Count,
                    PerformedByUserId = performedByUserId,
                    PerformedAt = DateTime.UtcNow,
                    Notes = notes,
                    Status = "Completed"
                };

                _context.BulkOperationAudits.Add(auditEntry);

                // Save all changes
                await _context.SaveChangesAsync();

                result.IsSuccess = true;
                result.Message = $"Successfully allocated balance to {studentsInClass.Count} students in class {className}.";
                result.RecordsAffected = studentsInClass.Count;
                result.TotalAmountAllocated = balanceAmount * studentsInClass.Count;
                result.AuditLogId = auditEntry.Id;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error during bulk operation: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Check if a duplicate payment exists for a student by purpose and term
        /// </summary>
        public async Task<DuplicatePaymentCheckResult> CheckDuplicatePaymentAsync(
            int studentId,
            PaymentPurpose purpose,
            string? term)
        {
            var result = new DuplicatePaymentCheckResult
            {
                StudentId = studentId,
                Purpose = purpose,
                Term = term
            };

            try
            {
                var existingPayment = await _context.Payments
                    .Where(p => p.StudentId == studentId 
                        && p.Purpose == purpose 
                        && p.Term == term)
                    .FirstOrDefaultAsync();

                if (existingPayment != null)
                {
                    result.IsDuplicate = true;
                    result.ExistingPaymentId = existingPayment.Id;
                    result.ExistingPaymentDate = existingPayment.DatePaid;
                    result.ExistingPaymentAmount = existingPayment.Amount;
                    result.Message = $"Payment for {purpose} in {term} already exists for this student (Payment ID: {existingPayment.Id}, Date: {existingPayment.DatePaid:yyyy-MM-dd}).";
                }
                else
                {
                    result.IsDuplicate = false;
                    result.Message = "No duplicate payment found.";
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Get bulk operation history with pagination
        /// </summary>
        public async Task<List<BulkOperationAudit>> GetBulkOperationHistoryAsync(
            int pageNumber = 1,
            int pageSize = 20)
        {
            return await _context.BulkOperationAudits
                .Include(b => b.PerformedByUser)
                .OrderByDescending(b => b.PerformedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific bulk operation by ID
        /// </summary>
        public async Task<BulkOperationAudit?> GetBulkOperationByIdAsync(int id)
        {
            return await _context.BulkOperationAudits
                .Include(b => b.PerformedByUser)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
    }

    /// <summary>
    /// Result object for bulk balance allocation operation
    /// </summary>
    public class BulkOperationResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public int RecordsAffected { get; set; }
        public decimal TotalAmountAllocated { get; set; }
        public int AuditLogId { get; set; }
    }

    /// <summary>
    /// Result object for duplicate payment check
    /// </summary>
    public class DuplicatePaymentCheckResult
    {
        public int StudentId { get; set; }
        public PaymentPurpose Purpose { get; set; }
        public string? Term { get; set; }
        public bool IsDuplicate { get; set; }
        public int? ExistingPaymentId { get; set; }
        public DateTime? ExistingPaymentDate { get; set; }
        public decimal? ExistingPaymentAmount { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
