using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;

namespace PbmEventPortal.Web.Pages.Admin.ScoreVerification;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public List<ScoreVerificationSheetRow> PendingSheets { get; private set; } = [];
    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadPendingSheetsAsync();
    }

    public async Task<IActionResult> OnPostVerifyScoreSheetAsync(Guid scoreSheetId)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        var now = DateTime.UtcNow;
        var scoreSheet = await _dbContext.ScoreSheets
            .Include(x => x.ScoreEntries)
                .ThenInclude(x => x.ScoreEntryScores)
            .FirstOrDefaultAsync(x => x.Id == scoreSheetId && x.IsCurrent);

        if (scoreSheet == null)
        {
            ResultIsSuccess = false;
            ResultTitle = "Verification failed";
            ResultMessage = "The selected score sheet could not be found.";
            await LoadPendingSheetsAsync();
            return Page();
        }

        var pendingScores = scoreSheet.ScoreEntries
            .Where(entry => entry.IsActive)
            .SelectMany(entry => entry.ScoreEntryScores)
            .Where(score => score.IsActive && score.IsPendingVerification)
            .ToList();

        if (pendingScores.Count == 0)
        {
            ResultIsSuccess = true;
            ResultTitle = "No pending changes";
            ResultMessage = "This score sheet has no pending score changes to verify.";
            await LoadPendingSheetsAsync();
            return Page();
        }

        var oldSnapshot = CreateScoreSheetSnapshot(scoreSheet);

        foreach (var score in pendingScores)
        {
            score.IsPendingVerification = false;
            score.ChangeVerifiedAt = now;
            score.ChangeVerifiedBy = actorId.Value;
            score.UpdatedAt = now;
            score.UpdatedBy = actorId.Value;
        }

        var remainingPendingCount = scoreSheet.ScoreEntries
            .Where(entry => entry.IsActive)
            .SelectMany(entry => entry.ScoreEntryScores)
            .Count(score => score.IsActive && score.IsPendingVerification);

        if (remainingPendingCount == 0)
        {
            scoreSheet.ScoreSheetStatus = ScoreEnums.ScoreSheetStatusVerified;
            scoreSheet.VerifiedAt = now;
            scoreSheet.VerifiedBy = actorId.Value;
        }
        else
        {
            scoreSheet.ScoreSheetStatus = ScoreEnums.ScoreSheetStatusPendingVerification;
        }

        scoreSheet.UpdatedAt = now;
        scoreSheet.UpdatedBy = actorId.Value;

        await _dbContext.SaveChangesAsync();

        await _auditLogService.LogAsync(
            entityName: "score_sheets",
            entityId: scoreSheet.Id,
            actionName: "VERIFY_SCORE_CHANGES",
            changedBy: actorId.Value,
            changeReason: "Score changes were verified by an authorized user.",
            oldValues: oldSnapshot,
            newValues: CreateScoreSheetSnapshot(scoreSheet));

        ResultIsSuccess = true;
        ResultTitle = "Score changes verified";
        ResultMessage = $"Verified {pendingScores.Count} pending score change(s).";

        await LoadPendingSheetsAsync();
        return Page();
    }

    private async Task LoadPendingSheetsAsync()
    {
        var pendingScoreSheetIds = await _dbContext.ScoreSheets.AsNoTracking()
            .Where(scoreSheet =>
                scoreSheet.IsCurrent &&
                scoreSheet.ScoreEntries.Any(entry =>
                    entry.IsActive &&
                    entry.ScoreEntryScores.Any(score => score.IsActive && score.IsPendingVerification)))
            .Select(scoreSheet => scoreSheet.Id)
            .ToListAsync();

        if (pendingScoreSheetIds.Count == 0)
        {
            PendingSheets = [];
            return;
        }

        var scoreSheets = await _dbContext.ScoreSheets.AsNoTracking()
            .AsSplitQuery()
            .Include(scoreSheet => scoreSheet.CompetitionCategory)
                .ThenInclude(category => category.Competition)
                    .ThenInclude(competition => competition.CompetitionScoringCriteria)
            .Include(scoreSheet => scoreSheet.ScoreEntries)
                .ThenInclude(scoreEntry => scoreEntry.RegistrationEntry)
                    .ThenInclude(registrationEntry => registrationEntry.Team)
            .Include(scoreSheet => scoreSheet.ScoreEntries)
                .ThenInclude(scoreEntry => scoreEntry.RegistrationEntry)
                    .ThenInclude(registrationEntry => registrationEntry.RegistrationEntryMembers)
                        .ThenInclude(member => member.Participant)
            .Include(scoreSheet => scoreSheet.ScoreEntries)
                .ThenInclude(scoreEntry => scoreEntry.ScoreEntryScores)
                    .ThenInclude(score => score.Criterion)
            .Include(scoreSheet => scoreSheet.ScoreEntries)
                .ThenInclude(scoreEntry => scoreEntry.ScoreEntryScores)
                    .ThenInclude(score => score.OverrideByNavigation)
            .Where(scoreSheet => pendingScoreSheetIds.Contains(scoreSheet.Id))
            .ToListAsync();

        PendingSheets = scoreSheets
            .OrderBy(scoreSheet => scoreSheet.CompetitionCategory.Competition.NameEnglish)
            .ThenBy(scoreSheet => scoreSheet.CompetitionCategory.CategoryCode)
            .Select(BuildScoreVerificationSheetRow)
            .ToList();
    }

    private static ScoreVerificationSheetRow BuildScoreVerificationSheetRow(ScoreSheet scoreSheet)
    {
        var criteria = scoreSheet.CompetitionCategory.Competition.CompetitionScoringCriteria
            .Where(criterion => criterion.IsActive)
            .OrderBy(criterion => criterion.SortOrder)
            .ThenBy(criterion => criterion.CriterionNo)
            .Select(criterion => new ScoreVerificationCriterionRow
            {
                CriterionId = criterion.Id,
                CriterionNo = criterion.CriterionNo,
                CriterionName = criterion.CriterionName,
                MaxScore = criterion.MaxScore
            })
            .ToList();

        var sheetRow = new ScoreVerificationSheetRow
        {
            ScoreSheetId = scoreSheet.Id,
            CompetitionCode = scoreSheet.CompetitionCategory.Competition.Code,
            CompetitionName = scoreSheet.CompetitionCategory.Competition.NameEnglish,
            CategoryCode = scoreSheet.CompetitionCategory.CategoryCode,
            EventType = scoreSheet.CompetitionCategory.EventType,
            ScoreSheetStatus = scoreSheet.ScoreSheetStatus,
            VersionNo = scoreSheet.VersionNo,
            Criteria = criteria
        };

        var scoreEntries = scoreSheet.ScoreEntries
            .Where(entry => entry.IsActive)
            .OrderBy(entry => entry.RegistrationEntry.Team.Code)
            .ThenBy(entry => entry.RegistrationEntry.CreatedAt)
            .ToList();

        foreach (var scoreEntry in scoreEntries)
        {
            var memberRows = scoreEntry.RegistrationEntry.RegistrationEntryMembers
                .Where(member => member.IsActive)
                .OrderBy(member => member.MemberOrder)
                .Select(member => BuildMemberRow(scoreEntry, member, criteria))
                .ToList();

            var teamTotal = memberRows.Sum(row => row.SubTotal);
            foreach (var memberRow in memberRows)
            {
                memberRow.TeamTotal = teamTotal;
                sheetRow.Rows.Add(memberRow);
            }
        }

        sheetRow.HasTeamEntries = sheetRow.Rows.Any(row => row.IsTeamEntry);
        sheetRow.ActiveScoreEntries = sheetRow.HasTeamEntries
            ? scoreEntries.Count
            : sheetRow.Rows.Count;
        sheetRow.PendingCells = sheetRow.Rows.Sum(row => row.Cells.Count(cell => cell.IsPendingVerification));
        sheetRow.PendingEntries = sheetRow.HasTeamEntries
            ? sheetRow.Rows
                .GroupBy(row => row.RegistrationEntryId)
                .Count(group => group.Any(row => row.Cells.Any(cell => cell.IsPendingVerification)))
            : sheetRow.Rows.Count(row => row.Cells.Any(cell => cell.IsPendingVerification));
        sheetRow.LatestPendingChangeAt = sheetRow.Rows
            .SelectMany(row => row.Cells)
            .Where(cell => cell.IsPendingVerification)
            .Max(cell => cell.OverrideAt);

        return sheetRow;
    }

    private static ScoreVerificationEntryRow BuildMemberRow(
        ScoreEntry scoreEntry,
        RegistrationEntryMember member,
        List<ScoreVerificationCriterionRow> criteria)
    {
        var row = new ScoreVerificationEntryRow
        {
            RegistrationEntryId = scoreEntry.RegistrationEntryId,
            RegistrationEntryMemberId = member.Id,
            EntryType = scoreEntry.RegistrationEntry.EntryType,
            TeamCode = scoreEntry.RegistrationEntry.Team.Code,
            ParticipantName = member.Participant.FullNameAsPerId,
            MemberOrder = member.MemberOrder,
            IsTeamEntry = scoreEntry.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeTeam
        };

        foreach (var criterion in criteria)
        {
            var score = scoreEntry.ScoreEntryScores.FirstOrDefault(item =>
                item.IsActive &&
                item.RegistrationEntryMemberId == member.Id &&
                item.CriterionId == criterion.CriterionId);

            row.Cells.Add(new ScoreVerificationScoreCellRow
            {
                CriterionId = criterion.CriterionId,
                CriterionNo = criterion.CriterionNo,
                CriterionName = criterion.CriterionName,
                MaxScore = criterion.MaxScore,
                ScoreValue = score?.ScoreValue,
                PreviousScoreValue = score?.PreviousScoreValue,
                IsPendingVerification = score?.IsPendingVerification ?? false,
                Notes = score?.Notes,
                OverrideReason = score?.OverrideReason,
                OverrideAt = score?.OverrideAt,
                OverrideByName = score?.OverrideByNavigation?.FullNameAsPerId
            });
        }

        row.SubTotal = row.Cells.Where(cell => cell.ScoreValue.HasValue).Sum(cell => cell.ScoreValue!.Value);
        return row;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
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
}

