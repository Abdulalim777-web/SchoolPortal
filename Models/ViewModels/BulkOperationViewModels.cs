using SchoolPortal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.Models.ViewModels
{
    /// <summary>
    /// ViewModel for bulk balance allocation
    /// </summary>
    public class BulkBalanceAllocationViewModel
    {
        [Required(ErrorMessage = "Please select a class")]
        [Display(Name = "Select Class")]
        public string? SelectedClass { get; set; }

        [StringLength(50)]
        [Display(Name = "Term (Optional)")]
        public string? Term { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes (Optional)")]
        public string? Notes { get; set; }

        /// <summary>
        /// List of available classes for selection
        /// </summary>
        public List<string> AvailableClasses { get; set; } = new();

        /// <summary>
        /// Dictionary mapping class names to their balance amounts
        /// </summary>
        public Dictionary<string, decimal> ClassBalances { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for bulk operation history
    /// </summary>
    public class BulkOperationHistoryViewModel
    {
        public List<BulkOperationAudit> Operations { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
    }
}
