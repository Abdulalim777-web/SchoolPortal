using System;

namespace SchoolPortal.Models
{
    public class Salary
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Month { get; set; }

        public Staff? Staff { get; set; }
    }
}
