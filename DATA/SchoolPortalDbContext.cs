using Microsoft.EntityFrameworkCore;
using SchoolPortal.Models;

namespace SchoolPortal.DATA
{
    public class SchoolPortalDbContext : DbContext
    {
        public SchoolPortalDbContext(DbContextOptions<SchoolPortalDbContext> options)
            : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
