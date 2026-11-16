using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PbmEventPortal.Application.Services.Auditing;

namespace PbmEventPortal.Web.Pages;

public class LogoutModel : PageModel
{
    private readonly IAuditLogService _auditLogService;

    public LogoutModel(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LogLogoutAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LogLogoutAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToPage("/Index");
    }

    private async Task LogLogoutAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var userId = Guid.TryParse(userIdClaim, out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;

        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);

        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: userId,
            actionName: "LOGOUT",
            changedBy: userId,
            changeReason: "User logged out.",
            oldValues: null,
            newValues: new
            {
                user_id = userId,
                email,
                name
            });
    }
}