public class ScoreVerificationSheetRow
{
    public Guid ScoreSheetId { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ScoreSheetStatus { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public bool HasTeamEntries { get; set; }
    public int ActiveScoreEntries { get; set; }
    public int PendingEntries { get; set; }
    public int PendingCells { get; set; }
    public DateTime? LatestPendingChangeAt { get; set; }
    public List<ScoreVerificationCriterionRow> Criteria { get; set; } = [];
    public List<ScoreVerificationEntryRow> Rows { get; set; } = [];
}

public class ScoreVerificationCriterionRow
{
    public Guid CriterionId { get; set; }
    public short CriterionNo { get; set; }
    public string CriterionName { get; set; } = string.Empty;
    public decimal? MaxScore { get; set; }
}

public class ScoreVerificationEntryRow
{
    public Guid RegistrationEntryId { get; set; }
    public Guid RegistrationEntryMemberId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string TeamCode { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public short MemberOrder { get; set; }
    public bool IsTeamEntry { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TeamTotal { get; set; }
    public List<ScoreVerificationScoreCellRow> Cells { get; set; } = [];
}

public class ScoreVerificationScoreCellRow
{
    public Guid CriterionId { get; set; }
    public short CriterionNo { get; set; }
    public string CriterionName { get; set; } = string.Empty;
    public decimal? MaxScore { get; set; }
    public decimal? ScoreValue { get; set; }
    public decimal? PreviousScoreValue { get; set; }
    public bool IsPendingVerification { get; set; }
    public string? Notes { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime? OverrideAt { get; set; }
    public string? OverrideByName { get; set; }
}
