using System.Collections.Generic;

namespace SchoolPortal.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Class { get; set; }
        public decimal Balance { get; set; }

        public ICollection<Payment>? Payments { get; set; }
    }
}
