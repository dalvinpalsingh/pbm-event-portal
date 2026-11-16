using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.Competitions;

namespace PbmEventPortal.Web.Pages.Admin.Competitions;

public class EditModel : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> CompetitionTypeOptionsSource =
    [
        new SelectListItem { Value = CompetitionEnums.CompetitionTypeIndividual, Text = "Individual" },
        new SelectListItem { Value = CompetitionEnums.CompetitionTypeTeam, Text = "Team" }
    ];

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
    public CompetitionEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];

    public List<SelectListItem> CompetitionTypeOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var competition = await _dbContext.Competitions
            .AsNoTracking()
            .FirstOrDefaultAsync(competition => competition.Id == id);

        if (competition == null)
        {
            return NotFound();
        }

        Input = MapToInput(competition);

        await LoadLookupOptionsAsync();

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
            ModelState.AddModelError(string.Empty, "Invalid competition request.");
        }

        NormalizeInput();

        await ValidateCompetitionAsync(id);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var competition = await _dbContext.Competitions
            .FirstOrDefaultAsync(competition => competition.Id == id);

        if (competition == null)
        {
            return NotFound();
        }

        var oldSnapshot = CreateCompetitionSnapshot(competition);

        var now = DateTime.UtcNow;

        competition.CompetitionYearId = Input.CompetitionYearId!.Value;
        competition.Code = Input.Code;
        competition.NameEnglish = Input.NameEnglish;
        competition.NameGurmukhi = Input.NameGurmukhi;
        competition.CompetitionType = Input.CompetitionType;
        competition.CriteriaCount = Input.CriteriaCount!.Value;
        competition.MinJudges = Input.MinJudges!.Value;
        competition.MaxJudges = Input.MaxJudges!.Value;
        competition.Notes = Input.Notes;
        competition.IsActive = Input.IsActive;
        competition.UpdatedAt = now;
        competition.UpdatedBy = actorId.Value;

        var newSnapshot = CreateCompetitionSnapshot(competition);

        await _auditLogService.LogAsync(
            entityName: "competitions",
            entityId: competition.Id,
            actionName: "UPDATE",
            changedBy: actorId.Value,
            changeReason: "System Admin updated competition.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = "Competition updated";
        ResultMessage = "The competition has been updated successfully.";

        await LoadLookupOptionsAsync();

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static CompetitionEditInputDTO MapToInput(Competition competition)
    {
        return new CompetitionEditInputDTO
        {
            Id = competition.Id,
            CompetitionYearId = competition.CompetitionYearId,
            Code = competition.Code,
            NameEnglish = competition.NameEnglish,
            NameGurmukhi = competition.NameGurmukhi,
            CompetitionType = competition.CompetitionType,
            CriteriaCount = competition.CriteriaCount,
            MinJudges = competition.MinJudges,
            MaxJudges = competition.MaxJudges,
            Notes = competition.Notes,
            IsActive = competition.IsActive
        };
    }

    private void NormalizeInput()
    {
        Input.Code = string.IsNullOrWhiteSpace(Input.Code)
            ? string.Empty
            : Input.Code.Trim().ToUpperInvariant().Replace(" ", "_");

        Input.NameEnglish = string.IsNullOrWhiteSpace(Input.NameEnglish)
            ? string.Empty
            : Input.NameEnglish.Trim();

        Input.NameGurmukhi = string.IsNullOrWhiteSpace(Input.NameGurmukhi)
            ? null
            : Input.NameGurmukhi.Trim();

        Input.CompetitionType = string.IsNullOrWhiteSpace(Input.CompetitionType)
            ? string.Empty
            : Input.CompetitionType.Trim().ToUpperInvariant();

        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes)
            ? null
            : Input.Notes.Trim();
    }

    private async Task ValidateCompetitionAsync(Guid currentCompetitionId)
    {
        if (!Input.CompetitionYearId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        }
        else
        {
            var competitionYearExists = await _dbContext.CompetitionYears
                .AsNoTracking()
                .AnyAsync(competitionYear =>
                    competitionYear.Id == Input.CompetitionYearId.Value &&
                    competitionYear.IsActive);

            if (!competitionYearExists)
            {
                ModelState.AddModelError(nameof(Input.CompetitionYearId), "Selected competition year is not valid or inactive.");
            }
        }

        if (string.IsNullOrWhiteSpace(Input.Code))
        {
            ModelState.AddModelError(nameof(Input.Code), "Competition code is required.");
        }
        else if (Input.Code.Length > 50)
        {
            ModelState.AddModelError(nameof(Input.Code), "Competition code must not be more than 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(Input.NameEnglish))
        {
            ModelState.AddModelError(nameof(Input.NameEnglish), "English name is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.CompetitionType))
        {
            ModelState.AddModelError(nameof(Input.CompetitionType), "Competition type is required.");
        }
        else if (!CompetitionTypeOptionsSource.Any(option => option.Value == Input.CompetitionType))
        {
            ModelState.AddModelError(nameof(Input.CompetitionType), "Selected competition type is not valid.");
        }

        if (!Input.CriteriaCount.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CriteriaCount), "Criteria count is required.");
        }
        else if (Input.CriteriaCount.Value < 1 || Input.CriteriaCount.Value > 20)
        {
            ModelState.AddModelError(nameof(Input.CriteriaCount), "Criteria count must be between 1 and 20.");
        }

        if (!Input.MinJudges.HasValue)
        {
            ModelState.AddModelError(nameof(Input.MinJudges), "Minimum judges is required.");
        }
        else if (Input.MinJudges.Value < 1 || Input.MinJudges.Value > 10)
        {
            ModelState.AddModelError(nameof(Input.MinJudges), "Minimum judges must be between 1 and 10.");
        }

        if (!Input.MaxJudges.HasValue)
        {
            ModelState.AddModelError(nameof(Input.MaxJudges), "Maximum judges is required.");
        }
        else if (Input.MaxJudges.Value < 1 || Input.MaxJudges.Value > 10)
        {
            ModelState.AddModelError(nameof(Input.MaxJudges), "Maximum judges must be between 1 and 10.");
        }

        if (Input.MinJudges.HasValue &&
            Input.MaxJudges.HasValue &&
            Input.MaxJudges.Value < Input.MinJudges.Value)
        {
            ModelState.AddModelError(nameof(Input.MaxJudges), "Maximum judges must be greater than or equal to minimum judges.");
        }

        if (Input.CompetitionYearId.HasValue &&
            !string.IsNullOrWhiteSpace(Input.Code))
        {
            var codeExists = await _dbContext.Competitions
                .AsNoTracking()
                .AnyAsync(competition =>
                    competition.Id != currentCompetitionId &&
                    competition.CompetitionYearId == Input.CompetitionYearId.Value &&
                    competition.Code == Input.Code);

            if (codeExists)
            {
                ModelState.AddModelError(nameof(Input.Code), "Competition code is already used for the selected competition year.");
            }
        }

        if (Input.CompetitionYearId.HasValue &&
            !string.IsNullOrWhiteSpace(Input.NameEnglish))
        {
            var nameExists = await _dbContext.Competitions
                .AsNoTracking()
                .AnyAsync(competition =>
                    competition.Id != currentCompetitionId &&
                    competition.CompetitionYearId == Input.CompetitionYearId.Value &&
                    competition.NameEnglish.ToLower() == Input.NameEnglish.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(nameof(Input.NameEnglish), "English name is already used for the selected competition year.");
            }
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears
            .AsNoTracking()
            .Where(competitionYear => competitionYear.IsActive)
            .OrderByDescending(competitionYear => competitionYear.YearNo)
            .Select(competitionYear => new SelectListItem
            {
                Value = competitionYear.Id.ToString(),
                Text = $"{competitionYear.YearNo} - {competitionYear.DisplayName}"
            })
            .ToListAsync();

        CompetitionYearOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition year..."
        });

        CompetitionTypeOptions = CompetitionTypeOptionsSource
            .Select(option => new SelectListItem
            {
                Value = option.Value,
                Text = option.Text
            })
            .ToList();

        CompetitionTypeOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition type..."
        });
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Competition update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreateCompetitionSnapshot(Competition competition)
    {
        return new
        {
            competition.Id,
            competition.CompetitionYearId,
            competition.Code,
            competition.NameEnglish,
            competition.NameGurmukhi,
            competition.CompetitionType,
            competition.CriteriaCount,
            competition.MinJudges,
            competition.MaxJudges,
            competition.Notes,
            competition.IsActive,
            competition.PointsSchemeId,
            competition.CreatedAt,
            competition.CreatedBy,
            competition.UpdatedAt,
            competition.UpdatedBy
        };
    }
}
