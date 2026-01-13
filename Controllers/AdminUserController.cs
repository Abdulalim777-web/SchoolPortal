using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using SchoolPortal.Models;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly UserManager<User> _userManager;

    public AdminUsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View(_userManager.Users.ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Prevent duplicate email
        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError("", "A user with this email already exists.");
            return View(model);
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }

        var allowed = new[] { "Student", "Teacher", "Bursar", "Admin" };
        var role = allowed.Contains(model.SelectedRole) ? model.SelectedRole : "Student";

        await _userManager.AddToRoleAsync(user, role);

        TempData["SuccessMessage"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleSuspension(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        user.IsSuspended = !user.IsSuspended;
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
            await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Index));
    }
}
