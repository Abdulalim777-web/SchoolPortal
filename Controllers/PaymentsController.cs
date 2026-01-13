using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.Data;
using SchoolPortal.Models;
using Microsoft.AspNetCore.Authorization;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Admin,Bursar,Student")]
    public class PaymentsController : Controller
    {
        private readonly SchoolPortalDbContext _context;

        public PaymentsController(SchoolPortalDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = _context.Payments.Include(p => p.Student).AsQueryable();

            // If current user is a Student, show only payments they created
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Student"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                payments = payments.Where(p => p.CreatedByUserId == userId);
            }

            return View(await payments.ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        [Authorize(Roles = "Admin,Bursar,Student")]
        public IActionResult Create()
        {
            if (User.IsInRole("Student"))
            {
                // For students, they can only create payments for themselves
                // Since there's no direct User-Student link, students will see a single student
                ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName");
            }
            else
            {
                // For Admin and Bursar, they can create payments for any student
                ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName");
            }
            return View();
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Bursar,Student")]
        public async Task<IActionResult> Create([Bind("Id,StudentId,Amount,DatePaid,Purpose")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                // Set creator
                payment.CreatedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                payment.CreatedAt = DateTime.UtcNow;

                _context.Add(payment);
                await _context.SaveChangesAsync();

                // Generate an RRR-style number using the newly assigned Id
                payment.RrrNumber = $"RRR{payment.Id:D6}";
                _context.Update(payment);
                await _context.SaveChangesAsync();

                // --- Auto-update student balance ---
                var student = await _context.Students.FindAsync(payment.StudentId);
                if (student != null)
                {
                    student.Balance += payment.Amount;
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName", payment.StudentId);
            return View(payment);
        }


        // GET: Payments/Edit/5
        [Authorize(Roles = "Admin,Bursar")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "Id", payment.StudentId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Bursar")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,Amount,DatePaid,Purpose")] Payment payment)
        {
            if (id != payment.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // --- Get existing payment from DB ---
                    var existingPayment = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (existingPayment != null)
                    {
                        // Update payment
                        _context.Update(payment);
                        await _context.SaveChangesAsync();

                        // Adjust student balance
                        var student = await _context.Students.FindAsync(payment.StudentId);
                        if (student != null)
                        {
                            // Remove old amount, add new amount
                            student.Balance = student.Balance - existingPayment.Amount + payment.Amount;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                        if (!PaymentExists(payment.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["StudentId"] = new SelectList(_context.Students, "Id", "FullName", payment.StudentId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        [Authorize(Roles = "Admin,Bursar")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Bursar")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.Id == id);
        }
    }
}
