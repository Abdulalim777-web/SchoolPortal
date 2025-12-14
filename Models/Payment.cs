using System;

namespace SchoolPortal.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? Purpose { get; set; }

        public Student? Student { get; set; }
    }
}
