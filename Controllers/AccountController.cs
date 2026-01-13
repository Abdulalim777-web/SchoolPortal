using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SchoolPortal.Data;
using SchoolPortal.Models;
using SchoolPortal.Models.ViewModels;
using System.Security.Claims;
using System.Linq;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ===================== AUDIT =====================
    private async Task LogAttempt(string? userId, string email, bool success, string? reason)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SchoolPortalDbContext>();

        db.LoginAudits.Add(new LoginAudit
        {
            UserId = userId,
            Email = email,
            IsSuccessful = success,
            FailureReason = reason,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString()
        });

        await db.SaveChangesAsync();
    }

    // ===================== LOGIN =====================
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            await LogAttempt(null, model.Email, false, "User not found");
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        if (user.IsSuspended)
        {
            await LogAttempt(user.Id, model.Email, false, "Account suspended");
            ModelState.AddModelError("", "Account suspended.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, true);

        if (!result.Succeeded)
        {
            await LogAttempt(user.Id, model.Email, false, "Invalid password");
            ModelState.AddModelError("", "Invalid credentials.");
            return View(model);
        }

        await LogAttempt(user.Id, model.Email, true, null);
        return await RedirectByRole(user);
    }

    // ===================== REGISTER =====================
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }

        // Determine role to assign based on selection, but never allow self-registration to Admin
        var selected = model.SelectedRole ?? string.Empty;
        var allowed = new[] { "Student", "Teacher", "Bursar", "Admin" };
        var roleToAssign = allowed.Contains(selected) ? selected : "Student";

        // If user selected Admin but the current principal is not an Admin, downgrade to Student
        if (roleToAssign == "Admin" && !(User.Identity?.IsAuthenticated == true && User.IsInRole("Admin")))
        {
            roleToAssign = "Student";
        }

        await _userManager.AddToRoleAsync(user, roleToAssign);

        await _signInManager.SignInAsync(user, false);
        return RedirectToAction("Index", "Home");
    }

    // ===================== GOOGLE LOGIN =====================
    [HttpPost]
    public IActionResult ExternalLogin(string provider)
    {
        // Ensure the provider is available (e.g. Google configured)
        var schemes = _signInManager.GetExternalAuthenticationSchemesAsync().Result;
        if (!schemes.Any(s => string.Equals(s.Name, provider, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["ErrorMessage"] = $"External provider '{provider}' is not configured.";
            return RedirectToAction(nameof(Login));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback));
        var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(props, provider);
    }

    public async Task<IActionResult> ExternalLoginCallback()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToAction(nameof(Login));

        // Try to get email from claims; if missing, try other identifiers
        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ??
                    info.Principal.FindFirstValue(ClaimTypes.Name) ??
                    string.Empty;

        // Try to find user by external login first, then by email
        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user == null && !string.IsNullOrEmpty(email))
        {
            user = await _userManager.FindByEmailAsync(email);
        }

        if (user != null && user.IsSuspended)
        {
            await LogAttempt(user.Id, email, false, "Suspended (Google)");
            return RedirectToAction(nameof(Login));
        }

        var signIn = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, false);

        if (signIn.Succeeded)
        {
            // If user was not loaded earlier, attempt to resolve from login
            user ??= await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            await LogAttempt(user?.Id, email, true, null);
            return await RedirectByRole(user!);
        }

        // FIRST TIME GOOGLE USER
        if (user == null)
        {
            if (string.IsNullOrEmpty(email))
            {
                await LogAttempt(null, "", false, "Google login missing email");
                TempData["ErrorMessage"] = "Google did not provide an email address. Please use another login method.";
                return RedirectToAction(nameof(Login));
            }

            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                await LogAttempt(null, email, false, "Failed to create user from Google");
                TempData["ErrorMessage"] = "Could not create user account from Google login.";
                return RedirectToAction(nameof(Login));
            }

            await _userManager.AddToRoleAsync(user, "Student");
        }

        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, false);

        await LogAttempt(user.Id, email, true, "Google signup");
        return await RedirectByRole(user);
    }

    // ===================== LOGOUT =====================
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ===================== ROLE REDIRECT =====================
    private async Task<IActionResult> RedirectByRole(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin")) return RedirectToAction("Index", "Dashboard");
        if (roles.Contains("Bursar")) return RedirectToAction("Index", "Payments");
        if (roles.Contains("Teacher")) return RedirectToAction("Index", "Students");

        return RedirectToAction("Index", "Home");
    }
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);

        var roles = await _userManager.GetRolesAsync(user!);

        var model = new ProfileViewModel
        {
            FullName = user!.FullName,
            Email = user.Email,
            Role = roles.FirstOrDefault() ?? "User"
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

}
