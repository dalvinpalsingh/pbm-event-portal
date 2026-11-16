using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;

namespace PbmEventPortal.Web.Pages.Admin.WinnersList;

public class IndexModel : PageModel
{
    private const int BestParticipantsDisplayCount = 5;

    private static readonly Lazy<TimeZoneInfo> MalaysiaTimeZone = new(GetMalaysiaTimeZone);

    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? CompetitionYearId { get; set; }

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];
    public WinnersListWorkspace Workspace { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await ResolveDefaultCompetitionYearAsync();
        await LoadCompetitionYearOptionsAsync();
        await LoadWorkspaceAsync();
    }

    public async Task<IActionResult> OnGetYearPreviewAsync(Guid competitionYearId)
    {
        CompetitionYearId = competitionYearId;
        await LoadWorkspaceAsync();

        if (Workspace.CompetitionYearId == Guid.Empty)
        {
            return NotFound();
        }

        return new JsonResult(Workspace, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<IActionResult> OnPostGenerateOverallStandingsAsync(Guid competitionYearId)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null)
        {
            return new JsonResult(new
            {
                success = false,
                title = "Sign in required",
                message = "Please sign in again before generating overall standings."
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
        }

        var competitionYear = await _dbContext.CompetitionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == competitionYearId);

        if (competitionYear == null)
        {
            return new JsonResult(new
            {
                success = false,
                title = "Competition year not found",
                message = "The selected competition year could not be found."
            })
            { StatusCode = StatusCodes.Status404NotFound };
        }

        var currentResults = await LoadCurrentCompetitionResultsAsync(competitionYearId);
        var verifiedScoreSheets = await LoadVerifiedScoreSheetStatusesAsync(competitionYearId);
        var pendingCompetitionResultCount = CountPendingCompetitionResults(verifiedScoreSheets, currentResults);

        if (pendingCompetitionResultCount > 0)
        {
            return new JsonResult(new
            {
                success = false,
                title = "Overall generation blocked",
                message = $"Complete {pendingCompetitionResultCount} pending competition result(s) before generating the overall standings."
            })
            { StatusCode = StatusCodes.Status409Conflict };
        }

        var overallGroups = await BuildOverallTahapGroupsAsync(competitionYearId, displayLimit: null);
        var overallRows = overallGroups.SelectMany(x => x.Rows).ToList();

        if (overallRows.Count == 0)
        {
            return new JsonResult(new
            {
                success = false,
                title = "No standings to generate",
                message = "No participant points were found from completed competition results for this competition year."
            })
            { StatusCode = StatusCodes.Status409Conflict };
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var oldCurrentRows = await _dbContext.OverallTahapResults
            .Where(x => x.CompetitionYearId == competitionYearId && x.IsCurrent)
            .ToListAsync();

        var oldSnapshot = new
        {
            current_overall_tahap_results = oldCurrentRows.Select(CreateOverallTahapResultSnapshot).ToList()
        };

        foreach (var oldRow in oldCurrentRows)
        {
            oldRow.IsCurrent = false;
            oldRow.UpdatedAt = now;
            oldRow.UpdatedBy = actorId.Value;
        }

        var nextRunNo = await _dbContext.ResultCalculationRuns.AsNoTracking()
            .Where(x => x.CompetitionYearId == competitionYearId && x.CalculationScope == ResultCalculationEnums.CalculationScopeOverallTahapResults)
            .Select(x => (int?)x.RunNo)
            .MaxAsync() ?? 0;
        nextRunNo++;

        var run = new ResultCalculationRun
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = competitionYearId,
            CalculationScope = ResultCalculationEnums.CalculationScopeOverallTahapResults,
            RunNo = nextRunNo,
            RunStatus = ResultCalculationEnums.RunStatusCompleted,
            IsCurrent = false,
            CalculatedAt = now,
            CalculatedBy = actorId.Value,
            Notes = $"Generated Winners List overall standings for {competitionYear.YearNo}.",
            CreatedAt = now,
            CreatedBy = actorId.Value
        };

        _dbContext.ResultCalculationRuns.Add(run);

        var newResults = overallRows.Select(row => new OverallTahapResult
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = competitionYearId,
            TahapCode = row.TahapCode,
            ParticipantId = row.ParticipantId,
            TeamId = row.TeamId,
            TotalPoints = row.TotalPoints,
            EventsParticipated = row.EventsParticipated,
            AveragePoints = row.AveragePoints,
            IndividualEventPoints = row.IndividualEventPoints,
            TeamEventPoints = row.TeamEventPoints,
            RankingNo = row.RankNo,
            CalculationRunNo = nextRunNo,
            CalculationRunId = run.Id,
            CalculatedAt = now,
            IsCurrent = true,
            ResultStatus = ResultCalculationEnums.ResultStatusCalculated,
            TieBreakerPolicyId = row.TieBreakerPolicyId,
            TieBreakerValuesJson = JsonSerializer.Serialize(CreateTieBreakerValuesSnapshot(row)),
            PointsBreakdownJson = JsonSerializer.Serialize(row.PointsBreakdown.Select(CreatePointsBreakdownSnapshot).ToList()),
            CreatedAt = now,
            CreatedBy = actorId.Value
        }).ToList();

        _dbContext.OverallTahapResults.AddRange(newResults);
        await _dbContext.SaveChangesAsync();

        var newSnapshot = new
        {
            run = CreateResultCalculationRunSnapshot(run),
            current_overall_tahap_results = newResults.Select(CreateOverallTahapResultSnapshot).ToList()
        };

        await _auditLogService.LogAsync(
            entityName: "overall_tahap_results",
            entityId: run.Id,
            actionName: "GENERATE_OVERALL_STANDINGS",
            changedBy: actorId.Value,
            changeReason: "System Admin generated Winners List overall standings from current completed competition results.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        await transaction.CommitAsync();

        return new JsonResult(new
        {
            success = true,
            title = "Overall standings generated",
            message = $"Generated {newResults.Count} overall standing row(s) across {overallGroups.Count} Tahap group(s)."
        });
    }

    private async Task ResolveDefaultCompetitionYearAsync()
    {
        if (CompetitionYearId.HasValue) return;

        var currentYear = DateTime.Today.Year;

        CompetitionYearId = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(x => x.IsActive && x.YearNo == currentYear)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        CompetitionYearId ??= await _dbContext.CompetitionYears.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.YearNo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        CompetitionYearId ??= await _dbContext.CompetitionYears.AsNoTracking()
            .OrderByDescending(x => x.YearNo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    private async Task LoadCompetitionYearOptionsAsync()
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears.AsNoTracking()
            .OrderByDescending(x => x.YearNo)
            .ThenBy(x => x.DisplayName)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.YearNo + " - " + x.DisplayName + (x.IsActive ? "" : " (Inactive)"),
                Selected = CompetitionYearId == x.Id
            })
            .ToListAsync();
    }

    private async Task LoadWorkspaceAsync()
    {
        Workspace = new WinnersListWorkspace();
        if (!CompetitionYearId.HasValue) return;

        var competitionYear = await _dbContext.CompetitionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == CompetitionYearId.Value);

        if (competitionYear == null) return;

        var nowUtc = DateTime.UtcNow;

        Workspace.CompetitionYearId = competitionYear.Id;
        Workspace.CompetitionYearDisplay = competitionYear.YearNo + " - " + competitionYear.DisplayName;

        var currentResults = await LoadCurrentCompetitionResultsAsync(competitionYear.Id);
        var verifiedScoreSheets = await LoadVerifiedScoreSheetStatusesAsync(competitionYear.Id);
        var currentOverallRows = await _dbContext.OverallTahapResults.AsNoTracking()
            .Where(x => x.CompetitionYearId == competitionYear.Id && x.IsCurrent)
            .ToListAsync();

        Workspace.CompetitionWinnerGroups = currentResults
            .OrderBy(x => x.CompetitionCategory.SortOrder)
            .ThenBy(x => x.Competition.NameEnglish)
            .ThenBy(x => x.CompetitionCategory.CategoryCode)
            .Select(BuildCompetitionWinnerGroup)
            .ToList();

        var tieBreakerPolicy = await LoadOverallTieBreakerPolicyAsync(competitionYear.Id);
        Workspace.TieBreakerRuleDisplay = BuildTieBreakerRuleDisplay(tieBreakerPolicy.Rules);
        Workspace.OverallTahapGroups = await BuildOverallTahapGroupsAsync(competitionYear.Id, BestParticipantsDisplayCount, tieBreakerPolicy);
        Workspace.CompletedCompetitionResultCount = currentResults.Count;
        Workspace.PendingCompetitionResultCount = CountPendingCompetitionResults(verifiedScoreSheets, currentResults);
        Workspace.OverallCurrentRowCount = currentOverallRows.Count;
        Workspace.OverallStatus = currentOverallRows.Count == 0 ? "Not generated" : "Generated";
        Workspace.OverallStatusClass = currentOverallRows.Count == 0 ? "bg-secondary" : "bg-success";
        Workspace.LastOverallGeneratedAt = currentOverallRows.Count == 0
            ? "-"
            : FormatMalaysiaDateTimeWithAgo(currentOverallRows.Max(x => x.CalculatedAt), nowUtc);
    }

    private async Task<List<CompetitionResult>> LoadCurrentCompetitionResultsAsync(Guid competitionYearId)
    {
        return await _dbContext.CompetitionResults
            .AsNoTracking()
            .AsSplitQuery()
            .Where(x => x.CompetitionYearId == competitionYearId && x.IsCurrent && x.ResultStatus == ResultCalculationEnums.ResultStatusCalculated)
            .Include(x => x.Competition)
            .Include(x => x.CompetitionCategory)
            .Include(x => x.CompetitionResultEntries)
                .ThenInclude(x => x.Team)
            .Include(x => x.CompetitionResultEntries)
                .ThenInclude(x => x.CompetitionResultParticipants)
                    .ThenInclude(x => x.Participant)
            .Include(x => x.CompetitionResultEntries)
                .ThenInclude(x => x.CompetitionResultParticipants)
                    .ThenInclude(x => x.Team)
            .Include(x => x.CompetitionResultEntries)
                .ThenInclude(x => x.CompetitionResultParticipants)
                    .ThenInclude(x => x.RegistrationEntryMember)
            .ToListAsync();
    }

    private async Task<List<VerifiedScoreSheetStatus>> LoadVerifiedScoreSheetStatusesAsync(Guid competitionYearId)
    {
        return await _dbContext.ScoreSheets.AsNoTracking()
            .Where(x => x.IsCurrent &&
                        x.ScoreSheetStatus == ScoreEnums.ScoreSheetStatusVerified &&
                        x.CompetitionCategory.Competition.CompetitionYearId == competitionYearId)
            .Select(x => new VerifiedScoreSheetStatus
            {
                ScoreSheetId = x.Id,
                CompetitionCategoryId = x.CompetitionCategoryId,
                VersionNo = x.VersionNo
            })
            .ToListAsync();
    }

    private static int CountPendingCompetitionResults(List<VerifiedScoreSheetStatus> verifiedScoreSheets, List<CompetitionResult> currentResults)
    {
        return verifiedScoreSheets.Count(sheet => !currentResults.Any(result =>
            result.CompetitionCategoryId == sheet.CompetitionCategoryId &&
            result.ScoreSheetId == sheet.ScoreSheetId &&
            result.ScoreSheetVersionNo == sheet.VersionNo));
    }

    private static CompetitionWinnerGroup BuildCompetitionWinnerGroup(CompetitionResult result)
    {
        var rows = result.CompetitionResultEntries
            .Where(x => x.IsAwarded && x.PlacingNo <= result.WinnerCount)
            .OrderBy(x => x.RankNo)
            .ThenBy(x => x.PlacingNo)
            .ThenBy(x => x.Team.Code)
            .Select(BuildCompetitionWinnerRow)
            .ToList();

        return new CompetitionWinnerGroup
        {
            CompetitionCode = result.Competition.Code,
            CompetitionName = result.Competition.NameEnglish,
            CategoryCode = result.CompetitionCategory.CategoryCode,
            CategoryDisplayName = ToDisplayName(result.CompetitionCategory.CategoryCode),
            EventType = result.CompetitionCategory.EventType,
            WinnerCount = result.WinnerCount,
            CalculatedAt = FormatMalaysiaDateTime(result.CalculatedAt),
            Rows = rows
        };
    }

    private static CompetitionWinnerRow BuildCompetitionWinnerRow(CompetitionResultEntry entry)
    {
        var participants = entry.CompetitionResultParticipants
            .OrderBy(x => x.RegistrationEntryMember.MemberOrder)
            .ThenBy(x => x.Participant.FullNameAsPerId)
            .Select(x => new WinnerParticipantRow
            {
                ParticipantName = x.Participant.FullNameAsPerId,
                TahapCode = x.ParticipantTahapCode,
                PointsAwarded = x.PointsAwarded
            })
            .ToList();

        return new CompetitionWinnerRow
        {
            RankNo = entry.RankNo,
            PlacingNo = entry.PlacingNo,
            EntryType = entry.EntryType,
            TeamCode = entry.Team.Code,
            TeamName = entry.Team.NameEnglish,
            EntryTotalScore = entry.EntryTotalScore,
            PointsAwarded = entry.PointsAwarded,
            DisplayName = entry.EntryType == RegistrationEnums.EntryTypeTeam
                ? entry.Team.Code
                : participants.FirstOrDefault()?.ParticipantName ?? entry.Team.Code,
            Participants = participants
        };
    }

    private async Task<List<OverallTahapWinnerGroup>> BuildOverallTahapGroupsAsync(Guid competitionYearId, int? displayLimit, OverallTieBreakerPolicyModel? tieBreakerPolicy = null)
    {
        var sourceRows = await _dbContext.CompetitionResultParticipants.AsNoTracking()
            .Where(x => x.CompetitionResult.CompetitionYearId == competitionYearId &&
                        x.CompetitionResult.IsCurrent &&
                        x.CompetitionResult.ResultStatus == ResultCalculationEnums.ResultStatusCalculated &&
                        x.IsAwarded)
            .Select(x => new OverallTahapSourceRow
            {
                ParticipantId = x.ParticipantId,
                ParticipantName = x.Participant.FullNameAsPerId,
                TahapCode = x.ParticipantTahapCode,
                TeamId = x.TeamId,
                TeamCode = x.Team.Code,
                TeamName = x.Team.NameEnglish,
                CompetitionCode = x.CompetitionResult.Competition.Code,
                CompetitionName = x.CompetitionResult.Competition.NameEnglish,
                CategoryCode = x.CompetitionResult.CompetitionCategory.CategoryCode,
                CategoryDisplayName = ToDisplayName(x.CompetitionResult.CompetitionCategory.CategoryCode),
                EventType = x.CompetitionResult.CompetitionCategory.EventType,
                PointsAwarded = x.PointsAwarded
            })
            .ToListAsync();

        tieBreakerPolicy ??= await LoadOverallTieBreakerPolicyAsync(competitionYearId);

        var standingRows = sourceRows
            .GroupBy(x => new { x.TahapCode, x.ParticipantId, x.ParticipantName, x.TeamId, x.TeamCode, x.TeamName })
            .Select(group =>
            {
                var rows = group.ToList();
                var totalPoints = rows.Sum(x => x.PointsAwarded);
                var eventsParticipated = rows.Count;

                return new OverallTahapWinnerRow
                {
                    TahapCode = group.Key.TahapCode,
                    ParticipantId = group.Key.ParticipantId,
                    ParticipantName = group.Key.ParticipantName,
                    TeamId = group.Key.TeamId,
                    TeamCode = group.Key.TeamCode,
                    TeamName = group.Key.TeamName,
                    TotalPoints = totalPoints,
                    EventsParticipated = eventsParticipated,
                    AveragePoints = eventsParticipated == 0 ? 0 : Math.Round(totalPoints / (decimal)eventsParticipated, 2),
                    IndividualEventPoints = rows.Where(x => x.EventType == RegistrationEnums.EntryTypeIndividual).Sum(x => x.PointsAwarded),
                    TeamEventPoints = rows.Where(x => x.EventType == RegistrationEnums.EntryTypeTeam).Sum(x => x.PointsAwarded),
                    TieBreakerPolicyId = tieBreakerPolicy.PolicyId,
                    PointsBreakdown = rows
                        .OrderBy(x => x.CompetitionCode)
                        .ThenBy(x => x.CategoryCode)
                        .Select(x => new OverallTahapPointsBreakdownRow
                        {
                            CompetitionCode = x.CompetitionCode,
                            CompetitionName = x.CompetitionName,
                            CategoryCode = x.CategoryCode,
                            CategoryDisplayName = x.CategoryDisplayName,
                            EventType = x.EventType,
                            PointsAwarded = x.PointsAwarded
                        })
                        .ToList()
                };
            })
            .ToList();

        var comparer = new OverallStandingComparer(tieBreakerPolicy.Rules, includeNameFallback: true);
        var tieOnlyComparer = new OverallStandingComparer(tieBreakerPolicy.Rules, includeNameFallback: false);

        return standingRows
            .GroupBy(x => x.TahapCode)
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var rankedRows = group.OrderBy(x => x, comparer).ToList();
                ApplyDenseRanks(rankedRows, tieOnlyComparer);
                ApplyTopScoreHighlight(rankedRows);

                return new OverallTahapWinnerGroup
                {
                    TahapCode = group.Key,
                    TahapDisplayName = ToDisplayName(group.Key),
                    TotalParticipantCount = rankedRows.Count,
                    HasTopScoreTie = rankedRows.Any(x => x.HasTopScoreTie),
                    TopScoreTieCount = rankedRows.Count(x => x.HasTopScoreTie),
                    Rows = displayLimit.HasValue ? rankedRows.Take(displayLimit.Value).ToList() : rankedRows
                };
            })
            .ToList();
    }

    private async Task<OverallTieBreakerPolicyModel> LoadOverallTieBreakerPolicyAsync(Guid competitionYearId)
    {
        var policy = await _dbContext.TieBreakerPolicies.AsNoTracking()
            .Include(x => x.TieBreakerPolicyRules)
            .Where(x => x.CompetitionYearId == competitionYearId &&
                        x.AppliesTo == ResultCalculationEnums.OverallTahapWinnerPolicyAppliesTo &&
                        x.IsActive)
            .OrderBy(x => x.PolicyName)
            .FirstOrDefaultAsync();

        var rules = policy?.TieBreakerPolicyRules
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityNo)
            .Select(x => new TieBreakerRuleDefinition
            {
                RuleCode = x.RuleCode,
                SortDirection = x.SortDirection
            })
            .ToList() ?? [];

        if (rules.Count == 0)
        {
            rules =
            [
                new TieBreakerRuleDefinition { RuleCode = "TOTAL_POINTS", SortDirection = "DESC" },
                new TieBreakerRuleDefinition { RuleCode = "AVERAGE_POINTS", SortDirection = "DESC" },
                new TieBreakerRuleDefinition { RuleCode = "EVENTS_PARTICIPATED", SortDirection = "DESC" },
                new TieBreakerRuleDefinition { RuleCode = "INDIVIDUAL_EVENT_POINTS", SortDirection = "DESC" },
                new TieBreakerRuleDefinition { RuleCode = "TEAM_EVENT_POINTS", SortDirection = "DESC" }
            ];
        }

        return new OverallTieBreakerPolicyModel
        {
            PolicyId = policy?.Id,
            Rules = rules
        };
    }

    private static void ApplyDenseRanks(List<OverallTahapWinnerRow> rows, IComparer<OverallTahapWinnerRow> tieOnlyComparer)
    {
        var rank = 0;
        OverallTahapWinnerRow? previous = null;

        foreach (var row in rows)
        {
            if (previous == null || tieOnlyComparer.Compare(previous, row) != 0)
            {
                rank++;
            }

            row.RankNo = rank;
            previous = row;
        }
    }

    private static void ApplyTopScoreHighlight(List<OverallTahapWinnerRow> rows)
    {
        if (rows.Count == 0) return;

        var highestPoints = rows.Max(x => x.TotalPoints);
        var topRows = rows.Where(x => x.TotalPoints == highestPoints).ToList();
        var hasTopScoreTie = topRows.Count > 1;
        var winningRank = topRows.Min(x => x.RankNo);

        foreach (var row in topRows)
        {
            row.IsTopScore = true;
            row.HasTopScoreTie = hasTopScoreTie;
            row.IsTieBreakerWinner = hasTopScoreTie && row.RankNo == winningRank;
            row.IsTiedOnTotalPoints = hasTopScoreTie && row.RankNo != winningRank;
        }
    }

    private static string BuildTieBreakerRuleDisplay(List<TieBreakerRuleDefinition> rules)
    {
        return string.Join(" → ", rules.Select(rule => $"{ToDisplayName(rule.RuleCode)} {(rule.SortDirection.Equals("ASC", StringComparison.OrdinalIgnoreCase) ? "↑" : "↓")}"));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static object CreateResultCalculationRunSnapshot(ResultCalculationRun run)
    {
        return new
        {
            run.Id,
            run.CompetitionYearId,
            run.CalculationScope,
            run.RunNo,
            run.RunStatus,
            run.IsCurrent,
            run.CalculatedAt,
            run.CalculatedBy,
            run.Notes,
            run.CreatedAt,
            run.CreatedBy,
            run.UpdatedAt,
            run.UpdatedBy
        };
    }

    private static object CreateOverallTahapResultSnapshot(OverallTahapResult result)
    {
        return new
        {
            result.Id,
            result.CompetitionYearId,
            result.TahapCode,
            result.ParticipantId,
            result.TeamId,
            result.TotalPoints,
            result.EventsParticipated,
            result.AveragePoints,
            result.IndividualEventPoints,
            result.TeamEventPoints,
            result.RankingNo,
            result.CalculationRunNo,
            result.CalculationRunId,
            result.CalculatedAt,
            result.IsCurrent,
            result.ResultStatus,
            result.TieBreakerPolicyId,
            result.TieBreakerValuesJson,
            result.PointsBreakdownJson,
            result.CreatedAt,
            result.CreatedBy,
            result.UpdatedAt,
            result.UpdatedBy
        };
    }

    private static object CreateTieBreakerValuesSnapshot(OverallTahapWinnerRow row)
    {
        return new
        {
            total_points = row.TotalPoints,
            average_points = row.AveragePoints,
            events_participated = row.EventsParticipated,
            individual_event_points = row.IndividualEventPoints,
            team_event_points = row.TeamEventPoints,
            rank_no = row.RankNo
        };
    }

    private static object CreatePointsBreakdownSnapshot(OverallTahapPointsBreakdownRow row)
    {
        return new
        {
            row.CompetitionCode,
            row.CompetitionName,
            row.CategoryCode,
            row.CategoryDisplayName,
            row.EventType,
            row.PointsAwarded
        };
    }

    private static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Replace('_', ' ').Trim().ToLowerInvariant();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private static string FormatMalaysiaDateTime(DateTime value)
    {
        return ConvertToMalaysiaTime(value).ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
    }

    private static string FormatMalaysiaDateTimeWithAgo(DateTime value, DateTime nowUtc)
    {
        var malaysiaTime = ConvertToMalaysiaTime(value);
        var formatted = malaysiaTime.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
        var relativeAge = BuildRelativeAgeText(NormalizeToUtc(value), nowUtc);

        if (string.IsNullOrWhiteSpace(relativeAge)) return formatted;
        return relativeAge == "just now" ? $"{formatted} (just now)" : $"{formatted} ({relativeAge} ago)";
    }

    private static DateTime ConvertToMalaysiaTime(DateTime value)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(NormalizeToUtc(value), MalaysiaTimeZone.Value);
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static TimeZoneInfo GetMalaysiaTimeZone()
    {
        foreach (var timeZoneId in new[] { "Asia/Kuala_Lumpur", "Singapore Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static string BuildRelativeAgeText(DateTime valueUtc, DateTime nowUtc)
    {
        nowUtc = NormalizeToUtc(nowUtc);
        var age = nowUtc - valueUtc;
        if (age < TimeSpan.Zero) age = TimeSpan.Zero;

        if (age.TotalMinutes < 1) return "just now";

        var days = (int)age.TotalDays;
        var hours = age.Hours;
        var minutes = age.Minutes;

        if (days > 0)
        {
            return hours > 0
                ? $"{Pluralize(days, "day")}, {Pluralize(hours, "hour")}"
                : Pluralize(days, "day");
        }

        if (hours > 0)
        {
            return minutes > 0
                ? $"{Pluralize(hours, "hour")}, {Pluralize(minutes, "minute")}"
                : Pluralize(hours, "hour");
        }

        return Pluralize(minutes, "minute");
    }

    private static string Pluralize(int value, string unit)
    {
        return value == 1 ? $"1 {unit}" : $"{value} {unit}s";
    }
}

public class WinnersListWorkspace
{
    public Guid CompetitionYearId { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public int CompletedCompetitionResultCount { get; set; }
    public int PendingCompetitionResultCount { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public string OverallStatusClass { get; set; } = string.Empty;
    public int OverallCurrentRowCount { get; set; }
    public string LastOverallGeneratedAt { get; set; } = string.Empty;
    public string TieBreakerRuleDisplay { get; set; } = string.Empty;
    public List<CompetitionWinnerGroup> CompetitionWinnerGroups { get; set; } = [];
    public List<OverallTahapWinnerGroup> OverallTahapGroups { get; set; } = [];
}

public class CompetitionWinnerGroup
{
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public short WinnerCount { get; set; }
    public string CalculatedAt { get; set; } = string.Empty;
    public List<CompetitionWinnerRow> Rows { get; set; } = [];
}

public class CompetitionWinnerRow
{
    public int RankNo { get; set; }
    public int PlacingNo { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public decimal EntryTotalScore { get; set; }
    public int PointsAwarded { get; set; }
    public List<WinnerParticipantRow> Participants { get; set; } = [];
}

public class WinnerParticipantRow
{
    public string ParticipantName { get; set; } = string.Empty;
    public string TahapCode { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
}

public class OverallTahapWinnerGroup
{
    public string TahapCode { get; set; } = string.Empty;
    public string TahapDisplayName { get; set; } = string.Empty;
    public int TotalParticipantCount { get; set; }
    public bool HasTopScoreTie { get; set; }
    public int TopScoreTieCount { get; set; }
    public List<OverallTahapWinnerRow> Rows { get; set; } = [];
}

public class OverallTahapWinnerRow
{
    public int RankNo { get; set; }
    public Guid ParticipantId { get; set; }
    public string TahapCode { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int EventsParticipated { get; set; }
    public decimal AveragePoints { get; set; }
    public int IndividualEventPoints { get; set; }
    public int TeamEventPoints { get; set; }
    public Guid? TieBreakerPolicyId { get; set; }
    public bool IsTopScore { get; set; }
    public bool HasTopScoreTie { get; set; }
    public bool IsTieBreakerWinner { get; set; }
    public bool IsTiedOnTotalPoints { get; set; }
    public List<OverallTahapPointsBreakdownRow> PointsBreakdown { get; set; } = [];
}

public class OverallTahapPointsBreakdownRow
{
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
}

public class OverallTahapSourceRow
{
    public Guid ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string TahapCode { get; set; } = string.Empty;
    public Guid TeamId { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
}

public class VerifiedScoreSheetStatus
{
    public Guid ScoreSheetId { get; set; }
    public Guid CompetitionCategoryId { get; set; }
    public int VersionNo { get; set; }
}

public class OverallTieBreakerPolicyModel
{
    public Guid? PolicyId { get; set; }
    public List<TieBreakerRuleDefinition> Rules { get; set; } = [];
}

public class TieBreakerRuleDefinition
{
    public string RuleCode { get; set; } = string.Empty;
    public string SortDirection { get; set; } = "DESC";
}

public class OverallStandingComparer : IComparer<OverallTahapWinnerRow>
{
    private readonly List<TieBreakerRuleDefinition> _rules;
    private readonly bool _includeNameFallback;

    public OverallStandingComparer(List<TieBreakerRuleDefinition> rules, bool includeNameFallback)
    {
        _rules = rules;
        _includeNameFallback = includeNameFallback;
    }

    public int Compare(OverallTahapWinnerRow? x, OverallTahapWinnerRow? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        foreach (var rule in _rules)
        {
            var comparison = GetRuleValue(x, rule.RuleCode).CompareTo(GetRuleValue(y, rule.RuleCode));
            if (comparison == 0) continue;

            return string.Equals(rule.SortDirection, "ASC", StringComparison.OrdinalIgnoreCase)
                ? comparison
                : -comparison;
        }

        if (!_includeNameFallback) return 0;

        var nameComparison = string.Compare(x.ParticipantName, y.ParticipantName, StringComparison.OrdinalIgnoreCase);
        if (nameComparison != 0) return nameComparison;

        return x.ParticipantId.CompareTo(y.ParticipantId);
    }

    private static decimal GetRuleValue(OverallTahapWinnerRow row, string ruleCode)
    {
        return ruleCode switch
        {
            "TOTAL_POINTS" => row.TotalPoints,
            "AVERAGE_POINTS" => row.AveragePoints,
            "EVENTS_PARTICIPATED" => row.EventsParticipated,
            "INDIVIDUAL_EVENT_POINTS" => row.IndividualEventPoints,
            "TEAM_EVENT_POINTS" => row.TeamEventPoints,
            _ => 0
        };
    }
}
