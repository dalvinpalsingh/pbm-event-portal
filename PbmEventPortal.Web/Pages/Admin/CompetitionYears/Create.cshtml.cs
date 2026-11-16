using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionYears;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionYears;

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public CompetitionYearCreateInputDTO Input { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedCompetitionYearId { get; private set; }

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

        await ValidateCompetitionYearAsync();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var now = DateTime.UtcNow;

        var competitionYear = new CompetitionYear
        {
            Id = Guid.NewGuid(),
            YearNo = Input.YearNo!.Value,
            DisplayName = Input.DisplayName,
            RegistrationOpenAt = Input.RegistrationOpenAt,
            RegistrationCloseAt = Input.RegistrationCloseAt,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.CompetitionYears.Add(competitionYear);

        await _auditLogService.LogAsync(
            entityName: "competition_years",
            entityId: competitionYear.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created competition year.",
            oldValues: null,
            newValues: CreateCompetitionYearSnapshot(competitionYear));

        CreatedCompetitionYearId = competitionYear.Id;
        ResultIsSuccess = true;
        ResultTitle = "Competition year created";
        ResultMessage = "The competition year has been created successfully.";

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
        Input.DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName)
            ? string.Empty
            : Input.DisplayName.Trim();
    }

    private async Task ValidateCompetitionYearAsync()
    {
        if (!Input.YearNo.HasValue)
        {
            ModelState.AddModelError(nameof(Input.YearNo), "Year is required.");
        }
        else if (Input.YearNo.Value < 2000 || Input.YearNo.Value > 2100)
        {
            ModelState.AddModelError(nameof(Input.YearNo), "Year must be between 2000 and 2100.");
        }

        if (string.IsNullOrWhiteSpace(Input.DisplayName))
        {
            ModelState.AddModelError(nameof(Input.DisplayName), "Display name is required.");
        }
        else if (Input.DisplayName.Length > 255)
        {
            ModelState.AddModelError(nameof(Input.DisplayName), "Display name must not be more than 255 characters.");
        }

        if (Input.RegistrationOpenAt.HasValue &&
            Input.RegistrationCloseAt.HasValue &&
            Input.RegistrationCloseAt.Value <= Input.RegistrationOpenAt.Value)
        {
            ModelState.AddModelError(
                nameof(Input.RegistrationCloseAt),
                "Registration close date/time must be after the open date/time.");
        }

        if (Input.YearNo.HasValue)
        {
            var yearExists = await _dbContext.CompetitionYears
                .AsNoTracking()
                .AnyAsync(competitionYear => competitionYear.YearNo == Input.YearNo.Value);

            if (yearExists)
            {
                ModelState.AddModelError(nameof(Input.YearNo), "This competition year already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Input.DisplayName))
        {
            var displayNameExists = await _dbContext.CompetitionYears
                .AsNoTracking()
                .AnyAsync(competitionYear => competitionYear.DisplayName.ToLower() == Input.DisplayName.ToLower());

            if (displayNameExists)
            {
                ModelState.AddModelError(nameof(Input.DisplayName), "This display name is already used by another competition year.");
            }
        }
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Competition year creation failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreateCompetitionYearSnapshot(CompetitionYear competitionYear)
    {
        return new
        {
            competitionYear.Id,
            competitionYear.YearNo,
            competitionYear.DisplayName,
            competitionYear.RegistrationOpenAt,
            competitionYear.RegistrationCloseAt,
            competitionYear.IsActive,
            competitionYear.CreatedAt,
            competitionYear.CreatedBy,
            competitionYear.UpdatedAt,
            competitionYear.UpdatedBy
        };
    }
}
