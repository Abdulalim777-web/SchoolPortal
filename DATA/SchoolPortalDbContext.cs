using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Models;

namespace SchoolPortal.Data
{
    // DbContext supporting roles
    public class SchoolPortalDbContext 
        : IdentityDbContext<User, IdentityRole, string>
    {
        public SchoolPortalDbContext(DbContextOptions<SchoolPortalDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Salary> Salaries { get; set; }
    }
}
