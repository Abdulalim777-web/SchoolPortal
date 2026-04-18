using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SchoolPortal.Data;
using SchoolPortal.Models;
using SchoolPortal.Models.ViewModels;
using System.Security.Claims;

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

    // ═══════════════════════════════════════════════
    //  AUDIT
    // ═══════════════════════════════════════════════
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

    // ═══════════════════════════════════════════════
    //  LOGIN
    // ═══════════════════════════════════════════════
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
            ModelState.AddModelError("", "Your account has been suspended.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            var reason = result.IsLockedOut ? "Account locked out" : "Invalid password";
            await LogAttempt(user.Id, model.Email, false, reason);
            ModelState.AddModelError("", result.IsLockedOut
                ? "Account locked. Try again later."
                : "Invalid credentials.");
            return View(model);
        }

        await LogAttempt(user.Id, model.Email, true, null);
        return await RedirectByRole(user);
    }

    // ═══════════════════════════════════════════════
    //  REGISTER
    // ═══════════════════════════════════════════════
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

        var selected = model.SelectedRole ?? string.Empty;
        var allowed = new[] { "Student", "Teacher", "Bursar" }; // Admin never self-assignable

        // Non-admins can only pick from the allowed list
        var roleToAssign = allowed.Contains(selected) ? selected : "Student";

        // Admin can assign any role including "Admin" via the Register form
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            var adminAllowed = new[] { "Student", "Teacher", "Bursar", "Admin" };
            roleToAssign = adminAllowed.Contains(selected) ? selected : "Student";
        }

        await _userManager.AddToRoleAsync(user, roleToAssign);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return await RedirectByRole(user);
    }

    // ═══════════════════════════════════════════════
    //  GOOGLE OAUTH  – Step 1: challenge
    // ═══════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(
            nameof(ExternalLoginCallback),
            "Account",
            new { returnUrl });

        var props = _signInManager.ConfigureExternalAuthenticationProperties(
            provider, redirectUrl);

        return Challenge(props, provider);
    }

    // ═══════════════════════════════════════════════
    //  GOOGLE OAUTH  – Step 2: callback
    //
    //  Flow:
    //  A) Known user with external login → sign in directly
    //  B) Email already exists (old password user) → link Google login
    //  C) Brand new email → redirect to role-selection page
    // ═══════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();

        if (info == null)
        {
            TempData["ErrorMessage"] = "Google login failed. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Google did not provide an email. Please use email/password login.";
            return RedirectToAction(nameof(Login));
        }

        // A) Try to sign in via the stored external login record
        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            var existingUser = await _userManager.FindByLoginAsync(
                info.LoginProvider, info.ProviderKey);

            if (existingUser is { IsSuspended: true })
            {
                await _signInManager.SignOutAsync();
                await LogAttempt(existingUser.Id, email, false, "Suspended (Google)");
                TempData["ErrorMessage"] = "Your account has been suspended.";
                return RedirectToAction(nameof(Login));
            }

            await LogAttempt(existingUser?.Id, email, true, null);
            return await RedirectByRole(existingUser!);
        }

        // B) Email already registered with a password account → link Google
        var userByEmail = await _userManager.FindByEmailAsync(email);

        if (userByEmail != null)
        {
            if (userByEmail.IsSuspended)
            {
                await LogAttempt(userByEmail.Id, email, false, "Suspended (Google link)");
                TempData["ErrorMessage"] = "Your account has been suspended.";
                return RedirectToAction(nameof(Login));
            }

            // Add Google as an additional login method for this existing account
            var addLoginResult = await _userManager.AddLoginAsync(userByEmail, info);
            if (!addLoginResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Could not link Google to your account.";
                return RedirectToAction(nameof(Login));
            }

            await _signInManager.SignInAsync(userByEmail, isPersistent: false);
            await LogAttempt(userByEmail.Id, email, true, "Google linked to existing account");
            return await RedirectByRole(userByEmail);
        }

        // C) Completely new Google user → store info in TempData and ask for role
        TempData["GoogleEmail"] = email;
        TempData["GoogleName"] = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
        TempData["GoogleProvider"] = info.LoginProvider;
        TempData["GoogleProviderKey"] = info.ProviderKey;

        return RedirectToAction(nameof(ExternalLoginConfirmation));
    }

    // ═══════════════════════════════════════════════
    //  GOOGLE OAUTH  – Step 3: new user picks a role
    // ═══════════════════════════════════════════════
    [HttpGet]
    public IActionResult ExternalLoginConfirmation()
    {
        // If TempData is missing the user navigated here directly – reject
        if (TempData["GoogleEmail"] == null)
            return RedirectToAction(nameof(Login));

        // Keep TempData alive for the POST
        TempData.Keep();

        var vm = new ExternalLoginConfirmationViewModel
        {
            Email = TempData["GoogleEmail"]?.ToString() ?? "",
            FullName = TempData["GoogleName"]?.ToString() ?? ""
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(
        ExternalLoginConfirmationViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = TempData["GoogleEmail"]?.ToString();
        var name = TempData["GoogleName"]?.ToString();
        var provider = TempData["GoogleProvider"]?.ToString();
        var providerKey = TempData["GoogleProviderKey"]?.ToString();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(provider) ||
            string.IsNullOrEmpty(providerKey))
        {
            TempData["ErrorMessage"] = "Session expired. Please sign in with Google again.";
            return RedirectToAction(nameof(Login));
        }

        // Double-check nobody registered this email while we waited
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            TempData["ErrorMessage"] = "An account with that email already exists. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // Create the user
        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = name ?? email,
            EmailConfirmed = true   // Google already confirmed the email
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            foreach (var e in createResult.Errors)
                ModelState.AddModelError("", e.Description);
            return View(model);
        }

        // Assign role – Admin cannot be self-assigned via Google
        var allowed = new[] { "Student", "Teacher", "Bursar" };
        var roleToAssign = allowed.Contains(model.SelectedRole) ? model.SelectedRole : "Student";
        await _userManager.AddToRoleAsync(user, roleToAssign);

        // Link the Google external login
        var loginInfo = new UserLoginInfo(provider, providerKey, "Google");
        await _userManager.AddLoginAsync(user, loginInfo);

        await _signInManager.SignInAsync(user, isPersistent: false);
        await LogAttempt(user.Id, email, true, "Google new user");

        return await RedirectByRole(user);
    }

    // ═══════════════════════════════════════════════
    //  LOGOUT
    // ═══════════════════════════════════════════════
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ═══════════════════════════════════════════════
    //  PROFILE
    // ═══════════════════════════════════════════════
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);

        return View(new ProfileViewModel
        {
            FullName = user!.FullName,
            Email = user.Email,
            Role = roles.FirstOrDefault() ?? "User"
        });
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ═══════════════════════════════════════════════
    //  ROLE-BASED REDIRECT
    // ═══════════════════════════════════════════════
    private async Task<IActionResult> RedirectByRole(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Admin")) return RedirectToAction("Index", "Dashboard");
        if (roles.Contains("Bursar")) return RedirectToAction("Index", "Payments");
        if (roles.Contains("Teacher")) return RedirectToAction("Index", "Students");

        // Student → home (or student dashboard if you have one)
        return RedirectToAction("Index", "Home");
    }
}
