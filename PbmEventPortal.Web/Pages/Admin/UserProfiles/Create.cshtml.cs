using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Application.Services.Security;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

namespace PbmEventPortal.Web.Pages.Admin.UserProfiles;

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPasswordPolicyService _passwordPolicyService;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(
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
    public UserProfileCreateInputDTO Input { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedUserId { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        NormalizeInput();

        ValidateIdFields();
        ValidateMobileFields();
        ValidateDateOfBirth();
        await ValidateUniqueFieldsAsync();

        if (Input.CreateLoginCredential)
        {
            await ValidateCredentialFieldsAsync();
        }

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var now = DateTime.UtcNow;

        var userProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            FullNameAsPerId = Input.FullNameAsPerId.Trim(),
            FullNameGurmukhi = Input.FullNameGurmukhi?.Trim() ?? string.Empty,
            IdType = Input.IdType,
            NricNumber = Input.IdType == ProfileEnums.IdTypeNric ? Input.NricNumber : null,
            PassportNumber = Input.IdType == ProfileEnums.IdTypePassport ? Input.PassportNumber : null,
            PassportExpiryDate = Input.IdType == ProfileEnums.IdTypePassport ? Input.PassportExpiryDate : null,
            MobileNumber = Input.MobileNumber,
            Email = Input.Email.Trim(),
            Gender = Input.Gender[0],
            DateOfBirth = Input.DateOfBirth!.Value,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.UserProfiles.Add(userProfile);

        await _auditLogService.LogAsync(
            entityName: "user_profiles",
            entityId: userProfile.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created user profile.",
            oldValues: null,
            newValues: CreateUserProfileSnapshot(userProfile));

        if (Input.CreateLoginCredential)
        {
            var passwordHash = _passwordHashService.HashPassword(Input.TemporaryPassword!);

            var credential = new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = userProfile.Id,
                PasswordHash = passwordHash,
                MustChangePassword = true,
                FailedLoginCount = 0,
                LockedUntil = null,
                LastLoginAt = null,
                LastPasswordChangedAt = now,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = actorId.Value,
                UpdatedAt = null,
                UpdatedBy = null,
                SoftLockCount = 0,
                LastSoftLockedAt = null,
                HardLockedAt = null,
                HardLockedBy = null,
                HardLockReason = null,
                RequiresAdminUnlock = false
            };

            var passwordHistory = new UserPasswordHistory
            {
                Id = Guid.NewGuid(),
                UserId = userProfile.Id,
                PasswordHash = passwordHash,
                ChangedAt = now,
                ChangedBy = actorId.Value,
                ChangeReason = "ADMIN_CREATE_USER",
                CreatedAt = now,
                CreatedBy = actorId.Value,
                UpdatedAt = null,
                UpdatedBy = null
            };

            _dbContext.UserCredentials.Add(credential);
            _dbContext.UserPasswordHistories.Add(passwordHistory);

            await _auditLogService.LogAsync(
                entityName: "user_credentials",
                entityId: credential.Id,
                actionName: "CREATE",
                changedBy: actorId.Value,
                changeReason: "System Admin created login credential for user.",
                oldValues: null,
                newValues: CreateCredentialSnapshot(credential));

            await _auditLogService.LogAsync(
                entityName: "user_password_history",
                entityId: passwordHistory.Id,
                actionName: "CREATE",
                changedBy: actorId.Value,
                changeReason: "Initial password history record created for admin-created user.",
                oldValues: null,
                newValues: CreatePasswordHistorySnapshot(passwordHistory));
        }

        CreatedUserId = userProfile.Id;
        ResultIsSuccess = true;
        ResultTitle = "User created";
        ResultMessage = "The user profile has been created successfully.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private void NormalizeInput()
    {
        Input.FullNameAsPerId = Input.FullNameAsPerId.Trim();
        Input.FullNameGurmukhi = string.IsNullOrWhiteSpace(Input.FullNameGurmukhi)
            ? null
            : Input.FullNameGurmukhi.Trim();

        Input.IdType = Input.IdType.Trim().ToUpperInvariant();

        Input.NricNumberDisplay = string.IsNullOrWhiteSpace(Input.NricNumberDisplay)
            ? null
            : Input.NricNumberDisplay.Trim();

        Input.NricNumber = Input.NricNumberDisplay == null
            ? null
            : new string(Input.NricNumberDisplay.Where(char.IsDigit).ToArray());

        Input.PassportNumber = string.IsNullOrWhiteSpace(Input.PassportNumber)
            ? null
            : Input.PassportNumber.Trim();

        Input.MobileCountryCode = string.IsNullOrWhiteSpace(Input.MobileCountryCode)
            ? "+60"
            : Input.MobileCountryCode.Trim();

        Input.MobileLocalNumber = string.IsNullOrWhiteSpace(Input.MobileLocalNumber)
            ? null
            : new string(Input.MobileLocalNumber.Where(char.IsDigit).ToArray());

        Input.MobileNumber = $"{Input.MobileCountryCode}{Input.MobileLocalNumber}".Replace(" ", "");

        Input.Email = Input.Email.Trim();
        Input.Gender = Input.Gender.Trim().ToUpperInvariant();
    }

    private void ValidateIdFields()
    {
        if (!ProfileEnums.IdTypes.Contains(Input.IdType))
        {
            ModelState.AddModelError(nameof(Input.IdType), "ID type must be NRIC or Passport.");
            return;
        }

        if (Input.IdType == ProfileEnums.IdTypeNric)
        {
            if (string.IsNullOrWhiteSpace(Input.NricNumber))
            {
                ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number is required.");
            }
            else if (Input.NricNumber.Length != 12 || !Input.NricNumber.All(char.IsDigit))
            {
                ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number must be exactly 12 digits.");
            }

            if (!string.IsNullOrWhiteSpace(Input.PassportNumber))
            {
                ModelState.AddModelError(nameof(Input.PassportNumber), "Passport number must be empty when ID type is NRIC.");
            }

            if (Input.PassportExpiryDate.HasValue)
            {
                ModelState.AddModelError(nameof(Input.PassportExpiryDate), "Passport expiry date must be empty when ID type is NRIC.");
            }

            ApplyNricDerivedFields();
        }

        if (Input.IdType == ProfileEnums.IdTypePassport)
        {
            if (string.IsNullOrWhiteSpace(Input.PassportNumber))
            {
                ModelState.AddModelError(nameof(Input.PassportNumber), "Passport number is required.");
            }

            if (!Input.PassportExpiryDate.HasValue)
            {
                ModelState.AddModelError(nameof(Input.PassportExpiryDate), "Passport expiry date is required.");
            }

            if (!string.IsNullOrWhiteSpace(Input.NricNumber))
            {
                ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number must be empty when ID type is Passport.");
            }

            if (!ProfileEnums.Genders.Contains(Input.Gender))
            {
                ModelState.AddModelError(nameof(Input.Gender), "Gender must be Male or Female.");
            }
        }
    }

    private void ApplyNricDerivedFields()
    {
        if (Input.NricNumber == null || Input.NricNumber.Length != 12 || !Input.NricNumber.All(char.IsDigit))
        {
            return;
        }

        Input.Gender = int.Parse(Input.NricNumber[^1].ToString(), CultureInfo.InvariantCulture) % 2 == 1
            ? ProfileEnums.GenderMale
            : ProfileEnums.GenderFemale;

        var yy = int.Parse(Input.NricNumber[..2], CultureInfo.InvariantCulture);
        var mm = int.Parse(Input.NricNumber.Substring(2, 2), CultureInfo.InvariantCulture);
        var dd = int.Parse(Input.NricNumber.Substring(4, 2), CultureInfo.InvariantCulture);

        var currentYearTwoDigits = DateTime.UtcNow.Year % 100;
        var yyyy = yy <= currentYearTwoDigits
            ? 2000 + yy
            : 1900 + yy;

        try
        {
            Input.DateOfBirth = new DateOnly(yyyy, mm, dd);
        }
        catch
        {
            ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC birth date is invalid.");
        }
    }

    private void ValidateMobileFields()
    {
        if (string.IsNullOrWhiteSpace(Input.MobileCountryCode))
        {
            ModelState.AddModelError(nameof(Input.MobileCountryCode), "Country code is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.MobileLocalNumber))
        {
            ModelState.AddModelError(nameof(Input.MobileLocalNumber), "Mobile number is required.");
        }

        if (!string.IsNullOrWhiteSpace(Input.MobileLocalNumber) &&
            Input.MobileLocalNumber.Length < 6)
        {
            ModelState.AddModelError(nameof(Input.MobileLocalNumber), "Mobile number is too short.");
        }
    }

    private void ValidateDateOfBirth()
    {
        if (Input.IdType == ProfileEnums.IdTypeNric)
        {
            return;
        }

        if (!Input.DateOfBirth.HasValue)
        {
            ModelState.AddModelError(nameof(Input.DateOfBirth), "Date of birth is required.");
        }
    }

    private async Task ValidateUniqueFieldsAsync()
    {
        var normalizedEmail = Input.Email.ToLowerInvariant();

        var emailExists = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(user => user.Email.ToLower() == normalizedEmail);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(Input.Email), "Email address is already used by another user.");
        }

        if (Input.IdType == ProfileEnums.IdTypeNric && !string.IsNullOrWhiteSpace(Input.NricNumber))
        {
            var nricExists = await _dbContext.UserProfiles
                .AsNoTracking()
                .AnyAsync(user => user.NricNumber == Input.NricNumber);

            if (nricExists)
            {
                ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number is already used by another user.");
            }
        }

        if (Input.IdType == ProfileEnums.IdTypePassport && !string.IsNullOrWhiteSpace(Input.PassportNumber))
        {
            var passportExists = await _dbContext.UserProfiles
                .AsNoTracking()
                .AnyAsync(user => user.PassportNumber == Input.PassportNumber);

            if (passportExists)
            {
                ModelState.AddModelError(nameof(Input.PassportNumber), "Passport number is already used by another user.");
            }
        }
    }

    private async Task ValidateCredentialFieldsAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.TemporaryPassword))
        {
            ModelState.AddModelError(nameof(Input.TemporaryPassword), "Temporary password is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Input.ConfirmTemporaryPassword))
        {
            ModelState.AddModelError(nameof(Input.ConfirmTemporaryPassword), "Confirm temporary password is required.");
            return;
        }

        if (Input.TemporaryPassword != Input.ConfirmTemporaryPassword)
        {
            ModelState.AddModelError(nameof(Input.ConfirmTemporaryPassword), "Temporary password and confirm temporary password do not match.");
            return;
        }

        var policyResult = await _passwordPolicyService.ValidateAsync(
            new PasswordPolicyValidationRequest
            {
                CurrentPassword = null,
                NewPassword = Input.TemporaryPassword,
                Email = Input.Email,
                MobileNumber = Input.MobileNumber,
                FullNameAsPerId = Input.FullNameAsPerId
            });

        if (!policyResult.IsValid)
        {
            foreach (var error in policyResult.Errors)
            {
                ModelState.AddModelError(nameof(Input.TemporaryPassword), error);
            }
        }
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "User creation failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreateUserProfileSnapshot(UserProfile userProfile)
    {
        return new
        {
            userProfile.Id,
            userProfile.FullNameAsPerId,
            userProfile.FullNameGurmukhi,
            userProfile.IdType,
            userProfile.NricNumber,
            userProfile.PassportNumber,
            userProfile.PassportExpiryDate,
            userProfile.MobileNumber,
            userProfile.Email,
            userProfile.Gender,
            userProfile.DateOfBirth,
            userProfile.IsActive,
            userProfile.CreatedAt,
            userProfile.CreatedBy,
            userProfile.UpdatedAt,
            userProfile.UpdatedBy
        };
    }

    private static object CreateCredentialSnapshot(UserCredential credential)
    {
        return new
        {
            credential.Id,
            credential.UserId,
            password_hash_stored = true,
            password_hash_in_audit = false,
            temporary_password_value_in_audit = false,
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
    }

    private static object CreatePasswordHistorySnapshot(UserPasswordHistory passwordHistory)
    {
        return new
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
        };
    }
}
