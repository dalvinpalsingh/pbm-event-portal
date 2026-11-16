using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Application.Services.Security;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Auth;

namespace PbmEventPortal.Web.Pages;

public class LoginModel : PageModel
{
    private const int MaxFailedLoginAttempts = 5;
    private const int HardLockSoftLockThresholdIn24Hours = 3;
    private const int HardLockSoftLockThresholdIn7Days = 5;

    private static readonly TimeSpan SoftLockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan HardLockShortWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan HardLockLongWindow = TimeSpan.FromDays(7);

    private readonly PbmDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IAuditLogService _auditLogService;

    public LoginModel(
        PbmDbContext dbContext,
        IPasswordHashService passwordHashService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _passwordHashService = passwordHashService;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public LoginInputDTO Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var normalizedEmail = Input.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        var loginUser = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => user.IsActive && user.Email.ToLower() == normalizedEmail)
            .Select(user => new LoginUserDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi,
                Email = user.Email,
                IsActive = user.IsActive
            })
            .FirstOrDefaultAsync();

        if (loginUser == null)
        {
            await LogLoginFailureAsync(
                entityId: null,
                changedBy: null,
                normalizedEmail: normalizedEmail,
                failureReason: "EMAIL_NOT_FOUND_OR_INACTIVE",
                changeReason: "Login failed: email not found or inactive user.");

            AddInvalidLoginError();
            return Page();
        }

        var credential = await _dbContext.UserCredentials
            .FirstOrDefaultAsync(credential =>
                credential.UserId == loginUser.Id &&
                credential.IsActive);

        if (credential == null)
        {
            await LogLoginFailureAsync(
                entityId: loginUser.Id,
                changedBy: loginUser.Id,
                normalizedEmail: normalizedEmail,
                failureReason: "CREDENTIAL_NOT_FOUND_OR_INACTIVE",
                changeReason: "Login failed: credential record not found or inactive.");

            AddInvalidLoginError();
            return Page();
        }

        if (credential.RequiresAdminUnlock)
        {
            await LogLoginBlockedHardLockedAsync(
                loginUser.Id,
                normalizedEmail,
                credential.HardLockedAt,
                credential.HardLockReason);

            ModelState.AddModelError(
                string.Empty,
                "This account is locked. Please contact a System Admin.");

            return Page();
        }

        if (credential.LockedUntil.HasValue && credential.LockedUntil.Value > now)
        {
            await LogLoginBlockedLockedAsync(
                loginUser.Id,
                normalizedEmail,
                credential.LockedUntil.Value);

            ModelState.AddModelError(
                string.Empty,
                "This account is temporarily locked. Please try again later.");

            return Page();
        }

        var passwordIsValid = _passwordHashService.VerifyPassword(
            credential.PasswordHash,
            Input.Password);

        if (!passwordIsValid)
        {
            credential.FailedLoginCount += 1;

            if (credential.FailedLoginCount >= MaxFailedLoginAttempts)
            {
                await ApplySoftLockAndPossibleHardLockAsync(
                    credential,
                    loginUser.Id,
                    normalizedEmail,
                    now);
            }
            else
            {
                credential.UpdatedAt = now;
                credential.UpdatedBy = loginUser.Id;

                await LogLoginFailureAsync(
                    entityId: loginUser.Id,
                    changedBy: loginUser.Id,
                    normalizedEmail: normalizedEmail,
                    failureReason: "INVALID_PASSWORD",
                    changeReason: "Login failed: invalid password.",
                    failedLoginCount: credential.FailedLoginCount,
                    lockedUntil: credential.LockedUntil);
            }

            AddInvalidLoginError();
            return Page();
        }

        var roleCodes = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(roleAssignment =>
                roleAssignment.UserId == loginUser.Id &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified)
            .Select(roleAssignment => roleAssignment.RoleCode)
            .Distinct()
            .ToListAsync();

        credential.FailedLoginCount = 0;
        credential.LockedUntil = null;
        credential.LastLoginAt = now;
        credential.UpdatedAt = now;
        credential.UpdatedBy = loginUser.Id;

        await LogLoginSuccessAsync(
            loginUser,
            roleCodes,
            Input.RememberMe);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, loginUser.Id.ToString()),
            new(ClaimTypes.Name, loginUser.FullNameAsPerId),
            new(ClaimTypes.GivenName, loginUser.FullNameGurmukhi),
            new(ClaimTypes.Email, loginUser.Email),
            new(ClaimTypeEnums.MustChangePassword, credential.MustChangePassword ? "true" : "false"),
            new(ClaimTypeEnums.RememberMe, Input.RememberMe ? "true" : "false")
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
            IsPersistent = Input.RememberMe
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authenticationProperties);

        if (credential.MustChangePassword)
        {
            return LocalRedirect("/Account/ChangePassword");
        }

        return LocalRedirect("/");
    }

    private async Task ApplySoftLockAndPossibleHardLockAsync(
        dynamic credential,
        Guid userId,
        string normalizedEmail,
        DateTime now)
    {
        credential.LockedUntil = now.Add(SoftLockoutDuration);
        credential.SoftLockCount += 1;
        credential.LastSoftLockedAt = now;
        credential.UpdatedAt = now;
        credential.UpdatedBy = userId;

        var softLockCountIn24Hours = await CountRecentSoftLocksAsync(userId, now.Subtract(HardLockShortWindow)) + 1;
        var softLockCountIn7Days = await CountRecentSoftLocksAsync(userId, now.Subtract(HardLockLongWindow)) + 1;

        await LogLoginFailureAsync(
            entityId: userId,
            changedBy: userId,
            normalizedEmail: normalizedEmail,
            failureReason: "INVALID_PASSWORD_SOFT_LOCKED",
            changeReason: "Login failed: invalid password and account was soft-locked.",
            failedLoginCount: credential.FailedLoginCount,
            lockedUntil: credential.LockedUntil);

        await LogAccountSoftLockedAsync(
            userId,
            credential.FailedLoginCount,
            credential.LockedUntil,
            credential.SoftLockCount,
            softLockCountIn24Hours,
            softLockCountIn7Days);

        if (softLockCountIn24Hours >= HardLockSoftLockThresholdIn24Hours ||
            softLockCountIn7Days >= HardLockSoftLockThresholdIn7Days)
        {
            credential.RequiresAdminUnlock = true;
            credential.HardLockedAt = now;
            credential.HardLockedBy = userId;
            credential.HardLockReason = BuildHardLockReason(
                softLockCountIn24Hours,
                softLockCountIn7Days);
            credential.UpdatedAt = now;
            credential.UpdatedBy = userId;

            await LogAccountHardLockedAsync(
                userId,
                credential.HardLockedAt,
                credential.HardLockReason,
                softLockCountIn24Hours,
                softLockCountIn7Days);
        }
    }

    private async Task<int> CountRecentSoftLocksAsync(Guid userId, DateTime since)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .CountAsync(auditLog =>
                auditLog.EntityName == "auth" &&
                auditLog.EntityId == userId &&
                auditLog.ActionName == "ACCOUNT_SOFT_LOCKED" &&
                auditLog.CreatedAt >= since);
    }

    private static string BuildHardLockReason(
        int softLockCountIn24Hours,
        int softLockCountIn7Days)
    {
        if (softLockCountIn24Hours >= HardLockSoftLockThresholdIn24Hours)
        {
            return $"Account hard-locked after {softLockCountIn24Hours} soft lockouts within 24 hours.";
        }

        return $"Account hard-locked after {softLockCountIn7Days} soft lockouts within 7 days.";
    }

    private async Task LogLoginFailureAsync(
        Guid? entityId,
        Guid? changedBy,
        string normalizedEmail,
        string failureReason,
        string changeReason,
        int? failedLoginCount = null,
        DateTime? lockedUntil = null)
    {
        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: entityId,
            actionName: "LOGIN_FAILED",
            changedBy: changedBy,
            changeReason: changeReason,
            oldValues: null,
            newValues: new
            {
                attempted_email = normalizedEmail,
                user_id = entityId,
                failure_reason = failureReason,
                failed_login_count = failedLoginCount,
                locked_until = lockedUntil
            });
    }

    private async Task LogLoginBlockedLockedAsync(
        Guid userId,
        string normalizedEmail,
        DateTime lockedUntil)
    {
        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: userId,
            actionName: "LOGIN_BLOCKED_LOCKED",
            changedBy: userId,
            changeReason: "Login blocked because account is temporarily locked.",
            oldValues: null,
            newValues: new
            {
                attempted_email = normalizedEmail,
                user_id = userId,
                locked_until = lockedUntil
            });
    }

    private async Task LogLoginBlockedHardLockedAsync(
        Guid userId,
        string normalizedEmail,
        DateTime? hardLockedAt,
        string? hardLockReason)
    {
        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: userId,
            actionName: "LOGIN_BLOCKED_HARD_LOCKED",
            changedBy: userId,
            changeReason: "Login blocked because account is hard-locked.",
            oldValues: null,
            newValues: new
            {
                attempted_email = normalizedEmail,
                user_id = userId,
                hard_locked_at = hardLockedAt,
                hard_lock_reason = hardLockReason
            });
    }

    private async Task LogAccountSoftLockedAsync(
        Guid userId,
        int failedLoginCount,
        DateTime lockedUntil,
        int softLockCount,
        int softLockCountIn24Hours,
        int softLockCountIn7Days)
    {
        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: userId,
            actionName: "ACCOUNT_SOFT_LOCKED",
            changedBy: userId,
            changeReason: "Account soft-locked due to too many failed login attempts.",
            oldValues: null,
            newValues: new
            {
                user_id = userId,
                failed_login_count = failedLoginCount,
                locked_until = lockedUntil,
                soft_lock_count = softLockCount,
                soft_lock_count_in_24_hours = softLockCountIn24Hours,
                soft_lock_count_in_7_days = softLockCountIn7Days
            });
    }

    private async Task LogAccountHardLockedAsync(
        Guid userId,
        DateTime? hardLockedAt,
        string? hardLockReason,
        int softLockCountIn24Hours,
        int softLockCountIn7Days)
    {
        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: userId,
            actionName: "ACCOUNT_HARD_LOCKED",
            changedBy: userId,
            changeReason: "Account hard-locked due to repeated soft lockouts.",
            oldValues: null,
            newValues: new
            {
                user_id = userId,
                hard_locked_at = hardLockedAt,
                hard_lock_reason = hardLockReason,
                soft_lock_count_in_24_hours = softLockCountIn24Hours,
                soft_lock_count_in_7_days = softLockCountIn7Days
            });
    }

    private async Task LogLoginSuccessAsync(
        LoginUserDTO loginUser,
        List<string> roleCodes,
        bool rememberMe)
    {
        await _auditLogService.LogAsync(
            entityName: "auth",
            entityId: loginUser.Id,
            actionName: "LOGIN_SUCCESS",
            changedBy: loginUser.Id,
            changeReason: "User logged in successfully.",
            oldValues: null,
            newValues: new
            {
                user_id = loginUser.Id,
                email = loginUser.Email,
                role_codes = roleCodes,
                remember_me = rememberMe,
                must_change_password = false
            });
    }

    private void AddInvalidLoginError()
    {
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
    }
}
