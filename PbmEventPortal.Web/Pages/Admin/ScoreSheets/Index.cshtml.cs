using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;

namespace PbmEventPortal.Web.Pages.Admin.ScoreSheets;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? CompetitionYearId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CompetitionCategoryId { get; set; }

    [BindProperty]
    public ScoreSheetInputModel Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];
    public List<SelectListItem> CompetitionCategoryOptions { get; private set; } = [];
    public ScoreSheetWorkspaceModel Workspace { get; private set; } = new();
    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
        Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: false);
    }

    public async Task<JsonResult> OnGetCompetitionCategoriesAsync(Guid competitionYearId)
    {
        var categories = await LoadCompetitionCategoryOptionRowsAsync(competitionYearId);
        return new JsonResult(categories.Select(x => new
        {
            value = x.Id.ToString(),
            text = x.DisplayText,
            groupName = x.EventType == RegistrationEnums.EntryTypeIndividual ? "-- Individual Events --" : "-- Team Events --"
        }));
    }

    public async Task<IActionResult> OnGetScoreSheetWorkspaceAsync(Guid? competitionYearId, Guid? competitionCategoryId)
    {
        var workspace = await BuildWorkspaceAsync(competitionYearId, competitionCategoryId, preferPostedScores: false);
        return new PartialViewResult
        {
            ViewName = "_ScoreSheetWorkspace",
            ViewData = new ViewDataDictionary<ScoreSheetWorkspaceModel>(ViewData, workspace)
        };
    }

    public async Task<IActionResult> OnPostSaveDraftAsync()
    {
        return await SaveScoreSheetAsync(ScoreEnums.ScoreSheetStatusDraft);
    }

    public async Task<IActionResult> OnPostSubmitScoresAsync()
    {
        return await SaveScoreSheetAsync(ScoreEnums.ScoreSheetStatusSubmitted);
    }

    public async Task<IActionResult> OnPostSaveChangesAsync()
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        NormalizeInput();
        CompetitionYearId = Input.CompetitionYearId;
        CompetitionCategoryId = Input.CompetitionCategoryId;

        var validation = await ValidateScoreSheetDraftAsync();
        if (validation.Category != null && ModelState.IsValid)
        {
            ValidateSubmitCompleteness(validation);
        }

        if (!ModelState.IsValid || validation.Category == null)
        {
            ResultIsSuccess = false;
            ResultTitle = "Save Changes failed";
            ResultMessage = "Please review the score sheet changes and try again.";

            await LoadOptionsAsync();
            Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: true);
            return Page();
        }

        var now = DateTime.UtcNow;
        var scoreSheet = await _dbContext.ScoreSheets
            .Include(x => x.ScoreEntries)
                .ThenInclude(x => x.ScoreEntryScores)
            .FirstOrDefaultAsync(x =>
                x.CompetitionCategoryId == validation.Category.CompetitionCategoryId &&
                x.IsCurrent);

        if (scoreSheet == null ||
            scoreSheet.ScoreSheetStatus is not ScoreEnums.ScoreSheetStatusSubmitted and not ScoreEnums.ScoreSheetStatusPendingVerification)
        {
            ResultIsSuccess = false;
            ResultTitle = "Save Changes blocked";
            ResultMessage = "Only submitted or pending-verification score sheets can be changed through this flow.";

            await LoadOptionsAsync();
            Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: true);
            return Page();
        }

        var oldSnapshot = CreateScoreSheetSnapshot(scoreSheet);
        var notesChanged = scoreSheet.Notes != Input.Notes;
        var changedScoreEntryIds = ApplyPostSubmitChanges(scoreSheet, validation, actorId.Value, now);

        if (changedScoreEntryIds.Count == 0 && !notesChanged)
        {
            ResultIsSuccess = true;
            ResultTitle = "No changes detected";
            ResultMessage = "No score sheet changes were detected.";

            await LoadOptionsAsync();
            Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: false);
            return Page();
        }

        scoreSheet.ScoreSheetStatus = ScoreEnums.ScoreSheetStatusPendingVerification;
        scoreSheet.Notes = Input.Notes;
        scoreSheet.UpdatedAt = now;
        scoreSheet.UpdatedBy = actorId.Value;

        foreach (var scoreEntry in scoreSheet.ScoreEntries)
        {
            if (!changedScoreEntryIds.Contains(scoreEntry.Id)) continue;

            scoreEntry.EntryTotalScore = scoreEntry.ScoreEntryScores
                .Where(x => x.IsActive)
                .Sum(x => x.ScoreValue);

            scoreEntry.IsActive = scoreEntry.ScoreEntryScores.Any(x => x.IsActive);
            scoreEntry.UpdatedAt = now;
            scoreEntry.UpdatedBy = actorId.Value;
        }

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            entityName: "score_sheets",
            entityId: scoreSheet.Id,
            actionName: "OVERRIDE_PENDING_VERIFICATION",
            changedBy: actorId.Value,
            changeReason: "System Admin updated a submitted score sheet. Changes require score verification.",
            oldValues: oldSnapshot,
            newValues: CreateScoreSheetSnapshot(scoreSheet));

        ResultIsSuccess = true;
        ResultTitle = "Changes saved";
        ResultMessage = "The score sheet changes were saved and marked as pending verification.";

        await LoadOptionsAsync();
        Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: false);
        return Page();
    }

    private async Task<IActionResult> SaveScoreSheetAsync(string status)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        NormalizeInput();
        CompetitionYearId = Input.CompetitionYearId;
        CompetitionCategoryId = Input.CompetitionCategoryId;

        var isSubmit = status == ScoreEnums.ScoreSheetStatusSubmitted;
        if (status != ScoreEnums.ScoreSheetStatusDraft && status != ScoreEnums.ScoreSheetStatusSubmitted)
        {
            ResultIsSuccess = false;
            ResultTitle = "Score sheet action not supported";
            ResultMessage = "The requested score sheet action is not supported.";
            await LoadOptionsAsync();
            Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: true);
            return Page();
        }

        var validation = await ValidateScoreSheetDraftAsync();
        if (isSubmit && validation.Category != null && ModelState.IsValid)
        {
            ValidateSubmitCompleteness(validation);
        }

        if (!ModelState.IsValid || validation.Category == null)
        {
            ResultIsSuccess = false;
            ResultTitle = isSubmit ? "Submit Scores failed" : "Draft save failed";
            ResultMessage = isSubmit
                ? "Please complete all required score cells and try again."
                : "Please review the score sheet and try again.";
            await LoadOptionsAsync();
            Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: true);
            return Page();
        }

        var now = DateTime.UtcNow;
        var scoreSheet = await _dbContext.ScoreSheets
            .Include(x => x.ScoreEntries)
                .ThenInclude(x => x.ScoreEntryScores)
            .FirstOrDefaultAsync(x =>
                x.CompetitionCategoryId == validation.Category.CompetitionCategoryId &&
                x.IsCurrent);

        var isNewScoreSheet = scoreSheet == null;
        scoreSheet ??= new ScoreSheet
        {
            Id = Guid.NewGuid(),
            CompetitionCategoryId = validation.Category.CompetitionCategoryId,
            VersionNo = 1,
            ScoreSheetStatus = ScoreEnums.ScoreSheetStatusNotStarted,
            IsCurrent = true,
            CreatedAt = now,
            CreatedBy = actorId.Value
        };

        if (scoreSheet.ScoreSheetStatus is not ScoreEnums.ScoreSheetStatusNotStarted and not ScoreEnums.ScoreSheetStatusDraft)
        {
            ResultIsSuccess = false;
            ResultTitle = isSubmit ? "Submit Scores blocked" : "Draft save blocked";
            ResultMessage = "This score sheet has already moved beyond draft status. Post-submission override handling will be wired separately.";
            await LoadOptionsAsync();
            Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: true);
            return Page();
        }

        var oldSnapshot = isNewScoreSheet ? null : CreateScoreSheetSnapshot(scoreSheet);
        var scoreSheetHeaderChanged =
            isNewScoreSheet ||
            scoreSheet.ScoreSheetStatus != status ||
            scoreSheet.Notes != Input.Notes ||
            (isSubmit && (!scoreSheet.SubmittedAt.HasValue || scoreSheet.SubmittedBy != actorId.Value));

        scoreSheet.ScoreSheetStatus = status;
        scoreSheet.Notes = Input.Notes;

        if (isSubmit)
        {
            scoreSheet.SubmittedAt = now;
            scoreSheet.SubmittedBy = actorId.Value;
        }

        if (!isNewScoreSheet && scoreSheetHeaderChanged)
        {
            scoreSheet.UpdatedAt = now;
            scoreSheet.UpdatedBy = actorId.Value;
        }

        ApplyDraftScores(scoreSheet, validation, actorId.Value, now);

        if (isNewScoreSheet)
        {
            _dbContext.ScoreSheets.Add(scoreSheet);
        }

        await _dbContext.SaveChangesAsync();
        var newSnapshot = CreateScoreSheetSnapshot(scoreSheet);

        await _auditLogService.LogAsync(
            entityName: "score_sheets",
            entityId: scoreSheet.Id,
            actionName: isNewScoreSheet ? "CREATE" : isSubmit ? "SUBMIT" : "UPDATE",
            changedBy: actorId.Value,
            changeReason: isSubmit ? "System Admin submitted score sheet." : "System Admin saved score sheet draft.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = isSubmit ? "Scores submitted" : "Draft saved";
        ResultMessage = isSubmit
            ? "The score sheet has been submitted successfully. Further score changes will require the override/verification flow."
            : "The score sheet draft has been saved successfully.";

        await LoadOptionsAsync();
        Workspace = await BuildWorkspaceAsync(CompetitionYearId, CompetitionCategoryId, preferPostedScores: false);
        return Page();
    }

    private void ApplyDraftScores(ScoreSheet scoreSheet, ScoreSheetDraftValidationResult validation, Guid actorId, DateTime now)
    {
        var memberToEntryMap = validation.ApprovedEntries
            .SelectMany(entry => entry.MemberIds.Select(memberId => new
            {
                MemberId = memberId,
                entry.RegistrationEntryId
            }))
            .ToDictionary(x => x.MemberId, x => x.RegistrationEntryId);

        var validCriterionIds = validation.Criteria.Select(x => x.Id).ToHashSet();
        var scoreEntriesByEntryId = scoreSheet.ScoreEntries.ToDictionary(x => x.RegistrationEntryId, x => x);
        var changedScoreEntryIds = new HashSet<Guid>();

        foreach (var inputScore in Input.Scores.Where(x => x.RegistrationEntryMemberId != Guid.Empty && x.CriterionId != Guid.Empty))
        {
            if (!memberToEntryMap.TryGetValue(inputScore.RegistrationEntryMemberId, out var expectedEntryId)) continue;
            if (inputScore.RegistrationEntryId != expectedEntryId) continue;
            if (!validCriterionIds.Contains(inputScore.CriterionId)) continue;

            if (!scoreEntriesByEntryId.TryGetValue(inputScore.RegistrationEntryId, out var scoreEntry))
            {
                if (!inputScore.ScoreValue.HasValue) continue;

                scoreEntry = new ScoreEntry
                {
                    Id = Guid.NewGuid(),
                    ScoreSheetId = scoreSheet.Id,
                    RegistrationEntryId = inputScore.RegistrationEntryId,
                    EntryTotalScore = 0,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = actorId
                };

                scoreSheet.ScoreEntries.Add(scoreEntry);
                _dbContext.Entry(scoreEntry).State = EntityState.Added;
                scoreEntriesByEntryId.Add(inputScore.RegistrationEntryId, scoreEntry);
                changedScoreEntryIds.Add(scoreEntry.Id);
            }

            if (ApplyDraftScoreCell(scoreEntry, inputScore, actorId, now))
            {
                changedScoreEntryIds.Add(scoreEntry.Id);
            }
        }

        foreach (var scoreEntry in scoreSheet.ScoreEntries)
        {
            var activeScores = scoreEntry.ScoreEntryScores.Where(x => x.IsActive).ToList();
            var nextEntryTotalScore = activeScores.Sum(x => x.ScoreValue);
            var nextIsActive = activeScores.Count > 0;
            var parentChanged = scoreEntry.EntryTotalScore != nextEntryTotalScore || scoreEntry.IsActive != nextIsActive;

            if (parentChanged)
            {
                scoreEntry.EntryTotalScore = nextEntryTotalScore;
                scoreEntry.IsActive = nextIsActive;
            }

            if ((parentChanged || changedScoreEntryIds.Contains(scoreEntry.Id)) && scoreEntry.CreatedAt != now)
            {
                scoreEntry.UpdatedAt = now;
                scoreEntry.UpdatedBy = actorId;
            }

            if (_dbContext.Entry(scoreEntry).State == EntityState.Modified && scoreEntry.CreatedAt == now)
            {
                _dbContext.Entry(scoreEntry).State = EntityState.Added;
            }
        }
    }

    private HashSet<Guid> ApplyPostSubmitChanges(ScoreSheet scoreSheet, ScoreSheetDraftValidationResult validation, Guid actorId, DateTime now)
    {
        var memberToEntryMap = validation.ApprovedEntries
            .SelectMany(entry => entry.MemberIds.Select(memberId => new
            {
                MemberId = memberId,
                entry.RegistrationEntryId
            }))
            .ToDictionary(x => x.MemberId, x => x.RegistrationEntryId);

        var validCriterionIds = validation.Criteria.Select(x => x.Id).ToHashSet();
        var scoreEntriesByEntryId = scoreSheet.ScoreEntries.ToDictionary(x => x.RegistrationEntryId, x => x);
        var changedScoreEntryIds = new HashSet<Guid>();

        foreach (var inputScore in Input.Scores.Where(x => x.RegistrationEntryMemberId != Guid.Empty && x.CriterionId != Guid.Empty))
        {
            if (!memberToEntryMap.TryGetValue(inputScore.RegistrationEntryMemberId, out var expectedEntryId)) continue;
            if (inputScore.RegistrationEntryId != expectedEntryId) continue;
            if (!validCriterionIds.Contains(inputScore.CriterionId)) continue;
            if (!scoreEntriesByEntryId.TryGetValue(inputScore.RegistrationEntryId, out var scoreEntry)) continue;

            var existingScore = scoreEntry.ScoreEntryScores.FirstOrDefault(x =>
                x.RegistrationEntryMemberId == inputScore.RegistrationEntryMemberId &&
                x.CriterionId == inputScore.CriterionId);

            if (existingScore == null || !inputScore.ScoreValue.HasValue) continue;

            var valueChanged = existingScore.ScoreValue != inputScore.ScoreValue.Value;
            var notesChanged = existingScore.Notes != inputScore.Notes;

            if (!valueChanged && !notesChanged)
            {
                continue;
            }

            if (valueChanged)
            {
                existingScore.PreviousScoreValue = existingScore.ScoreValue;
            }

            existingScore.JudgeAssignmentId = null;
            existingScore.ScoreValue = inputScore.ScoreValue.Value;
            existingScore.ScoreSourceType = ScoreEnums.ScoreSourceTypeAdminOverride;
            existingScore.OverrideReason = "Admin updated submitted score sheet from the score sheet workspace.";
            existingScore.OverrideAt = now;
            existingScore.OverrideBy = actorId;
            existingScore.IsPendingVerification = true;
            existingScore.ChangeVerifiedAt = null;
            existingScore.ChangeVerifiedBy = null;
            existingScore.Notes = inputScore.Notes;
            existingScore.IsActive = true;
            existingScore.UpdatedAt = now;
            existingScore.UpdatedBy = actorId;

            changedScoreEntryIds.Add(scoreEntry.Id);
        }

        return changedScoreEntryIds;
    }

    private bool ApplyDraftScoreCell(ScoreEntry scoreEntry, ScoreSheetScoreInputModel inputScore, Guid actorId, DateTime now)
    {
        var existingScore = scoreEntry.ScoreEntryScores.FirstOrDefault(x =>
            x.RegistrationEntryMemberId == inputScore.RegistrationEntryMemberId &&
            x.CriterionId == inputScore.CriterionId);

        if (!inputScore.ScoreValue.HasValue)
        {
            if (existingScore == null || (!existingScore.IsActive && existingScore.Notes == null))
            {
                return false;
            }

            existingScore.IsActive = false;
            existingScore.Notes = null;
            existingScore.PreviousScoreValue = null;
            existingScore.IsPendingVerification = false;
            existingScore.ChangeVerifiedAt = null;
            existingScore.ChangeVerifiedBy = null;
            existingScore.UpdatedAt = now;
            existingScore.UpdatedBy = actorId;
            return true;
        }

        const string overrideReason = "Admin entered or updated score from the score sheet workspace.";

        if (existingScore == null)
        {
            var newScore = new ScoreEntryScore
            {
                Id = Guid.NewGuid(),
                ScoreEntryId = scoreEntry.Id,
                RegistrationEntryId = inputScore.RegistrationEntryId,
                RegistrationEntryMemberId = inputScore.RegistrationEntryMemberId,
                CriterionId = inputScore.CriterionId,
                JudgeAssignmentId = null,
                ScoreValue = inputScore.ScoreValue.Value,
                ScoreSourceType = ScoreEnums.ScoreSourceTypeAdminOverride,
                PreviousScoreValue = null,
                IsPendingVerification = false,
                OverrideReason = overrideReason,
                OverrideAt = now,
                OverrideBy = actorId,
                ChangeVerifiedAt = null,
                ChangeVerifiedBy = null,
                Notes = inputScore.Notes,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = actorId
            };

            scoreEntry.ScoreEntryScores.Add(newScore);
            _dbContext.Entry(newScore).State = EntityState.Added;
            return true;
        }

        var valueChanged = existingScore.ScoreValue != inputScore.ScoreValue.Value;
        var notesChanged = existingScore.Notes != inputScore.Notes;
        var sourceChanged =
            existingScore.ScoreSourceType != ScoreEnums.ScoreSourceTypeAdminOverride ||
            existingScore.JudgeAssignmentId != null ||
            !existingScore.IsActive ||
            existingScore.PreviousScoreValue != null ||
            existingScore.IsPendingVerification ||
            existingScore.ChangeVerifiedAt != null ||
            existingScore.ChangeVerifiedBy != null;

        if (!valueChanged && !notesChanged && !sourceChanged)
        {
            return false;
        }

        existingScore.JudgeAssignmentId = null;
        existingScore.ScoreValue = inputScore.ScoreValue.Value;
        existingScore.ScoreSourceType = ScoreEnums.ScoreSourceTypeAdminOverride;

        // Draft saves should not carry post-submit override/verification metadata.
        existingScore.PreviousScoreValue = null;
        existingScore.IsPendingVerification = false;
        existingScore.ChangeVerifiedAt = null;
        existingScore.ChangeVerifiedBy = null;

        existingScore.OverrideReason = overrideReason;
        existingScore.OverrideAt = now;
        existingScore.OverrideBy = actorId;
        existingScore.Notes = inputScore.Notes;
        existingScore.IsActive = true;
        existingScore.UpdatedAt = now;
        existingScore.UpdatedBy = actorId;
        return true;
    }

    private async Task<ScoreSheetDraftValidationResult> ValidateScoreSheetDraftAsync()
    {
        ScoreSheetCategoryValidationModel? category = null;
        var criteria = new List<ScoreSheetCriterionValidationModel>();
        var approvedEntries = new List<ApprovedEntryValidationModel>();

        if (!Input.CompetitionYearId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        }

        if (!Input.CompetitionCategoryId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Competition/category is required.");
        }

        if (Input.CompetitionYearId.HasValue && Input.CompetitionCategoryId.HasValue)
        {
            category = await _dbContext.CompetitionCategories.AsNoTracking()
                .Where(x =>
                    x.Id == Input.CompetitionCategoryId.Value &&
                    x.IsActive &&
                    x.Competition.IsActive &&
                    x.Competition.CompetitionYearId == Input.CompetitionYearId.Value)
                .Select(x => new ScoreSheetCategoryValidationModel(x.Id, x.CompetitionId))
                .FirstOrDefaultAsync();

            if (category == null)
            {
                ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition/category is not valid for this year.");
            }
        }

        if (category != null)
        {
            criteria = await _dbContext.CompetitionScoringCriteria.AsNoTracking()
                .Where(x => x.CompetitionId == category.CompetitionId && x.IsActive)
                .Select(x => new ScoreSheetCriterionValidationModel(x.Id, x.CriterionName, x.MaxScore))
                .ToListAsync();

            if (criteria.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No active scoring criteria are available for this score sheet.");
            }

            approvedEntries = await _dbContext.RegistrationEntries.AsNoTracking()
                .Where(x =>
                    x.CompetitionCategoryId == category.CompetitionCategoryId &&
                    x.IsActive &&
                    x.EntryStatus == RegistrationEnums.EntryStatusApproved)
                .Select(x => new ApprovedEntryValidationModel(
                    x.Id,
                    x.RegistrationEntryMembers
                        .Where(member => member.IsActive)
                        .Select(member => member.Id)
                        .ToList()))
                .ToListAsync();

            if (approvedEntries.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "No approved registration entries are available for this score sheet.");
            }
        }

        ValidatePostedScoreCells(criteria, approvedEntries);
        return new ScoreSheetDraftValidationResult(category, criteria, approvedEntries);
    }

    private void ValidateSubmitCompleteness(ScoreSheetDraftValidationResult validation)
    {
        if (validation.Criteria.Count == 0 || validation.ApprovedEntries.Count == 0)
        {
            return;
        }

        var submittedScoreCells = Input.Scores
            .Where(x =>
                x.RegistrationEntryId != Guid.Empty &&
                x.RegistrationEntryMemberId != Guid.Empty &&
                x.CriterionId != Guid.Empty &&
                x.ScoreValue.HasValue)
            .Select(x => (x.RegistrationEntryId, x.RegistrationEntryMemberId, x.CriterionId))
            .ToHashSet();

        var missingScoreCellCount = 0;
        foreach (var entry in validation.ApprovedEntries)
        {
            foreach (var memberId in entry.MemberIds)
            {
                foreach (var criterion in validation.Criteria)
                {
                    if (!submittedScoreCells.Contains((entry.RegistrationEntryId, memberId, criterion.Id)))
                    {
                        missingScoreCellCount++;
                    }
                }
            }
        }

        if (missingScoreCellCount > 0)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Submit Scores requires every participant to have every active criterion scored. Missing score cells: {missingScoreCellCount}.");
        }
    }

    private void ValidatePostedScoreCells(List<ScoreSheetCriterionValidationModel> criteria, List<ApprovedEntryValidationModel> approvedEntries)
    {
        var validMemberEntryMap = approvedEntries
            .SelectMany(entry => entry.MemberIds.Select(memberId => new
            {
                MemberId = memberId,
                entry.RegistrationEntryId
            }))
            .ToDictionary(x => x.MemberId, x => x.RegistrationEntryId);

        var criteriaById = criteria.ToDictionary(x => x.Id, x => x);
        var duplicateCells = Input.Scores
            .Where(x => x.RegistrationEntryMemberId != Guid.Empty && x.CriterionId != Guid.Empty)
            .GroupBy(x => new { x.RegistrationEntryId, x.RegistrationEntryMemberId, x.CriterionId })
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicateCells.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "Duplicate score cells were submitted.");
        }

        foreach (var inputScore in Input.Scores)
        {
            if (inputScore.RegistrationEntryMemberId == Guid.Empty && inputScore.CriterionId == Guid.Empty)
            {
                continue;
            }

            if (!validMemberEntryMap.TryGetValue(inputScore.RegistrationEntryMemberId, out var expectedEntryId) ||
                expectedEntryId != inputScore.RegistrationEntryId)
            {
                ModelState.AddModelError(string.Empty, "One or more score rows do not belong to this score sheet.");
                continue;
            }

            if (!criteriaById.TryGetValue(inputScore.CriterionId, out var criterion))
            {
                ModelState.AddModelError(string.Empty, "One or more score criteria do not belong to this score sheet.");
                continue;
            }

            if (!inputScore.ScoreValue.HasValue)
            {
                continue;
            }

            if (inputScore.ScoreValue.Value < 0)
            {
                ModelState.AddModelError(string.Empty, $"Score for {criterion.Name} cannot be negative.");
            }

            if (decimal.Truncate(inputScore.ScoreValue.Value) != inputScore.ScoreValue.Value)
            {
                ModelState.AddModelError(string.Empty, $"Score for {criterion.Name} must be a whole number.");
            }

            if (criterion.MaxScore.HasValue && inputScore.ScoreValue.Value > criterion.MaxScore.Value)
            {
                ModelState.AddModelError(string.Empty, $"Score for {criterion.Name} cannot exceed {criterion.MaxScore.Value:0.##}.");
            }
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private void NormalizeInput()
    {
        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();
        Input.Scores ??= [];

        foreach (var score in Input.Scores)
        {
            score.Notes = string.IsNullOrWhiteSpace(score.Notes) ? null : score.Notes.Trim();
        }
    }

    private static object CreateScoreSheetSnapshot(ScoreSheet scoreSheet)
    {
        return new
        {
            scoreSheet.Id,
            scoreSheet.CompetitionCategoryId,
            scoreSheet.VersionNo,
            scoreSheet.ScoreSheetStatus,
            scoreSheet.IsCurrent,
            scoreSheet.PreviousScoreSheetId,
            scoreSheet.ReplacedByScoreSheetId,
            scoreSheet.Notes,
            scoreSheet.SubmittedAt,
            scoreSheet.SubmittedBy,
            scoreSheet.VerifiedAt,
            scoreSheet.VerifiedBy,
            scoreSheet.CreatedAt,
            scoreSheet.CreatedBy,
            scoreSheet.UpdatedAt,
            scoreSheet.UpdatedBy,
            Entries = scoreSheet.ScoreEntries
                .OrderBy(x => x.RegistrationEntryId)
                .Select(x => new
                {
                    x.Id,
                    x.ScoreSheetId,
                    x.RegistrationEntryId,
                    x.EntryTotalScore,
                    x.IsActive,
                    x.CreatedAt,
                    x.CreatedBy,
                    x.UpdatedAt,
                    x.UpdatedBy,
                    Scores = x.ScoreEntryScores
                        .OrderBy(score => score.RegistrationEntryMemberId)
                        .ThenBy(score => score.CriterionId)
                        .Select(score => new
                        {
                            score.Id,
                            score.ScoreEntryId,
                            score.RegistrationEntryId,
                            score.RegistrationEntryMemberId,
                            score.CriterionId,
                            score.JudgeAssignmentId,
                            score.ScoreValue,
                            score.ScoreSourceType,
                            score.PreviousScoreValue,
                            score.IsPendingVerification,
                            score.OverrideReason,
                            score.OverrideAt,
                            score.OverrideBy,
                            score.ChangeVerifiedAt,
                            score.ChangeVerifiedBy,
                            score.Notes,
                            score.IsActive,
                            score.CreatedAt,
                            score.CreatedBy,
                            score.UpdatedAt,
                            score.UpdatedBy
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private async Task LoadOptionsAsync()
    {
        var years = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.YearNo)
            .Select(x => new { x.Id, x.DisplayName })
            .ToListAsync();

        CompetitionYearOptions = years.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = x.DisplayName,
            Selected = CompetitionYearId == x.Id
        }).ToList();

        CompetitionYearOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition year..."
        });

        if (!CompetitionYearId.HasValue)
        {
            CompetitionCategoryOptions = [new SelectListItem
            {
                Value = string.Empty,
                Text = "Select competition year first..."
            }];
            return;
        }

        var categories = await LoadCompetitionCategoryOptionRowsAsync(CompetitionYearId.Value);
        var individualGroup = new SelectListGroup { Name = "-- Individual Events --" };
        var teamGroup = new SelectListGroup { Name = "-- Team Events --" };

        CompetitionCategoryOptions = [new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition/category..."
        }];

        CompetitionCategoryOptions.AddRange(categories.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = x.DisplayText,
            Group = x.EventType == RegistrationEnums.EntryTypeIndividual ? individualGroup : teamGroup,
            Selected = CompetitionCategoryId == x.Id
        }));
    }

    private async Task<List<CompetitionCategoryOptionRow>> LoadCompetitionCategoryOptionRowsAsync(Guid competitionYearId)
    {
        return await _dbContext.CompetitionCategories.AsNoTracking()
            .Where(x => x.IsActive && x.Competition.IsActive && x.Competition.CompetitionYearId == competitionYearId)
            .OrderBy(x => x.EventType == RegistrationEnums.EntryTypeIndividual ? 0 : 1)
            .ThenBy(x => x.Competition.NameEnglish)
            .ThenBy(x => x.SortOrder)
            .Select(x => new CompetitionCategoryOptionRow(
                x.Id,
                x.EventType,
                x.Competition.NameEnglish + " / " + x.CategoryCode))
            .ToListAsync();
    }

    private async Task<ScoreSheetWorkspaceModel> BuildWorkspaceAsync(Guid? competitionYearId, Guid? competitionCategoryId, bool preferPostedScores)
    {
        var workspace = new ScoreSheetWorkspaceModel
        {
            Input = new ScoreSheetInputModel
            {
                CompetitionYearId = competitionYearId,
                CompetitionCategoryId = competitionCategoryId,
                Scores = preferPostedScores ? Input.Scores : []
            }
        };

        if (!competitionYearId.HasValue || !competitionCategoryId.HasValue)
        {
            workspace.EmptyMessage = "Select a competition year and competition/category to load the score sheet.";
            return workspace;
        }

        var category = await _dbContext.CompetitionCategories
            .AsNoTracking()
            .Where(x => x.Id == competitionCategoryId.Value && x.IsActive && x.Competition.IsActive && x.Competition.CompetitionYear.IsActive)
            .Select(x => new
            {
                x.Id,
                x.CompetitionId,
                x.CategoryCode,
                x.EventType,
                CompetitionDisplay = x.Competition.Code + " - " + x.Competition.NameEnglish,
                CompetitionCode = x.Competition.Code,
                CompetitionYearId = x.Competition.CompetitionYearId,
                CompetitionYearDisplay = x.Competition.CompetitionYear.DisplayName
            })
            .FirstOrDefaultAsync();

        if (category == null || category.CompetitionYearId != competitionYearId.Value)
        {
            workspace.EmptyMessage = "The selected score sheet category is not valid for this competition year.";
            return workspace;
        }

        var scoreSheet = await _dbContext.ScoreSheets.AsNoTracking()
            .Where(x => x.CompetitionCategoryId == category.Id && x.IsCurrent)
            .Select(x => new { x.Id, x.VersionNo, x.ScoreSheetStatus })
            .FirstOrDefaultAsync();

        workspace.IsLoaded = true;
        workspace.Context = new ScoreSheetContextModel
        {
            CompetitionYearDisplay = category.CompetitionYearDisplay,
            CompetitionDisplay = category.CompetitionDisplay,
            CompetitionCode = category.CompetitionCode,
            CategoryDisplay = category.CategoryCode + " / " + category.EventType,
            CategoryCode = category.CategoryCode,
            EventType = category.EventType,
            ScoreSheetStatus = scoreSheet?.ScoreSheetStatus ?? ScoreEnums.ScoreSheetStatusNotStarted,
            VersionDisplay = scoreSheet == null ? "V1" : $"V{scoreSheet.VersionNo}"
        };

        var assignmentCriterionIds = await _dbContext.JudgeAssignments.AsNoTracking()
            .Where(x => x.CompetitionCategoryId == category.Id && x.IsActive && x.CriterionId != null)
            .Select(x => x.CriterionId!.Value)
            .Distinct()
            .ToListAsync();

        workspace.Criteria = await _dbContext.CompetitionScoringCriteria.AsNoTracking()
            .Where(x => x.CompetitionId == category.CompetitionId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new ScoreSheetCriterionModel
            {
                CriterionId = x.Id,
                CriterionDisplay = x.CriterionNo + ". " + x.CriterionName,
                MaxScore = x.MaxScore,
                HasJudgeAssignment = assignmentCriterionIds.Contains(x.Id)
            })
            .ToListAsync();

        var approvedEntries = await _dbContext.RegistrationEntries.AsNoTracking()
            .Where(x => x.IsActive && x.EntryStatus == RegistrationEnums.EntryStatusApproved && x.CompetitionCategoryId == category.Id)
            .OrderBy(x => x.Team.Code)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.EntryType,
                x.EntryName,
                TeamCode = x.Team.Code,
                TeamDisplay = x.Team.Code + " - " + x.Team.NameEnglish,
                Members = x.RegistrationEntryMembers.Where(member => member.IsActive).OrderBy(member => member.MemberOrder).Select(member => new
                {
                    member.Id,
                    member.MemberOrder,
                    ParticipantName = member.Participant.FullNameAsPerId,
                    ParticipantMetaDisplay = member.Participant.Team.Code + " / " + member.Participant.TahapCode
                }).ToList(),
                ExistingScore = scoreSheet == null
                    ? null
                    : x.ScoreEntries.Where(scoreEntry => scoreEntry.ScoreSheetId == scoreSheet.Id && scoreEntry.IsActive).Select(scoreEntry => new
                    {
                        scoreEntry.Id,
                        scoreEntry.EntryTotalScore,
                        Scores = scoreEntry.ScoreEntryScores.Where(score => score.IsActive).Select(score => new
                        {
                            score.RegistrationEntryMemberId,
                            score.CriterionId,
                            score.ScoreValue,
                            score.Notes,
                            score.IsPendingVerification,
                            score.PreviousScoreValue
                        }).ToList()
                    }).FirstOrDefault()
            })
            .ToListAsync();

        var inputScores = preferPostedScores
            ? Input.Scores
            : approvedEntries
                .SelectMany(entry => entry.ExistingScore?.Scores.Select(score => new ScoreSheetScoreInputModel
                {
                    RegistrationEntryId = entry.Id,
                    RegistrationEntryMemberId = score.RegistrationEntryMemberId,
                    CriterionId = score.CriterionId,
                    ScoreValue = score.ScoreValue,
                    Notes = score.Notes
                }) ?? [])
                .ToList();

        var inputIndex = 0;
        foreach (var entry in approvedEntries)
        {
            var entryRows = new List<ScoreSheetRowModel>();

            foreach (var member in entry.Members)
            {
                var row = new ScoreSheetRowModel
                {
                    RegistrationEntryId = entry.Id,
                    RegistrationEntryMemberId = member.Id,
                    EntryDisplay = string.IsNullOrWhiteSpace(entry.EntryName) ? entry.EntryType + " entry" : entry.EntryName,
                    EntryType = entry.EntryType,
                    ParticipantName = member.ParticipantName,
                    ParticipantMetaDisplay = member.ParticipantMetaDisplay,
                    MemberOrder = member.MemberOrder,
                    TeamCode = entry.TeamCode,
                    TeamDisplay = entry.TeamDisplay,
                    IsTeamEntry = entry.EntryType == RegistrationEnums.EntryTypeTeam
                };

                foreach (var criterion in workspace.Criteria)
                {
                    var postedOrExisting = inputScores.FirstOrDefault(x =>
                        x.RegistrationEntryId == entry.Id &&
                        x.RegistrationEntryMemberId == member.Id &&
                        x.CriterionId == criterion.CriterionId);

                    var existingScore = entry.ExistingScore?.Scores.FirstOrDefault(x =>
                        x.RegistrationEntryMemberId == member.Id &&
                        x.CriterionId == criterion.CriterionId);

                    var input = new ScoreSheetScoreInputModel
                    {
                        RegistrationEntryId = entry.Id,
                        RegistrationEntryMemberId = member.Id,
                        CriterionId = criterion.CriterionId,
                        ScoreValue = postedOrExisting?.ScoreValue,
                        Notes = postedOrExisting?.Notes
                    };

                    workspace.Input.Scores.Add(input);

                    row.Scores.Add(new ScoreSheetScoreCellModel
                    {
                        InputIndex = inputIndex,
                        CriterionId = criterion.CriterionId,
                        CriterionDisplay = criterion.CriterionDisplay,
                        MaxScore = criterion.MaxScore,
                        ScoreValue = input.ScoreValue,
                        Notes = input.Notes,
                        HasJudgeAssignment = criterion.HasJudgeAssignment,
                        IsPendingVerification = existingScore?.IsPendingVerification ?? false,
                        PreviousScoreValue = existingScore?.PreviousScoreValue
                    });
                    inputIndex++;
                }

                row.SubTotal = row.Scores.Where(x => x.ScoreValue.HasValue).Sum(x => x.ScoreValue!.Value);
                entryRows.Add(row);
            }

            var teamTotal = entryRows.Sum(x => x.SubTotal);
            foreach (var row in entryRows)
            {
                row.TeamTotal = teamTotal;
                workspace.Rows.Add(row);
            }
        }

        workspace.Summary = BuildSummary(approvedEntries.Select(entry =>
        {
            var participantCount = entry.Members.Count;
            var scoreCount = entry.ExistingScore?.Scores.Count ?? 0;
            var pendingCount = entry.ExistingScore?.Scores.Count(score => score.IsPendingVerification) ?? 0;
            return new EntryProgressSnapshot(participantCount, scoreCount, pendingCount);
        }).ToList(), workspace.Criteria.Count, workspace.Context.ScoreSheetStatus);

        return workspace;
    }

    private static ScoreSheetSummaryModel BuildSummary(List<EntryProgressSnapshot> entries, int criteriaCount, string scoreSheetStatus)
    {
        var entriesInSheet = entries.Sum(x => x.ParticipantCount);
        var isSubmitted = string.Equals(scoreSheetStatus, ScoreEnums.ScoreSheetStatusSubmitted, StringComparison.OrdinalIgnoreCase);

        if (isSubmitted)
        {
            return new ScoreSheetSummaryModel
            {
                EntriesInSheet = entriesInSheet,
                NotStarted = 0,
                InProgress = 0,
                Completed = 0,
                PendingVerification = entriesInSheet
            };
        }

        return new ScoreSheetSummaryModel
        {
            EntriesInSheet = entriesInSheet,
            NotStarted = entries.Where(x => x.ScoreCount == 0).Sum(x => x.ParticipantCount),
            InProgress = entries.Where(x => x.ScoreCount > 0 && x.ScoreCount < x.ParticipantCount * criteriaCount).Sum(x => x.ParticipantCount),
            Completed = entries.Where(x => criteriaCount > 0 && x.ScoreCount >= x.ParticipantCount * criteriaCount).Sum(x => x.ParticipantCount),
            PendingVerification = entries.Where(x => x.PendingVerificationCount > 0).Sum(x => x.ParticipantCount)
        };
    }

    private sealed record EntryProgressSnapshot(int ParticipantCount, int ScoreCount, int PendingVerificationCount);
    private sealed record ScoreSheetDraftValidationResult(ScoreSheetCategoryValidationModel? Category, List<ScoreSheetCriterionValidationModel> Criteria, List<ApprovedEntryValidationModel> ApprovedEntries);
    private sealed record ScoreSheetCategoryValidationModel(Guid CompetitionCategoryId, Guid CompetitionId);
    private sealed record ScoreSheetCriterionValidationModel(Guid Id, string Name, decimal? MaxScore);
    private sealed record ApprovedEntryValidationModel(Guid RegistrationEntryId, List<Guid> MemberIds);
}
