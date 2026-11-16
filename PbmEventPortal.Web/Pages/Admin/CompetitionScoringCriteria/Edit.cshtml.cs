using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionScoringCriteria;

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
    public CompetitionScoringCriteriaEditInputDTO Input { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var competition = await LoadCompetitionAsync(id, asNoTracking: true);

        if (competition == null)
        {
            return NotFound();
        }

        var criteria = await LoadCompetitionCriteriaAsync(competition.Id, asNoTracking: true);

        if (criteria.Count == 0)
        {
            return NotFound();
        }

        Input = MapToInput(competition, criteria);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        var competition = await LoadCompetitionAsync(id, asNoTracking: true);

        if (competition == null)
        {
            return NotFound();
        }

        var criteria = await LoadCompetitionCriteriaAsync(competition.Id, asNoTracking: false);

        if (criteria.Count == 0)
        {
            return NotFound();
        }

        PopulateReadOnlyInputFields(competition);
        NormalizeInput(criteria);
        ValidateInput(criteria);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var inputById = Input.Criteria.ToDictionary(criterion => criterion.Id);
        var oldSnapshots = criteria.ToDictionary(
            criterion => criterion.Id,
            criterion => CreateScoringCriterionSnapshot(criterion));
        var oldActiveById = criteria.ToDictionary(
            criterion => criterion.Id,
            criterion => criterion.IsActive);

        var changedCriteria = criteria
            .Where(criterion => HasScoringCriterionChanged(criterion, inputById[criterion.Id]))
            .ToList();

        if (changedCriteria.Count > 0)
        {
            var now = DateTime.UtcNow;

            foreach (var criterion in changedCriteria)
            {
                var inputCriterion = inputById[criterion.Id];

                criterion.CriterionName = inputCriterion.CriterionName;
                criterion.MaxScore = inputCriterion.MaxScore;
                criterion.IsActive = inputCriterion.IsActive;
                criterion.UpdatedAt = now;
                criterion.UpdatedBy = actorId.Value;
            }

            await _dbContext.SaveChangesAsync();

            foreach (var criterion in changedCriteria)
            {
                var oldSnapshot = oldSnapshots[criterion.Id];
                var newSnapshot = CreateScoringCriterionSnapshot(criterion);
                var oldIsActive = oldActiveById[criterion.Id];

                var actionName = oldIsActive != criterion.IsActive
                    ? criterion.IsActive
                        ? "ACTIVATE"
                        : "DEACTIVATE"
                    : "UPDATE";

                var changeReason = actionName switch
                {
                    "ACTIVATE" => "System Admin reactivated competition scoring criterion.",
                    "DEACTIVATE" => "System Admin deactivated competition scoring criterion.",
                    _ => "System Admin updated competition scoring criterion."
                };

                await _auditLogService.LogAsync(
                    entityName: "competition_scoring_criteria",
                    entityId: criterion.Id,
                    actionName: actionName,
                    changedBy: actorId.Value,
                    changeReason: changeReason,
                    oldValues: oldSnapshot,
                    newValues: newSnapshot);
            }
        }

        var refreshedCriteria = await LoadCompetitionCriteriaAsync(competition.Id, asNoTracking: true);
        Input = MapToInput(competition, refreshedCriteria);

        ResultIsSuccess = true;
        ResultTitle = changedCriteria.Count == 0
            ? "No changes detected"
            : "Scoring criteria updated";
        ResultMessage = changedCriteria.Count == 0
            ? "No scoring criteria changes were detected."
            : $"{changedCriteria.Count} scoring criteria row(s) have been updated successfully for {Input.CompetitionDisplay}.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private async Task<Competition?> LoadCompetitionAsync(Guid competitionId, bool asNoTracking)
    {
        var query = _dbContext.Competitions
            .Include(competition => competition.CompetitionYear)
            .Where(competition => competition.Id == competitionId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private async Task<List<CompetitionScoringCriterion>> LoadCompetitionCriteriaAsync(Guid competitionId, bool asNoTracking)
    {
        var query = _dbContext.CompetitionScoringCriteria
            .Where(criterion => criterion.CompetitionId == competitionId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query
            .OrderBy(criterion => criterion.SortOrder)
            .ThenBy(criterion => criterion.CriterionNo)
            .ThenBy(criterion => criterion.CreatedAt)
            .ToListAsync();
    }

    private static CompetitionScoringCriteriaEditInputDTO MapToInput(
        Competition competition,
        List<CompetitionScoringCriterion> criteria)
    {
        return new CompetitionScoringCriteriaEditInputDTO
        {
            SourceScoringCriterionId = Guid.Empty,
            CompetitionId = competition.Id,
            CompetitionDisplay = FormatCompetitionDisplay(competition),
            CompetitionYearNo = competition.CompetitionYear.YearNo,
            CompetitionCode = competition.Code,
            CompetitionType = competition.CompetitionType,
            CompetitionCriteriaCount = competition.CriteriaCount,
            Criteria = criteria
                .OrderBy(criterion => criterion.SortOrder)
                .ThenBy(criterion => criterion.CriterionNo)
                .ThenBy(criterion => criterion.CreatedAt)
                .Select(criterion => new CompetitionScoringCriteriaEditRowInputViewModel
                {
                    Id = criterion.Id,
                    CriterionNo = criterion.CriterionNo,
                    CriterionName = criterion.CriterionName,
                    MaxScore = criterion.MaxScore,
                    IsActive = criterion.IsActive
                })
                .ToList()
        };
    }

    private void PopulateReadOnlyInputFields(Competition competition)
    {
        Input.SourceScoringCriterionId = Guid.Empty;
        Input.CompetitionId = competition.Id;
        Input.CompetitionDisplay = FormatCompetitionDisplay(competition);
        Input.CompetitionYearNo = competition.CompetitionYear.YearNo;
        Input.CompetitionCode = competition.Code;
        Input.CompetitionType = competition.CompetitionType;
        Input.CompetitionCriteriaCount = competition.CriteriaCount;
    }

    private void NormalizeInput(List<CompetitionScoringCriterion> criteria)
    {
        var savedCriteriaById = criteria.ToDictionary(criterion => criterion.Id);

        Input.Criteria = Input.Criteria
            .Where(criterion => savedCriteriaById.ContainsKey(criterion.Id))
            .Select(criterion => new CompetitionScoringCriteriaEditRowInputViewModel
            {
                Id = criterion.Id,
                CriterionNo = savedCriteriaById[criterion.Id].CriterionNo,
                CriterionName = string.IsNullOrWhiteSpace(criterion.CriterionName)
                    ? string.Empty
                    : criterion.CriterionName.Trim(),
                MaxScore = criterion.MaxScore,
                IsActive = criterion.IsActive
            })
            .ToList();
    }

    private void ValidateInput(List<CompetitionScoringCriterion> criteria)
    {
        if (Input.Criteria.Count != criteria.Count)
        {
            ModelState.AddModelError(nameof(Input.Criteria), "The submitted scoring criteria set does not match the saved criteria set.");
            return;
        }

        var savedCriterionIds = criteria.Select(criterion => criterion.Id).ToHashSet();
        var submittedCriterionIds = new HashSet<Guid>();

        for (var index = 0; index < Input.Criteria.Count; index++)
        {
            var criterion = Input.Criteria[index];
            var rowLabel = $"Criterion row {index + 1}";

            if (!savedCriterionIds.Contains(criterion.Id))
            {
                ModelState.AddModelError($"Input.Criteria[{index}].Id", $"{rowLabel}: Invalid scoring criterion row.");
            }
            else if (!submittedCriterionIds.Add(criterion.Id))
            {
                ModelState.AddModelError($"Input.Criteria[{index}].Id", $"{rowLabel}: This scoring criterion row was submitted more than once.");
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

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Scoring criteria update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static bool HasScoringCriterionChanged(
        CompetitionScoringCriterion criterion,
        CompetitionScoringCriteriaEditRowInputViewModel inputCriterion)
    {
        return criterion.CriterionName != inputCriterion.CriterionName ||
               criterion.MaxScore != inputCriterion.MaxScore ||
               criterion.IsActive != inputCriterion.IsActive;
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

    private static string FormatCompetitionDisplay(Competition competition)
    {
        return string.IsNullOrWhiteSpace(competition.NameGurmukhi)
            ? competition.NameEnglish
            : $"{competition.NameEnglish} / {competition.NameGurmukhi}";
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
}
