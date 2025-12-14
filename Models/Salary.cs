using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolPortal.Models
{
    public class Salary
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Staff")]
        public int StaffId { get; set; }

        public Staff? Staff { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime Month { get; set; }

        [Required]
        public bool IsPaid { get; set; }
    }
}
