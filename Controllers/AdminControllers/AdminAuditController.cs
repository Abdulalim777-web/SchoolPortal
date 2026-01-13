using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.Data;
using SchoolPortal.Models;
using System.Linq;

using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminAuditController : Controller
{
    private readonly SchoolPortalDbContext _context;

    public AdminAuditController(SchoolPortalDbContext context)
    {
        _context = context;
    }

    public IActionResult LoginAttempts()
    {
        // Combine login audits and navigation audits into a single timeline
        var loginLogs = _context.LoginAudits
            .Select(l => new
            {
                Id = l.Id,
                Time = l.AttemptedAt,
                Email = l.Email,
                Type = l.IsSuccessful ? "Login Success" : "Login Failed",
                Url = (string?)null,
                Reason = l.FailureReason,
                Ip = l.IpAddress,
                UA = l.UserAgent,
                Source = "LoginAudit"
            });

        var navLogs = _context.NavigationAudits
            .Select(n => new
            {
                Id = n.Id,
                Time = n.EventAt,
                Email = n.Email ?? string.Empty,
                Type = n.ActionType,
                Url = n.Url,
                Reason = (string?)null,
                Ip = n.IpAddress,
                UA = n.UserAgent,
                Source = "NavigationAudit"
            });

        var combined = loginLogs
            .Union(navLogs!)
            .OrderByDescending(x => x.Time)
            .Take(500)
            .ToList()
            .Select(x => new AuditEntryViewModel
            {
                Id = x.Id,
                Source = x.Source,
                EventAt = x.Time,
                Email = x.Email,
                ActionType = x.Type,
                Url = x.Url,
                Reason = x.Reason,
                IpAddress = x.Ip,
                UserAgent = x.UA
            });

        return View(combined);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLoginAttempt(int id)
    {
        var entry = await _context.LoginAudits.FindAsync(id);
        if (entry == null)
        {
            TempData["ErrorMessage"] = "Login audit entry not found.";
            return RedirectToAction(nameof(LoginAttempts));
        }

        _context.LoginAudits.Remove(entry);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Deleted login audit entry.";
        return RedirectToAction(nameof(LoginAttempts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearAllLoginAttempts()
    {
        var all = _context.LoginAudits.ToList();
        if (!all.Any())
        {
            TempData["SuccessMessage"] = "No login attempts to clear.";
            return RedirectToAction(nameof(LoginAttempts));
        }

        _context.LoginAudits.RemoveRange(all);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Cleared {all.Count} login audit entries.";
        return RedirectToAction(nameof(LoginAttempts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearNonUsers()
    {
        // Entries where UserId is null or the referenced user no longer exists
        var orphaned = _context.LoginAudits
            .Where(a => a.UserId == null || !_context.Users.Any(u => u.Id == a.UserId));

        var list = orphaned.ToList();
        if (list.Any())
        {
            _context.LoginAudits.RemoveRange(list);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Cleared {list.Count} non-user login audit entries.";
        }
        else
        {
            TempData["SuccessMessage"] = "No non-user entries to clear.";
        }

        return RedirectToAction(nameof(LoginAttempts));
    }
}
