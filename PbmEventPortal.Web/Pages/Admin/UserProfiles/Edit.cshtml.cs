using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

namespace PbmEventPortal.Web.Pages.Admin.UserProfiles;

public class EditModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public EditModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public UserProfileEditInputDTO Input { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var userProfile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);

        if (userProfile == null)
        {
            return NotFound();
        }

        Input = MapToInput(userProfile);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        if (Input.Id != id)
        {
            ModelState.AddModelError(string.Empty, "Invalid user profile request.");
        }

        NormalizeInput();

        ValidateIdFields();
        ValidateMobileFields();
        ValidateDateOfBirth();
        await ValidateUniqueFieldsAsync(id);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var userProfile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(user => user.Id == id);

        if (userProfile == null)
        {
            return NotFound();
        }

        var oldSnapshot = CreateUserProfileSnapshot(userProfile);

        var now = DateTime.UtcNow;

        userProfile.FullNameAsPerId = Input.FullNameAsPerId.Trim();
        userProfile.FullNameGurmukhi = Input.FullNameGurmukhi?.Trim() ?? string.Empty;
        userProfile.IdType = Input.IdType;
        userProfile.NricNumber = Input.IdType == ProfileEnums.IdTypeNric ? Input.NricNumber : null;
        userProfile.PassportNumber = Input.IdType == ProfileEnums.IdTypePassport ? Input.PassportNumber : null;
        userProfile.PassportExpiryDate = Input.IdType == ProfileEnums.IdTypePassport ? Input.PassportExpiryDate : null;
        userProfile.MobileNumber = Input.MobileNumber;
        userProfile.Email = Input.Email.Trim();
        userProfile.Gender = Input.Gender[0];
        userProfile.DateOfBirth = Input.DateOfBirth!.Value;
        userProfile.IsActive = Input.IsActive;
        userProfile.UpdatedAt = now;
        userProfile.UpdatedBy = actorId.Value;

        var newSnapshot = CreateUserProfileSnapshot(userProfile);

        await _auditLogService.LogAsync(
            entityName: "user_profiles",
            entityId: userProfile.Id,
            actionName: "UPDATE",
            changedBy: actorId.Value,
            changeReason: "System Admin updated user profile.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = "User updated";
        ResultMessage = "The user profile has been updated successfully.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static UserProfileEditInputDTO MapToInput(UserProfile userProfile)
    {
        var mobileParts = SplitMobileNumber(userProfile.MobileNumber);

        return new UserProfileEditInputDTO
        {
            Id = userProfile.Id,
            FullNameAsPerId = userProfile.FullNameAsPerId,
            FullNameGurmukhi = string.IsNullOrWhiteSpace(userProfile.FullNameGurmukhi)
                ? null
                : userProfile.FullNameGurmukhi,
            IdType = userProfile.IdType,
            NricNumber = userProfile.NricNumber,
            NricNumberDisplay = FormatNric(userProfile.NricNumber),
            PassportNumber = userProfile.PassportNumber,
            PassportExpiryDate = userProfile.PassportExpiryDate,
            MobileNumber = userProfile.MobileNumber,
            MobileCountryCode = mobileParts.CountryCode,
            MobileLocalNumber = mobileParts.LocalNumber,
            Email = userProfile.Email,
            Gender = userProfile.Gender.ToString(),
            DateOfBirth = userProfile.DateOfBirth,
            IsActive = userProfile.IsActive
        };
    }

    private static (string CountryCode, string LocalNumber) SplitMobileNumber(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return ("+60", string.Empty);
        }

        var cleanedNumber = mobileNumber.Trim().Replace(" ", "").Replace("-", "");

        var matchingCountryCode = ProfileEnums.SupportedCountryCodes
            .OrderByDescending(code => code.Length)
            .FirstOrDefault(cleanedNumber.StartsWith);

        if (matchingCountryCode == null)
        {
            return ("+60", cleanedNumber.TrimStart('+'));
        }

        return (
            matchingCountryCode,
            cleanedNumber[matchingCountryCode.Length..]);
    }

    private static string? FormatNric(string? nricNumber)
    {
        if (string.IsNullOrWhiteSpace(nricNumber))
        {
            return null;
        }

        var digits = new string(nricNumber.Where(char.IsDigit).ToArray());

        if (digits.Length != 12)
        {
            return nricNumber;
        }

        return $"{digits[..6]}-{digits.Substring(6, 2)}-{digits[8..]}";
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

        if (Input.IdType == ProfileEnums.IdTypeNric)
        {
            Input.PassportNumber = null;
            Input.PassportExpiryDate = null;
        }

        if (Input.IdType == ProfileEnums.IdTypePassport)
        {
            Input.NricNumber = null;
            Input.NricNumberDisplay = null;
        }
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

    private async Task ValidateUniqueFieldsAsync(Guid currentUserId)
    {
        var normalizedEmail = Input.Email.ToLowerInvariant();

        var emailExists = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id != currentUserId &&
                user.Email.ToLower() == normalizedEmail);

        if (emailExists)
        {
            ModelState.AddModelError(nameof(Input.Email), "Email address is already used by another user.");
        }

        if (Input.IdType == ProfileEnums.IdTypeNric && !string.IsNullOrWhiteSpace(Input.NricNumber))
        {
            var nricExists = await _dbContext.UserProfiles
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id != currentUserId &&
                    user.NricNumber == Input.NricNumber);

            if (nricExists)
            {
                ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number is already used by another user.");
            }
        }

        if (Input.IdType == ProfileEnums.IdTypePassport && !string.IsNullOrWhiteSpace(Input.PassportNumber))
        {
            var passportExists = await _dbContext.UserProfiles
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id != currentUserId &&
                    user.PassportNumber == Input.PassportNumber);

            if (passportExists)
            {
                ModelState.AddModelError(nameof(Input.PassportNumber), "Passport number is already used by another user.");
            }
        }
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "User update failed";
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
}
