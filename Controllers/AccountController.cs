using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.Models;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ===================== LOGIN =====================

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity!.IsAuthenticated)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false
        );

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // If ReturnUrl exists and is local → respect it
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return await RedirectByRole(user);
    }

    // ===================== REGISTER =====================

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity!.IsAuthenticated)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (string.IsNullOrWhiteSpace(model.SelectedRole))
        {
            ModelState.AddModelError("", "Please select a role.");
            return View(model);
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.SelectedRole);

        // Auto-login after registration
        await _signInManager.SignInAsync(user, isPersistent: false);

        return await RedirectByRole(user);
    }

    // ===================== LOGOUT =====================

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }

    // ===================== ACCESS DENIED =====================

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ===================== ROLE REDIRECT (SINGLE SOURCE) =====================

    private async Task<IActionResult> RedirectByRole(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin"))
            return RedirectToAction("Index", "Dashboard");

        if (roles.Contains("Bursar"))
            return RedirectToAction("Index", "Dashboard");

        if (roles.Contains("Teacher"))
            return RedirectToAction("Index", "Dashboard");

        if (roles.Contains("Student"))
            return RedirectToAction("Index", "Dashboard");

        // Fallback (should never happen)
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login", "Account");
    }
}
