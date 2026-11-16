using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Application.Services.Security;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Account;
using System.Security.Claims;

namespace PbmEventPortal.Web.Pages.Account;

public class ChangePasswordModel : PageModel
{
    private const int PasswordHistoryReuseLimit = 5;

    private readonly PbmDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly IAuditLogService _auditLogService;

    public ChangePasswordModel(
        PbmDbContext dbContext,
        IPasswordHashService passwordHashService,
        IPasswordPolicyService passwordPolicyService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _passwordPolicyService = passwordPolicyService;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public ChangePasswordInputDTO Input { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return Challenge();
        }

        var userProfile = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => user.Id == userId.Value && user.IsActive)
            .Select(user => new
            {
                user.Id,
                user.FullNameAsPerId,
                user.FullNameGurmukhi,
                user.Email,
                user.MobileNumber
            })
            .FirstOrDefaultAsync();

        if (userProfile == null)
        {
            ModelState.AddModelError(string.Empty, "Your user profile could not be found.");
            return Page();
        }

        var credential = await _dbContext.UserCredentials
            .FirstOrDefaultAsync(credential =>
                credential.UserId == userProfile.Id &&
                credential.IsActive);

        if (credential == null)
        {
            ModelState.AddModelError(string.Empty, "Your login credential could not be found.");
            return Page();
        }

        var currentPasswordIsValid = _passwordHashService.VerifyPassword(
            credential.PasswordHash,
            Input.CurrentPassword);

        if (!currentPasswordIsValid)
        {
            await LogChangePasswordFailedAsync(
                credential.Id,
                userProfile.Id,
                "INVALID_CURRENT_PASSWORD",
                "Change password failed: invalid current password.");

            ModelState.AddModelError(nameof(Input.CurrentPassword), "Current password is incorrect.");
            return Page();
        }

        var policyResult = await _passwordPolicyService.ValidateAsync(
            new PasswordPolicyValidationRequest
            {
                CurrentPassword = Input.CurrentPassword,
                NewPassword = Input.NewPassword,
                Email = userProfile.Email,
                MobileNumber = userProfile.MobileNumber,
                FullNameAsPerId = userProfile.FullNameAsPerId
            });

        if (!policyResult.IsValid)
        {
            await LogChangePasswordFailedAsync(
                credential.Id,
                userProfile.Id,
                "PASSWORD_POLICY_FAILED",
                "Change password failed: new password did not meet password policy.",
                policyErrorCount: policyResult.Errors.Count);

            foreach (var error in policyResult.Errors)
            {
                ModelState.AddModelError(nameof(Input.NewPassword), error);
            }

            return Page();
        }

        var passwordWasUsedRecently = await PasswordWasUsedRecentlyAsync(
            userProfile.Id,
            Input.NewPassword);

        if (passwordWasUsedRecently)
        {
            await LogChangePasswordFailedAsync(
                credential.Id,
                userProfile.Id,
                "PASSWORD_HISTORY_REUSE",
                "Change password failed: new password was used recently.");

            ModelState.AddModelError(
                nameof(Input.NewPassword),
                $"New password cannot reuse any of your last {PasswordHistoryReuseLimit} passwords.");

            return Page();
        }

        var now = DateTime.UtcNow;

        var oldCredentialSnapshot = new
        {
            credential.Id,
            credential.UserId,
            password_hash_stored = true,
            password_hash_in_audit = false,
            credential.MustChangePassword,
            credential.FailedLoginCount,
            credential.LockedUntil,
            credential.LastLoginAt,
            credential.LastPasswordChangedAt,
            credential.IsActive,
            credential.SoftLockCount,
            credential.LastSoftLockedAt,
            credential.HardLockedAt,
            credential.HardLockedBy,
            credential.HardLockReason,
            credential.RequiresAdminUnlock,
            credential.CreatedAt,
            credential.CreatedBy,
            credential.UpdatedAt,
            credential.UpdatedBy
        };

        var newPasswordHash = _passwordHashService.HashPassword(Input.NewPassword);

        credential.PasswordHash = newPasswordHash;
        credential.MustChangePassword = false;
        credential.FailedLoginCount = 0;
        credential.LockedUntil = null;
        credential.LastPasswordChangedAt = now;
        credential.UpdatedAt = now;
        credential.UpdatedBy = userProfile.Id;

