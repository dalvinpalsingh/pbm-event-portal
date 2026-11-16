using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionYears;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionYears;

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
    public CompetitionYearEditInputDTO Input { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var competitionYear = await _dbContext.CompetitionYears
            .AsNoTracking()
            .FirstOrDefaultAsync(competitionYear => competitionYear.Id == id);

        if (competitionYear == null)
        {
            return NotFound();
        }

        Input = MapToInput(competitionYear);

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
            ModelState.AddModelError(string.Empty, "Invalid competition year request.");
        }

        NormalizeInput();

        await ValidateCompetitionYearAsync(id);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var competitionYear = await _dbContext.CompetitionYears
            .FirstOrDefaultAsync(competitionYear => competitionYear.Id == id);

        if (competitionYear == null)
        {
            return NotFound();
        }

        var oldSnapshot = CreateCompetitionYearSnapshot(competitionYear);

        var now = DateTime.UtcNow;

        competitionYear.YearNo = Input.YearNo!.Value;
        competitionYear.DisplayName = Input.DisplayName;
        competitionYear.RegistrationOpenAt = Input.RegistrationOpenAt;
        competitionYear.RegistrationCloseAt = Input.RegistrationCloseAt;
        competitionYear.IsActive = Input.IsActive;
        competitionYear.UpdatedAt = now;
        competitionYear.UpdatedBy = actorId.Value;

        var newSnapshot = CreateCompetitionYearSnapshot(competitionYear);

        await _auditLogService.LogAsync(
            entityName: "competition_years",
            entityId: competitionYear.Id,
            actionName: "UPDATE",
            changedBy: actorId.Value,
            changeReason: "System Admin updated competition year.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = "Competition year updated";
        ResultMessage = "The competition year has been updated successfully.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static CompetitionYearEditInputDTO MapToInput(CompetitionYear competitionYear)
    {
        return new CompetitionYearEditInputDTO
        {
            Id = competitionYear.Id,
            YearNo = competitionYear.YearNo,
            DisplayName = competitionYear.DisplayName,
            RegistrationOpenAt = competitionYear.RegistrationOpenAt,
            RegistrationCloseAt = competitionYear.RegistrationCloseAt,
            IsActive = competitionYear.IsActive
        };
    }

    private void NormalizeInput()
    {
        Input.DisplayName = string.IsNullOrWhiteSpace(Input.DisplayName)
            ? string.Empty
            : Input.DisplayName.Trim();
    }

    private async Task ValidateCompetitionYearAsync(Guid currentCompetitionYearId)
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
                .AnyAsync(competitionYear =>
                    competitionYear.Id != currentCompetitionYearId &&
                    competitionYear.YearNo == Input.YearNo.Value);

            if (yearExists)
            {
                ModelState.AddModelError(nameof(Input.YearNo), "This competition year already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Input.DisplayName))
        {
            var displayNameExists = await _dbContext.CompetitionYears
                .AsNoTracking()
                .AnyAsync(competitionYear =>
                    competitionYear.Id != currentCompetitionYearId &&
                    competitionYear.DisplayName.ToLower() == Input.DisplayName.ToLower());

            if (displayNameExists)
            {
                ModelState.AddModelError(nameof(Input.DisplayName), "This display name is already used by another competition year.");
            }
        }
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Competition year update failed";
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
