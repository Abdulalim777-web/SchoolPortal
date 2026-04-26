using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Models;

namespace SchoolPortal.Data
{
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
        public DbSet<LoginAudit> LoginAudits { get; set; }
        public DbSet<NavigationAudit> NavigationAudits { get; set; }
        public DbSet<BulkOperationAudit> BulkOperationAudits { get; set; }

        // ── NEW ─────────────────────────────────────────
        public DbSet<TransactionLog> TransactionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SQL Server does not allow multiple cascade paths to the same table.
            // All User FK relationships on Payment and TransactionLog must use NoAction.

            builder.Entity<Payment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.ApprovedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Payment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TransactionLog>()
                .HasOne(t => t.Payment)
                .WithMany()
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TransactionLog>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<BulkOperationAudit>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.PerformedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