        var passwordHistory = new UserPasswordHistory
        {
            Id = Guid.NewGuid(),
            UserId = userProfile.Id,
            PasswordHash = newPasswordHash,
            ChangedAt = now,
            ChangedBy = userProfile.Id,
            ChangeReason = "USER_CHANGE_PASSWORD",
            CreatedAt = now,
            CreatedBy = userProfile.Id,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.UserPasswordHistories.Add(passwordHistory);

        var newCredentialSnapshot = new
        {
            credential.Id,
            credential.UserId,
            password_hash_stored = true,
            password_hash_in_audit = false,
            password_hash_changed = true,
            credential.MustChangePassword,
            credential.FailedLoginCount,
            credential.LockedUntil,
            credential.LastLoginAt,
            credential.LastPasswordChangedAt,
            credential.IsActive,
            credential.SoftLockCount,
            credential.LastSoftLockedAt,
            credential.HardLockedAt,
            credential.HardLockedBy,
            credential.HardLockReason,
            credential.RequiresAdminUnlock,
            credential.CreatedAt,
            credential.CreatedBy,
            credential.UpdatedAt,
            credential.UpdatedBy
        };

        await _auditLogService.LogAsync(
            entityName: "user_credentials",
            entityId: credential.Id,
            actionName: "CHANGE_PASSWORD",
            changedBy: userProfile.Id,
            changeReason: "User changed own password.",
            oldValues: oldCredentialSnapshot,
            newValues: newCredentialSnapshot);

        await _auditLogService.LogAsync(
            entityName: "user_password_history",
            entityId: passwordHistory.Id,
            actionName: "CREATE",
            changedBy: userProfile.Id,
            changeReason: "User password history record created after password change.",
            oldValues: null,
            newValues: new
            {
                passwordHistory.Id,
                passwordHistory.UserId,
                password_hash_stored = true,
                password_hash_in_audit = false,
                passwordHistory.ChangedAt,
                passwordHistory.ChangedBy,
                passwordHistory.ChangeReason,
                passwordHistory.CreatedAt,
                passwordHistory.CreatedBy,
                passwordHistory.UpdatedAt,
                passwordHistory.UpdatedBy
            });

        await RefreshAuthenticationCookieAsync(
            userProfile.Id,
            userProfile.FullNameAsPerId,
            userProfile.FullNameGurmukhi,
            userProfile.Email);

        SuccessMessage = "Your password has been changed successfully.";

        return RedirectToPage();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private async Task<bool> PasswordWasUsedRecentlyAsync(
        Guid userId,
        string newPassword)
    {
        var recentPasswordHashes = await _dbContext.UserPasswordHistories
            .AsNoTracking()
            .Where(history => history.UserId == userId)
            .OrderByDescending(history => history.ChangedAt)
            .Take(PasswordHistoryReuseLimit)
            .Select(history => history.PasswordHash)
            .ToListAsync();

        return recentPasswordHashes.Any(passwordHash =>
            _passwordHashService.VerifyPassword(passwordHash, newPassword));
    }

    private async Task RefreshAuthenticationCookieAsync(
        Guid userId,
        string fullNameAsPerId,
        string fullNameGurmukhi,
        string email)
    {
        var roleCodes = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(roleAssignment =>
                roleAssignment.UserId == userId &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified)
            .Select(roleAssignment => roleAssignment.RoleCode)
            .Distinct()
            .ToListAsync();

        var rememberMe = string.Equals(
            User.FindFirstValue("RememberMe"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId.ToString()),
        new(ClaimTypes.Name, fullNameAsPerId),
        new(ClaimTypes.Email, email),
        new("FullNameGurmukhi", fullNameGurmukhi),
        new("MustChangePassword", "false"),
        new("RememberMe", rememberMe ? "true" : "false")
    };

        foreach (var roleCode in roleCodes)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);
    }

    private async Task LogChangePasswordFailedAsync(
        Guid credentialId,
        Guid userId,
        string failureReason,
        string changeReason,
        int? policyErrorCount = null)
    {
        await _auditLogService.LogAsync(
            entityName: "user_credentials",
            entityId: credentialId,
            actionName: "CHANGE_PASSWORD_FAILED",
            changedBy: userId,
            changeReason: changeReason,
            oldValues: null,
            newValues: new
            {
                user_id = userId,
                failure_reason = failureReason,
                policy_error_count = policyErrorCount,
                password_value_in_audit = false,
                password_hash_in_audit = false
            });
    }
}
