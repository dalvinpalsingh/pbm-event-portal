using System.Globalization;
using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Helpers.Common;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.Participants;

namespace PbmEventPortal.Web.Pages.Admin.Participants;

public class EditModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public EditModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public ParticipantEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];
    public List<SelectListItem> TeamOptions { get; private set; } = [];
    public List<SelectListItem> IdTypeOptions { get; private set; } = [];
    public List<SelectListItem> GenderOptions { get; private set; } = [];
    public List<SelectListItem> TahapOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var participant = await _dbContext.Participants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (participant == null) return NotFound();

        Input = new ParticipantEditInputDTO
        {
            Id = participant.Id,
            CompetitionYearId = participant.CompetitionYearId,
            TeamId = participant.TeamId,
            FullNameAsPerId = participant.FullNameAsPerId,
            FullNameGurmukhi = participant.FullNameGurmukhi,
            IdType = participant.IdType,
            NricNumber = participant.NricNumber,
            NricNumberDisplay = FormatNricForDisplay(participant.NricNumber),
            PassportNumber = participant.PassportNumber,
            PassportExpiryDate = participant.PassportExpiryDate,
            MobileNumber = participant.MobileNumber,
            MobileCountryCode = ProfileHelper.ExtractCountryCode(participant.MobileNumber),
            MobileLocalNumber = ProfileHelper.ExtractLocalNumber(participant.MobileNumber),
            Email = participant.Email,
            ParentGuardianName = participant.ParentGuardianName,
            ParentGuardianMobile = participant.ParentGuardianMobile,
            ParentGuardianMobileCountryCode = ProfileHelper.ExtractCountryCode(participant.ParentGuardianMobile),
            ParentGuardianMobileLocalNumber = ProfileHelper.ExtractLocalNumber(participant.ParentGuardianMobile),
            Gender = participant.Gender.ToString(),
            DateOfBirth = participant.DateOfBirth,
            TahapCode = participant.TahapCode,
            IsActive = participant.IsActive
        };

        await LoadLookupOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();
        if (Input.Id != id) ModelState.AddModelError(string.Empty, "Invalid participant request.");

        NormalizeInput();
        await RestoreInactiveIdValuesFromExistingParticipantAsync(id);
        ValidateIdFields();
        ValidateMobileFields();
        ValidateDateOfBirth();
        ValidateOptionalEmail();
        await ValidateInputAsync(id);
        await LoadLookupOptionsAsync();

        if (!ModelState.IsValid)
        {
            ResultIsSuccess = false;
            ResultTitle = "Participant update failed";
            ResultMessage = "Please review the following and try again.";
            return Page();
        }

        var participant = await _dbContext.Participants.FirstOrDefaultAsync(x => x.Id == id);
        if (participant == null) return NotFound();

        var oldSnapshot = Snapshot(participant);
        participant.CompetitionYearId = Input.CompetitionYearId!.Value;
        participant.TeamId = Input.TeamId!.Value;
        participant.FullNameAsPerId = Input.FullNameAsPerId;
        participant.FullNameGurmukhi = Input.FullNameGurmukhi;
        participant.IdType = Input.IdType;
        participant.NricNumber = Input.NricNumber;
        participant.PassportNumber = Input.PassportNumber;
        participant.PassportExpiryDate = Input.PassportExpiryDate;
        participant.MobileNumber = Input.MobileNumber;
        participant.Email = Input.Email;
        participant.ParentGuardianName = Input.ParentGuardianName;
        participant.ParentGuardianMobile = Input.ParentGuardianMobile;
        participant.Gender = Input.Gender[0];
        participant.DateOfBirth = Input.DateOfBirth!.Value;
        participant.TahapCode = Input.TahapCode;
        participant.IsActive = Input.IsActive;
        participant.UpdatedAt = DateTime.UtcNow;
        participant.UpdatedBy = actorId.Value;

        await _auditLogService.LogAsync("participants", participant.Id, "UPDATE", actorId.Value,
            "System Admin updated participant.", oldSnapshot, Snapshot(participant));

        ResultIsSuccess = true;
        ResultTitle = participant.IsActive ? "Participant updated" : "Participant deactivated";
        ResultMessage = "The participant has been updated successfully.";
        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private void NormalizeInput()
    {
        Input.FullNameAsPerId = string.IsNullOrWhiteSpace(Input.FullNameAsPerId) ? string.Empty : Input.FullNameAsPerId.Trim();
        Input.FullNameGurmukhi = string.IsNullOrWhiteSpace(Input.FullNameGurmukhi) ? string.Empty : Input.FullNameGurmukhi.Trim();
        Input.IdType = string.IsNullOrWhiteSpace(Input.IdType) ? string.Empty : Input.IdType.Trim().ToUpperInvariant();
        Input.NricNumberDisplay = string.IsNullOrWhiteSpace(Input.NricNumberDisplay) ? null : Input.NricNumberDisplay.Trim();
        Input.NricNumber = Input.NricNumberDisplay == null ? null : new string(Input.NricNumberDisplay.Where(char.IsDigit).ToArray());
        Input.PassportNumber = string.IsNullOrWhiteSpace(Input.PassportNumber) ? null : Input.PassportNumber.Trim().ToUpperInvariant();
        Input.MobileCountryCode = string.IsNullOrWhiteSpace(Input.MobileCountryCode) ? "+60" : Input.MobileCountryCode.Trim();
        Input.MobileLocalNumber = string.IsNullOrWhiteSpace(Input.MobileLocalNumber) ? null : new string(Input.MobileLocalNumber.Where(char.IsDigit).ToArray());
        Input.MobileNumber = string.IsNullOrWhiteSpace(Input.MobileLocalNumber) ? null : $"{Input.MobileCountryCode}{Input.MobileLocalNumber}".Replace(" ", "");
        Input.Email = string.IsNullOrWhiteSpace(Input.Email) ? null : Input.Email.Trim().ToLowerInvariant();
        Input.ParentGuardianName = string.IsNullOrWhiteSpace(Input.ParentGuardianName) ? string.Empty : Input.ParentGuardianName.Trim();
        Input.ParentGuardianMobileCountryCode = string.IsNullOrWhiteSpace(Input.ParentGuardianMobileCountryCode) ? "+60" : Input.ParentGuardianMobileCountryCode.Trim();
        Input.ParentGuardianMobileLocalNumber = string.IsNullOrWhiteSpace(Input.ParentGuardianMobileLocalNumber) ? null : new string(Input.ParentGuardianMobileLocalNumber.Where(char.IsDigit).ToArray());
        Input.ParentGuardianMobile = $"{Input.ParentGuardianMobileCountryCode}{Input.ParentGuardianMobileLocalNumber}".Replace(" ", "");
        Input.Gender = string.IsNullOrWhiteSpace(Input.Gender) ? string.Empty : Input.Gender.Trim().ToUpperInvariant();
        Input.TahapCode = string.IsNullOrWhiteSpace(Input.TahapCode) ? string.Empty : Input.TahapCode.Trim().ToUpperInvariant();
    }

    private async Task RestoreInactiveIdValuesFromExistingParticipantAsync(Guid participantId)
    {
        var existing = await _dbContext.Participants
            .AsNoTracking()
            .Where(x => x.Id == participantId)
            .Select(x => new
            {
                x.NricNumber,
                x.PassportNumber,
                x.PassportExpiryDate
            })
            .FirstOrDefaultAsync();

        if (existing == null) return;

        if (Input.IdType == ProfileEnums.IdTypeNric)
        {
            Input.PassportNumber ??= existing.PassportNumber;
            Input.PassportExpiryDate ??= existing.PassportExpiryDate;
        }
        else if (Input.IdType == ProfileEnums.IdTypePassport)
        {
            Input.NricNumber ??= existing.NricNumber;
            Input.NricNumberDisplay ??= FormatNricForDisplay(existing.NricNumber);
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
            if (string.IsNullOrWhiteSpace(Input.NricNumber)) ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number is required.");
            else if (Input.NricNumber.Length != 12 || !Input.NricNumber.All(char.IsDigit)) ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC number must be exactly 12 digits.");
            ApplyNricDerivedFields();
        }
        else if (Input.IdType == ProfileEnums.IdTypePassport)
        {
            if (string.IsNullOrWhiteSpace(Input.PassportNumber)) ModelState.AddModelError(nameof(Input.PassportNumber), "Passport number is required.");
            if (!Input.PassportExpiryDate.HasValue) ModelState.AddModelError(nameof(Input.PassportExpiryDate), "Passport expiry date is required.");
            if (!ProfileEnums.Genders.Contains(Input.Gender)) ModelState.AddModelError(nameof(Input.Gender), "Gender must be Male or Female.");
        }

        if (!string.IsNullOrWhiteSpace(Input.NricNumber) && (Input.NricNumber.Length != 12 || !Input.NricNumber.All(char.IsDigit)))
        {
            ModelState.AddModelError(nameof(Input.NricNumberDisplay), "Stored NRIC number must be exactly 12 digits.");
        }

        if (!string.IsNullOrWhiteSpace(Input.PassportNumber) && Input.PassportNumber.Length > 20)
        {
            ModelState.AddModelError(nameof(Input.PassportNumber), "Passport number must not be more than 20 characters.");
        }
    }

    private void ApplyNricDerivedFields()
    {
        if (Input.NricNumber == null || Input.NricNumber.Length != 12 || !Input.NricNumber.All(char.IsDigit)) return;
        Input.Gender = int.Parse(Input.NricNumber[^1].ToString(), CultureInfo.InvariantCulture) % 2 == 1 ? ProfileEnums.GenderMale : ProfileEnums.GenderFemale;
        var yy = int.Parse(Input.NricNumber[..2], CultureInfo.InvariantCulture);
        var mm = int.Parse(Input.NricNumber.Substring(2, 2), CultureInfo.InvariantCulture);
        var dd = int.Parse(Input.NricNumber.Substring(4, 2), CultureInfo.InvariantCulture);
        var yyyy = yy <= DateTime.UtcNow.Year % 100 ? 2000 + yy : 1900 + yy;
        try { Input.DateOfBirth = new DateOnly(yyyy, mm, dd); }
        catch { ModelState.AddModelError(nameof(Input.NricNumberDisplay), "NRIC birth date is invalid."); }
    }

    private void ValidateMobileFields()
    {
        if (!string.IsNullOrWhiteSpace(Input.MobileLocalNumber) && Input.MobileLocalNumber.Length < 6) ModelState.AddModelError(nameof(Input.MobileLocalNumber), "Mobile number is too short.");
        if (string.IsNullOrWhiteSpace(Input.ParentGuardianMobileLocalNumber)) ModelState.AddModelError(nameof(Input.ParentGuardianMobileLocalNumber), "Parent / guardian mobile is required.");
        else if (Input.ParentGuardianMobileLocalNumber.Length < 6) ModelState.AddModelError(nameof(Input.ParentGuardianMobileLocalNumber), "Parent / guardian mobile is too short.");
    }

    private void ValidateDateOfBirth()
    {
        if (Input.IdType == ProfileEnums.IdTypeNric) return;
        if (!Input.DateOfBirth.HasValue) ModelState.AddModelError(nameof(Input.DateOfBirth), "Date of birth is required.");
    }

    private void ValidateOptionalEmail()
    {
        if (string.IsNullOrWhiteSpace(Input.Email)) return;

        try
        {
            var address = new MailAddress(Input.Email);
            if (!string.Equals(address.Address, Input.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(Input.Email), "Please enter a valid email address.");
            }
        }
        catch
        {
            ModelState.AddModelError(nameof(Input.Email), "Please enter a valid email address.");
        }
    }

    private async Task ValidateInputAsync(Guid participantId)
    {
        if (!Input.CompetitionYearId.HasValue) ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        else if (!await _dbContext.CompetitionYears.AsNoTracking().AnyAsync(x => x.Id == Input.CompetitionYearId.Value && x.IsActive)) ModelState.AddModelError(nameof(Input.CompetitionYearId), "Selected competition year is not valid or inactive.");
        if (!Input.TeamId.HasValue) ModelState.AddModelError(nameof(Input.TeamId), "Team is required.");
        else if (!await _dbContext.Teams.AsNoTracking().AnyAsync(x => x.Id == Input.TeamId.Value && x.IsActive)) ModelState.AddModelError(nameof(Input.TeamId), "Selected team is not valid or inactive.");
        if (string.IsNullOrWhiteSpace(Input.FullNameAsPerId)) ModelState.AddModelError(nameof(Input.FullNameAsPerId), "Full name as per ID is required.");
        else if (Input.FullNameAsPerId.Length > 200) ModelState.AddModelError(nameof(Input.FullNameAsPerId), "Full name as per ID must not be more than 200 characters.");
        if (string.IsNullOrWhiteSpace(Input.FullNameGurmukhi)) ModelState.AddModelError(nameof(Input.FullNameGurmukhi), "Full name in Gurmukhi is required.");
        else if (Input.FullNameGurmukhi.Length > 200) ModelState.AddModelError(nameof(Input.FullNameGurmukhi), "Full name in Gurmukhi must not be more than 200 characters.");
        if (!TahapEnums.TahapCodes.Contains(Input.TahapCode)) ModelState.AddModelError(nameof(Input.TahapCode), "Selected Tahap is not valid.");
        if (!string.IsNullOrWhiteSpace(Input.NricNumber) && Input.CompetitionYearId.HasValue && await _dbContext.Participants.AsNoTracking().AnyAsync(x => x.Id != participantId && x.CompetitionYearId == Input.CompetitionYearId.Value && x.NricNumber == Input.NricNumber)) ModelState.AddModelError(nameof(Input.NricNumberDisplay), "A participant with this NRIC already exists for the selected competition year.");
        if (!string.IsNullOrWhiteSpace(Input.PassportNumber) && Input.CompetitionYearId.HasValue && await _dbContext.Participants.AsNoTracking().AnyAsync(x => x.Id != participantId && x.CompetitionYearId == Input.CompetitionYearId.Value && x.PassportNumber == Input.PassportNumber)) ModelState.AddModelError(nameof(Input.PassportNumber), "A participant with this passport number already exists for the selected competition year.");
        if (Input.MobileNumber?.Length > 30) ModelState.AddModelError(nameof(Input.MobileLocalNumber), "Mobile number must not be more than 30 characters.");
        if (Input.Email?.Length > 256) ModelState.AddModelError(nameof(Input.Email), "Email must not be more than 256 characters.");
        if (string.IsNullOrWhiteSpace(Input.ParentGuardianName)) ModelState.AddModelError(nameof(Input.ParentGuardianName), "Parent / guardian name is required.");
        else if (Input.ParentGuardianName.Length > 200) ModelState.AddModelError(nameof(Input.ParentGuardianName), "Parent / guardian name must not be more than 200 characters.");
        if (Input.ParentGuardianMobile?.Length > 30) ModelState.AddModelError(nameof(Input.ParentGuardianMobileLocalNumber), "Parent / guardian mobile must not be more than 30 characters.");
    }

    private async Task LoadLookupOptionsAsync()
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears.AsNoTracking().Where(x => x.IsActive).OrderByDescending(x => x.YearNo).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.DisplayName }).ToListAsync();
        CompetitionYearOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select competition year..." });
        TeamOptions = await _dbContext.Teams.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Code + " - " + x.NameEnglish }).ToListAsync();
        TeamOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select team..." });
        IdTypeOptions = [new SelectListItem { Value = ProfileEnums.IdTypeNric, Text = "NRIC" }, new SelectListItem { Value = ProfileEnums.IdTypePassport, Text = "Passport" }];
        GenderOptions = [new SelectListItem { Value = ProfileEnums.GenderMale, Text = "Male" }, new SelectListItem { Value = ProfileEnums.GenderFemale, Text = "Female" }];
        TahapOptions = TahapEnums.TahapCodes.Select(x => new SelectListItem { Value = x, Text = x.Replace("_", " ") }).ToList();
    }

    private static string? FormatNricForDisplay(string? nric) => string.IsNullOrWhiteSpace(nric) || nric.Length != 12 ? nric : $"{nric[..6]}-{nric.Substring(6, 2)}-{nric[8..]}";

    private static object Snapshot(Participant participant) => new
    {
        participant.Id, participant.CompetitionYearId, participant.TeamId, participant.FullNameAsPerId, participant.FullNameGurmukhi,
        participant.IdType, participant.NricNumber, participant.PassportNumber, participant.PassportExpiryDate, participant.MobileNumber,
        participant.Email, participant.ParentGuardianName, participant.ParentGuardianMobile, participant.Gender, participant.DateOfBirth,
        participant.TahapCode, participant.IsActive, participant.CreatedAt, participant.CreatedBy, participant.UpdatedAt, participant.UpdatedBy
    };
}
