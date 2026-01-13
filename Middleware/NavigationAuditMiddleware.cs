using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;
using SchoolPortal.Data;
using SchoolPortal.Models;

namespace SchoolPortal.Middleware
{
    public class NavigationAuditMiddleware
    {
        private readonly RequestDelegate _next;

        public NavigationAuditMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            try
            {
                // Exclude static resources and assets
                var path = context.Request.Path.ToString();
                if (path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith("favicon.ico", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Only log GET/POST
                if (!(context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Post))
                    return;

                // Determine action type: consider redirect to login as Denied
                var status = context.Response?.StatusCode ?? 0;
                string actionType;
                if (status == 401 || status == 403)
                {
                    actionType = "Denied";
                }
                else if (status == 302)
                {
                    var loc = context.Response!.Headers["Location"].ToString();
                    if (!string.IsNullOrEmpty(loc) && loc.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                        actionType = "Denied";
                    else
                        actionType = "Accessed";
                }
                else
                {
                    actionType = "Accessed";
                }

                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SchoolPortalDbContext>();

                var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var email = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var nav = new NavigationAudit
                {
                    UserId = userId,
                    Email = email,
                    Url = context.Request.Path + context.Request.QueryString,
                    ActionType = actionType,
                    EventAt = DateTime.UtcNow,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers["User-Agent"].ToString()
                };

                db.NavigationAudits.Add(nav);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Don't throw from middleware on logging failures
            }
        }
    }
}
