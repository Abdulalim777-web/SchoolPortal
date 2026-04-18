using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Admin,Bursar,Student")]
    public class PaymentsController : Controller
    {
        private readonly SchoolPortalDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _env;

        public PaymentsController(
            SchoolPortalDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // ═══════════════════════════════════════════════════════
        //  INDEX
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            IQueryable<Payment> payments = _context.Payments
                .Include(p => p.Student);

            if (await _userManager.IsInRoleAsync(user!, "Student"))
            {
                // Students only see their own payments
                payments = payments.Where(p => p.Student!.UserId == user!.Id);
            }

            return View(await payments.OrderByDescending(p => p.CreatedAt).ToListAsync());
        }

        // ═══════════════════════════════════════════════════════
        //  CREATE  (GET)
        // ═══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin,Bursar,Student")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            var isStudent = user != null && await _userManager.IsInRoleAsync(user, "Student");

            if (isStudent)
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    student = new Student
                    {
                        UserId = userId,
                        FullName = user!.FullName,
                        Balance = 0m
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                }

                ViewBag.StudentName = student.FullName;
                return View(new Payment { StudentId = student.Id, DatePaid = DateTime.Now });
            }

            // Admin / Bursar → student dropdown
            ViewData["StudentId"] = new SelectList(
                _context.Students.OrderBy(s => s.FullName), "Id", "FullName");

            return View(new Payment { DatePaid = DateTime.Now });
        }

        // ═══════════════════════════════════════════════════════
        //  CREATE  (POST)
        //  Student uploads a receipt → status = Initiated
        //  Balance is NOT touched here; only on bursar approval.
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment, IFormFile? receiptFile)
        {
            var user = await _userManager.GetUserAsync(User);

            // ── resolve student ───────────────────────────────
            if (await _userManager.IsInRoleAsync(user!, "Student"))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user!.Id);

                if (student == null) return Forbid();

                payment.StudentId = student.Id;
            }
            else
            {
                if (!await _context.Students.AnyAsync(s => s.Id == payment.StudentId))
                    ModelState.AddModelError("StudentId", "Invalid student selected.");
            }

            // ── receipt upload ────────────────────────────────
            if (receiptFile == null || receiptFile.Length == 0)
            {
                ModelState.AddModelError("receiptFile", "A receipt file is required.");
            }

            if (!ModelState.IsValid)
            {
                RepopulateDropdowns(user!, payment);
                return View(payment);
            }

            // Save file to  wwwroot/receipts/<year>/
            var uploadsFolder = Path.Combine(_env.WebRootPath, "receipts", DateTime.UtcNow.Year.ToString());
            Directory.CreateDirectory(uploadsFolder);

            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(receiptFile!.FileName)}";
            var filePath = Path.Combine(uploadsFolder, safeFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await receiptFile.CopyToAsync(stream);

            // ── set payment fields ────────────────────────────
            payment.ReceiptPath = $"/receipts/{DateTime.UtcNow.Year}/{safeFileName}";
            payment.Status = PaymentStatus.Initiated;
            payment.CreatedByUserId = user!.Id;
            payment.CreatedAt = DateTime.UtcNow;

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment submitted. Awaiting bursar approval.";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        //  APPROVE  (POST)  ← Bursar / Admin only
        //  Confirms payment → status = Approved
        //  Deducts amount from student balance
        //  Creates a TransactionLog entry
        // ═══════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Bursar")]
        public async Task<IActionResult> Approve(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
                return NotFound();

            if (payment.Status == PaymentStatus.Approved)
            {
                TempData["Warning"] = "Payment is already approved.";
                return RedirectToAction(nameof(Index));
            }

            var bursar = await _userManager.GetUserAsync(User);

            // 1. Update payment status
            payment.Status = PaymentStatus.Approved;
            payment.ApprovedByUserId = bursar!.Id;
            payment.ApprovedAt = DateTime.UtcNow;

            // 2. Deduct balance from student
            var student = payment.Student!;
            student.Balance -= payment.Amount;

            // 3. Create transaction log
            var log = new TransactionLog
            {
                StudentId = student.Id,
                PaymentId = payment.Id,
                Type = TransactionType.Debit,
                Amount = payment.Amount,
                BalanceAfter = student.Balance,
                Description = $"Payment approved by bursar – {payment.Purpose ?? "N/A"}",
                PerformedByUserId = bursar.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.TransactionLogs.Add(log);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Payment #{payment.Id} approved. Student balance updated.";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        //  DETAILS
        // ═══════════════════════════════════════════════════════
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            return payment == null ? NotFound() : View(payment);
        }

        // ═══════════════════════════════════════════════════════
        //  DELETE
        // ═══════════════════════════════════════════════════════
        [Authorize(Roles = "Admin,Bursar")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            return payment == null ? NotFound() : View(payment);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Bursar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════
        private void RepopulateDropdowns(User user, Payment payment)
        {
            if (!User.IsInRole("Student"))
            {
                ViewData["StudentId"] = new SelectList(
                    _context.Students.OrderBy(s => s.FullName),
                    "Id", "FullName", payment.StudentId);
            }
            else
            {
                ViewBag.StudentName = user.FullName;
            }
        }
    }
}
