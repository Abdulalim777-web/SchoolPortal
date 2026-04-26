using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.Data;
using SchoolPortal.Models;
using SchoolPortal.Models.Configuration;
using SchoolPortal.Models.ViewModels;
using SchoolPortal.Services;
using System.Linq;

namespace SchoolPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly SchoolPortalDbContext _context;
        private readonly IBulkOperationService _bulkOperationService;
        private readonly UserManager<User> _userManager;

        public AdminController(
            SchoolPortalDbContext context,
            IBulkOperationService bulkOperationService,
            UserManager<User> userManager)
        {
            _context = context;
            _bulkOperationService = bulkOperationService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            ViewBag.TotalStudents = _context.Students.Count();
            ViewBag.TotalStaff = _context.Staffs.Count();
            ViewBag.TotalPayments = _context.Payments.Count();
            ViewBag.PendingApprovals = 5; // Replace with real logic

            return View();
        }

        // ═══════════════════════════════════════════════════════
        //  BULK BALANCE ALLOCATION
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// GET: Display bulk balance allocation form
        /// </summary>
        public IActionResult BulkAllocateBalance()
        {
            var model = new BulkBalanceAllocationViewModel
            {
                AvailableClasses = ClassBalanceConfiguration.GetAllClasses(),
                ClassBalances = ClassBalanceConfiguration.GetAllClassBalances()
            };
            return View(model);
        }

        /// <summary>
        /// POST: Process bulk balance allocation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAllocateBalance(BulkBalanceAllocationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableClasses = ClassBalanceConfiguration.GetAllClasses();
                model.ClassBalances = ClassBalanceConfiguration.GetAllClassBalances();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(BulkAllocateBalance));
            }

            var result = await _bulkOperationService.AllocateBalanceToClassAsync(
                model.SelectedClass!,
                model.Term,
                model.Notes,
                user.Id);

            if (result.IsSuccess)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(BulkOperationHistory));
            }
            else
            {
                TempData["Error"] = result.ErrorMessage;
                model.AvailableClasses = ClassBalanceConfiguration.GetAllClasses();
                model.ClassBalances = ClassBalanceConfiguration.GetAllClassBalances();
                return View(model);
            }
        }

        /// <summary>
        /// GET: View bulk operation history
        /// </summary>
        public async Task<IActionResult> BulkOperationHistory(int pageNumber = 1)
        {
            const int pageSize = 20;
            var history = await _bulkOperationService.GetBulkOperationHistoryAsync(pageNumber, pageSize);
            
            var totalCount = _context.BulkOperationAudits.Count();
            var viewModel = new BulkOperationHistoryViewModel
            {
                Operations = history,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return View(viewModel);
        }

        /// <summary>
        /// GET: View details of a specific bulk operation
        /// </summary>
        public async Task<IActionResult> BulkOperationDetails(int id)
        {
            var operation = await _bulkOperationService.GetBulkOperationByIdAsync(id);
            if (operation == null)
            {
                TempData["Error"] = "Bulk operation not found.";
                return RedirectToAction(nameof(BulkOperationHistory));
            }

            return View(operation);
        }

        // ═══════════════════════════════════════════════════════
        //  DUPLICATE PAYMENT CHECK
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// GET: Display duplicate payment check form
        /// </summary>
        public IActionResult CheckDuplicatePayment()
        {
            var students = _context.Students.ToList();
            ViewBag.Students = students;
            return View();
        }

        /// <summary>
        /// POST: Check for duplicate payments
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckDuplicatePayment(int studentId, PaymentPurpose purpose, string? term)
        {
            var result = await _bulkOperationService.CheckDuplicatePaymentAsync(studentId, purpose, term);
            
            if (result.Error != null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(CheckDuplicatePayment));
            }

            return View("DuplicatePaymentResult", result);
        }
    }
}
