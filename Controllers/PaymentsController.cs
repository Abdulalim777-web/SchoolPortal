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

        public PaymentsController(
            SchoolPortalDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            IQueryable<Payment> payments = _context.Payments
                .Include(p => p.Student);

            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                payments = payments.Where(p => p.CreatedByUserId == user.Id);
            }

            return View(await payments.ToListAsync());
        }

        // ================= CREATE (GET) =================
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

                // Auto-create Student record if missing
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

                var model = new Payment
                {
                    StudentId = student.Id,
                    DatePaid = DateTime.Now
                };

                ViewBag.StudentName = student.FullName;
                return View(model);
            }

            // Admin / Bursar
            ViewData["StudentId"] = new SelectList(
                _context.Students.OrderBy(s => s.FullName),
                "Id",
                "FullName"
            );

            return View(new Payment { DatePaid = DateTime.Now });
        }


        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            var user = await _userManager.GetUserAsync(User);

            if (await _userManager.IsInRoleAsync(user, "Student"))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                if (student == null)
                {
                    return Forbid();
                }

                payment.StudentId = student.Id;
            }
            else
            {
                if (!await _context.Students.AnyAsync(s => s.Id == payment.StudentId))
                {
                    ModelState.AddModelError("StudentId", "Invalid student selected.");
                }
            }

            payment.CreatedByUserId = user.Id;

            if (!ModelState.IsValid)
            {
                if (!User.IsInRole("Student"))
                {
                    ViewData["StudentId"] = new SelectList(
                        _context.Students.OrderBy(s => s.FullName),
                        "Id",
                        "FullName",
                        payment.StudentId
                    );
                }
                else
                {
                    ViewBag.StudentName = user.FullName;
                }

                return View(payment);
            }

            _context.Payments.Add(payment);

            var studentToUpdate = await _context.Students.FindAsync(payment.StudentId);
            if (studentToUpdate != null)
            {
                studentToUpdate.Balance -= payment.Amount;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(p => p.Id == id);

            return payment == null ? NotFound() : View(payment);
        }

        // ================= DELETE =================
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
    }
}
