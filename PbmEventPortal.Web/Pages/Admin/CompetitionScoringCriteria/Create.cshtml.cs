using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionScoringCriteria;

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
    public CompetitionScoringCriterionCreateInputDTO Input { get; set; } = new();

    public List<CompetitionOptionViewModel> CompetitionOptions { get; private set; } = [];

    public List<ExistingScoringCriterionLinkViewModel> ExistingScoringCriterionLinks { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public List<Guid> CreatedScoringCriterionIds { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadLookupOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        NormalizeInput();

        var competition = await LoadSelectedCompetitionAsync();

        await ValidateScoringCriteriaAsync(competition);

        if (!ModelState.IsValid || competition == null)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var now = DateTime.UtcNow;

        var scoringCriteria = Input.Criteria
            .OrderBy(criterion => criterion.CriterionNo!.Value)
            .Select(criterion => new CompetitionScoringCriterion
            {
                Id = Guid.NewGuid(),
                CompetitionId = competition.Id,
                CriterionNo = criterion.CriterionNo!.Value,
                CriterionName = criterion.CriterionName,
                MaxScore = criterion.MaxScore,
                SortOrder = criterion.CriterionNo!.Value,
                IsActive = criterion.IsActive,
                CreatedAt = now,
                CreatedBy = actorId.Value,
                UpdatedAt = null,
                UpdatedBy = null
            })
            .ToList();

        _dbContext.CompetitionScoringCriteria.AddRange(scoringCriteria);

        foreach (var scoringCriterion in scoringCriteria)
        {
            await _auditLogService.LogAsync(
                entityName: "competition_scoring_criteria",
                entityId: scoringCriterion.Id,
                actionName: "CREATE",
                changedBy: actorId.Value,
                changeReason: "System Admin created competition scoring criterion.",
                oldValues: null,
                newValues: CreateScoringCriterionSnapshot(scoringCriterion));
        }

        CreatedScoringCriterionIds = scoringCriteria
            .Select(criterion => criterion.Id)
            .ToList();

        ResultIsSuccess = true;
        ResultTitle = scoringCriteria.Count == 1
            ? "Scoring criterion created"
            : "Scoring criteria created";
        ResultMessage = scoringCriteria.Count == 1
            ? $"{competition.NameEnglish} - Criterion {scoringCriteria[0].CriterionNo} has been created successfully."
            : $"{scoringCriteria.Count} scoring criteria have been created successfully for {competition.NameEnglish}.";

        Input = new CompetitionScoringCriterionCreateInputDTO();

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

    private void NormalizeInput()
    {
        Input.Criteria = Input.Criteria
            .Where(criterion => criterion.CriterionNo.HasValue)
            .Select(criterion => new CompetitionScoringCriterionCreateRowInputViewModel
            {
                CriterionNo = criterion.CriterionNo,
                CriterionName = string.IsNullOrWhiteSpace(criterion.CriterionName)
                    ? string.Empty
                    : criterion.CriterionName.Trim(),
                MaxScore = criterion.MaxScore,
                IsActive = criterion.IsActive
            })
            .ToList();
    }

    private async Task<Competition?> LoadSelectedCompetitionAsync()
    {
        if (!Input.CompetitionId.HasValue)
        {
            return null;
        }

        return await _dbContext.Competitions
            .AsNoTracking()
            .FirstOrDefaultAsync(competition => competition.Id == Input.CompetitionId.Value);
    }

    private async Task ValidateScoringCriteriaAsync(Competition? competition)
    {
        if (!Input.CompetitionId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionId), "Competition is required.");
        }
        else if (competition == null || !competition.IsActive)
        {
            ModelState.AddModelError(nameof(Input.CompetitionId), "Selected competition is not valid or inactive.");
        }
        else
        {
            var alreadyHasCriteria = await _dbContext.CompetitionScoringCriteria
                .AsNoTracking()
                .AnyAsync(criterion => criterion.CompetitionId == competition.Id);

            if (alreadyHasCriteria)
            {
                ModelState.AddModelError(nameof(Input.CompetitionId), "Selected competition already has scoring criteria configured.");
            }
        }

        if (Input.Criteria.Count == 0)
        {
            ModelState.AddModelError(nameof(Input.Criteria), "At least one scoring criterion is required.");
        }

        if (competition != null && Input.Criteria.Count > competition.CriteriaCount)
        {
            ModelState.AddModelError(nameof(Input.Criteria), $"You can only configure up to {competition.CriteriaCount} criteria for the selected competition.");
        }

        var submittedCriterionNumbers = new HashSet<short>();

        for (var index = 0; index < Input.Criteria.Count; index++)
        {
            var criterion = Input.Criteria[index];
            var rowLabel = $"Criterion row {index + 1}";

            if (!criterion.CriterionNo.HasValue)
            {
                ModelState.AddModelError($"Input.Criteria[{index}].CriterionNo", $"{rowLabel}: Criterion no. is required.");
            }
            else if (criterion.CriterionNo.Value < 1)
            {
                ModelState.AddModelError($"Input.Criteria[{index}].CriterionNo", $"{rowLabel}: Criterion no. must be 1 or higher.");
            }
            else if (competition != null && criterion.CriterionNo.Value > competition.CriteriaCount)
            {
                ModelState.AddModelError($"Input.Criteria[{index}].CriterionNo", $"{rowLabel}: Criterion no. must be between 1 and {competition.CriteriaCount} for the selected competition.");
            }
            else if (!submittedCriterionNumbers.Add(criterion.CriterionNo.Value))
            {
                ModelState.AddModelError($"Input.Criteria[{index}].CriterionNo", $"{rowLabel}: Criterion no. {criterion.CriterionNo.Value} is duplicated in this form.");
            }

            if (criterion.CriterionName.Length > 100)
            {
                ModelState.AddModelError($"Input.Criteria[{index}].CriterionName", $"{rowLabel}: Criterion name must not be more than 100 characters.");
            }

            if (!IsValidMaxScore(criterion.MaxScore))
            {
                ModelState.AddModelError($"Input.Criteria[{index}].MaxScore", $"{rowLabel}: Max score must be a whole number between 0 and 100.");
            }
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        CompetitionOptions = await _dbContext.Competitions
            .AsNoTracking()
            .Where(competition => competition.IsActive && !competition.CompetitionScoringCriteria.Any())
            .OrderByDescending(competition => competition.CompetitionYear.YearNo)
            .ThenBy(competition => competition.NameEnglish)
            .Select(competition => new CompetitionOptionViewModel
            {
                Id = competition.Id,
                DisplayName = string.IsNullOrWhiteSpace(competition.NameGurmukhi)
                    ? competition.NameEnglish
                    : $"{competition.NameEnglish} / {competition.NameGurmukhi}",
                CompetitionYearNo = competition.CompetitionYear.YearNo,
                CompetitionCode = competition.Code,
                CompetitionType = competition.CompetitionType,
                CriteriaCount = competition.CriteriaCount
            })
            .ToListAsync();

        ExistingScoringCriterionLinks = await _dbContext.CompetitionScoringCriteria
            .AsNoTracking()
            .Select(criterion => new ExistingScoringCriterionLinkViewModel
            {
                CompetitionId = criterion.CompetitionId,
                CriterionNo = criterion.CriterionNo
            })
            .ToListAsync();
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Scoring criteria creation failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static bool IsValidMaxScore(decimal? maxScore)
    {
        if (!maxScore.HasValue)
        {
            return true;
        }

        return maxScore.Value >= 0 &&
               maxScore.Value <= 100 &&
               decimal.Truncate(maxScore.Value) == maxScore.Value;
    }

    private static object CreateScoringCriterionSnapshot(CompetitionScoringCriterion scoringCriterion)
    {
        return new
        {
            scoringCriterion.Id,
            scoringCriterion.CompetitionId,
            scoringCriterion.CriterionNo,
            scoringCriterion.CriterionName,
            scoringCriterion.MaxScore,
            scoringCriterion.SortOrder,
            scoringCriterion.IsActive,
            scoringCriterion.CreatedAt,
            scoringCriterion.CreatedBy,
            scoringCriterion.UpdatedAt,
            scoringCriterion.UpdatedBy
        };
    }

    public class CompetitionOptionViewModel
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public int CompetitionYearNo { get; set; }

        public string CompetitionCode { get; set; } = string.Empty;

        public string CompetitionType { get; set; } = string.Empty;

        public short CriteriaCount { get; set; }

        public string DisplayText => DisplayName;
    }

    public class ExistingScoringCriterionLinkViewModel
    {
        public Guid CompetitionId { get; set; }

        public short CriterionNo { get; set; }
    }
}
