using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.Models
{
    public class Staff
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(50)]
        public string? Position { get; set; }

        [Required]
        public DateTime DateJoined { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
