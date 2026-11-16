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

namespace PbmEventPortal.Web.Pages.Admin.ResultsCalculation;

public class IndexModel : PageModel
{
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
    public ResultsCalculationWorkspace Workspace { get; private set; } = new();
    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

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

    public async Task<IActionResult> OnPostCalculateScoreSheetResultAsync(Guid scoreSheetId)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null)
        {
            return new JsonResult(new
            {
                success = false,
                title = "Login required",
                message = "Please log in again before calculating results."
            })
            { StatusCode = 401 };
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var scoreSheet = await LoadVerifiedScoreSheetForCalculationAsync(scoreSheetId);
        if (scoreSheet == null)
        {
            await transaction.RollbackAsync();
            return new JsonResult(new
            {
                success = false,
                title = "Score sheet not found",
                message = "The selected score sheet is not current, not VERIFIED, or no longer exists."
            })
            { StatusCode = 404 };
        }

        var competition = scoreSheet.CompetitionCategory.Competition;
        var category = scoreSheet.CompetitionCategory;
        var competitionYearId = competition.CompetitionYearId;
        var resolvedScheme = competition.PointsScheme ?? competition.CompetitionYear.DefaultPointsScheme;
        var displayName = competition.Code + " / " + category.CategoryCode;

        var validationErrors = ValidateScoreSheetCalculationReadiness(scoreSheet);
        if (validationErrors.Count > 0)
        {
            await transaction.RollbackAsync();
            return new JsonResult(new
            {
                success = false,
                title = "Calculate & Complete blocked",
                message = string.Join(" ", validationErrors)
            })
            { StatusCode = 400 };
        }

        var oldCurrentResults = await _dbContext.CompetitionResults
            .Include(x => x.CompetitionResultEntries)
            .Include(x => x.CompetitionResultParticipants)
            .Where(x => x.CompetitionCategoryId == category.Id && x.IsCurrent)
            .ToListAsync();

        var alreadyCompleted = resolvedScheme != null && oldCurrentResults.Any(x =>
            x.ScoreSheetId == scoreSheet.Id &&
            x.ScoreSheetVersionNo == scoreSheet.VersionNo &&
            x.PointsSchemeId == resolvedScheme.Id &&
            x.WinnerCount == resolvedScheme.WinnerCount &&
            x.ResultStatus == ResultCalculationEnums.ResultStatusCalculated);

        if (alreadyCompleted)
        {
            await transaction.RollbackAsync();
            return new JsonResult(new
            {
                success = false,
                title = "Already completed",
                message = displayName + " already has current calculated results."
            })
            { StatusCode = 409 };
        }

        var oldSnapshot = new
        {
            CurrentResults = oldCurrentResults.Select(CreateCompetitionResultSnapshot).ToList()
        };

        foreach (var oldResult in oldCurrentResults)
        {
            oldResult.IsCurrent = false;
            oldResult.ResultStatus = ResultCalculationEnums.ResultStatusSuperseded;
            oldResult.UpdatedAt = now;
            oldResult.UpdatedBy = actorId.Value;
        }

        if (oldCurrentResults.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        var nextRunNo = await _dbContext.ResultCalculationRuns.AsNoTracking()
            .Where(x => x.CompetitionYearId == competitionYearId && x.CalculationScope == ResultCalculationEnums.CalculationScopeCompetitionResults)
            .Select(x => (int?)x.RunNo)
            .MaxAsync() ?? 0;
        nextRunNo++;

        var calculationRun = new ResultCalculationRun
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = competitionYearId,
            CalculationScope = ResultCalculationEnums.CalculationScopeCompetitionResults,
            RunNo = nextRunNo,
            RunStatus = ResultCalculationEnums.RunStatusCompleted,
            IsCurrent = false,
            CalculatedAt = now,
            CalculatedBy = actorId.Value,
            Notes = "Calculated result for " + displayName + " from current VERIFIED score sheet.",
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        var competitionResult = BuildCompetitionResult(scoreSheet, calculationRun.Id, actorId.Value, now);
        calculationRun.CompetitionResults.Add(competitionResult);

        _dbContext.ResultCalculationRuns.Add(calculationRun);
        await _dbContext.SaveChangesAsync();

        var newSnapshot = CreateCalculationRunSnapshot(calculationRun);

        await _auditLogService.LogAsync(
            entityName: "result_calculation_runs",
            entityId: calculationRun.Id,
            actionName: "CALCULATE_SCORE_SHEET_RESULT",
            changedBy: actorId.Value,
            changeReason: "System Admin calculated and completed one VERIFIED score sheet result.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        await transaction.CommitAsync();

        return new JsonResult(new
        {
            success = true,
            title = "Results completed",
            message = displayName + " has been calculated and completed.",
            competitionYearId,
            scoreSheetId = scoreSheet.Id,
            calculationRunId = calculationRun.Id,
            competitionResultId = competitionResult.Id
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
        Workspace = new ResultsCalculationWorkspace();
        if (!CompetitionYearId.HasValue) return;

        var competitionYear = await _dbContext.CompetitionYears.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == CompetitionYearId.Value);

        if (competitionYear == null) return;

        Workspace.CompetitionYearId = competitionYear.Id;
        Workspace.CompetitionYearDisplay = competitionYear.YearNo + " - " + competitionYear.DisplayName;

        var currentResults = await _dbContext.CompetitionResults.AsNoTracking()
            .Where(x => x.CompetitionYearId == competitionYear.Id && x.IsCurrent)
            .ToDictionaryAsync(x => x.CompetitionCategoryId, x => x);

        var scoreSheets = await LoadVerifiedScoreSheetsForCalculationAsync(competitionYear.Id, asNoTracking: true);
        var nowUtc = DateTime.UtcNow;

        Workspace.Sheets = scoreSheets
            .OrderBy(x => x.CompetitionCategory.Competition.Code)
            .ThenBy(x => x.CompetitionCategory.SortOrder)
            .ThenBy(x => x.CompetitionCategory.CategoryCode)
            .Select(x => BuildSheet(x, currentResults, nowUtc))
            .ToList();

        Workspace.VerifiedSheetCount = Workspace.Sheets.Count;
        Workspace.CurrentResultCount = Workspace.Sheets.Count(x => x.ResultStatus == "Calculated/current");
        Workspace.NeedsActionCount = Workspace.Sheets.Count - Workspace.CurrentResultCount;
        Workspace.CanCalculate = Workspace.Sheets.Count > 0 && Workspace.Sheets.Any(x => x.IsCalculationReady && x.ResultStatus != "Calculated/current");
    }

    private async Task<List<ScoreSheet>> LoadVerifiedScoreSheetsForCalculationAsync(Guid competitionYearId, bool asNoTracking = false)
    {
        var query = BuildScoreSheetCalculationQuery()
            .Where(x =>
                x.IsCurrent &&
                x.ScoreSheetStatus == ScoreEnums.ScoreSheetStatusVerified &&
                x.CompetitionCategory.Competition.CompetitionYearId == competitionYearId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync();
    }

    private async Task<ScoreSheet?> LoadVerifiedScoreSheetForCalculationAsync(Guid scoreSheetId)
    {
        return await BuildScoreSheetCalculationQuery()
            .FirstOrDefaultAsync(x =>
                x.Id == scoreSheetId &&
                x.IsCurrent &&
                x.ScoreSheetStatus == ScoreEnums.ScoreSheetStatusVerified);
    }

    private IQueryable<ScoreSheet> BuildScoreSheetCalculationQuery()
    {
        return _dbContext.ScoreSheets
            .AsSplitQuery()
            .Include(x => x.CompetitionCategory)
                .ThenInclude(x => x.Competition)
                    .ThenInclude(x => x.CompetitionYear)
                        .ThenInclude(x => x.DefaultPointsScheme!)
                            .ThenInclude(x => x.PointsSchemeItems)
            .Include(x => x.CompetitionCategory)
                .ThenInclude(x => x.Competition)
                    .ThenInclude(x => x.PointsScheme!)
                        .ThenInclude(x => x.PointsSchemeItems)
            .Include(x => x.ScoreEntries)
                .ThenInclude(x => x.ScoreEntryScores)
            .Include(x => x.ScoreEntries)
                .ThenInclude(x => x.RegistrationEntry)
                    .ThenInclude(x => x.Team)
            .Include(x => x.ScoreEntries)
                .ThenInclude(x => x.RegistrationEntry)
                    .ThenInclude(x => x.RegistrationEntryMembers)
                        .ThenInclude(x => x.Participant);
    }

    private static List<string> ValidateScoreSheetCalculationReadiness(ScoreSheet scoreSheet)
    {
        var errors = new List<string>();
        var competition = scoreSheet.CompetitionCategory.Competition;
        var scheme = competition.PointsScheme ?? competition.CompetitionYear.DefaultPointsScheme;
        var displayName = competition.Code + " / " + scoreSheet.CompetitionCategory.CategoryCode;

        if (scheme == null)
        {
            errors.Add(displayName + " has no resolved points scheme.");
            return errors;
        }

        var schemePlacings = scheme.PointsSchemeItems.Select(x => x.PlacingNo).ToHashSet();
        var missingPlacings = Enumerable.Range(1, scheme.WinnerCount)
            .Where(placing => !schemePlacings.Contains((short)placing))
            .ToList();

        if (missingPlacings.Count > 0)
        {
            errors.Add(displayName + " points scheme is missing placing row(s): " + string.Join(", ", missingPlacings) + ".");
        }

        var activeEntries = scoreSheet.ScoreEntries
            .Where(x => x.IsActive && x.RegistrationEntry.IsActive)
            .ToList();

        if (activeEntries.Count == 0)
        {
            errors.Add(displayName + " has no active score entries.");
        }

        var pendingEntryCount = activeEntries.Count(entry =>
            entry.ScoreEntryScores.Any(score => score.IsActive && score.IsPendingVerification));

        if (pendingEntryCount > 0)
        {
            errors.Add(displayName + " has " + pendingEntryCount + " entr" + (pendingEntryCount == 1 ? "y" : "ies") + " pending score verification.");
        }

        return errors;
    }

    private static ResultsCalculationSheet BuildSheet(ScoreSheet scoreSheet, Dictionary<Guid, CompetitionResult> currentResults, DateTime nowUtc)
    {
        var category = scoreSheet.CompetitionCategory;
        var competition = category.Competition;
        var scheme = competition.PointsScheme ?? competition.CompetitionYear.DefaultPointsScheme;
        var schemeItems = scheme?.PointsSchemeItems.OrderBy(x => x.PlacingNo).ToList() ?? [];
        var hasCompletePointsRows = scheme != null && Enumerable.Range(1, scheme.WinnerCount)
            .All(placingNo => schemeItems.Any(item => item.PlacingNo == placingNo));

        var entries = scoreSheet.ScoreEntries
            .Where(x => x.IsActive && x.RegistrationEntry.IsActive)
            .OrderByDescending(x => x.EntryTotalScore)
            .ThenBy(x => x.RegistrationEntry.CreatedAt)
            .ThenBy(x => x.RegistrationEntryId)
            .ToList();

        var rows = entries.Select((entry, index) => BuildEntryRow(entry, index + 1, scheme, schemeItems)).ToList();
        var pendingVerificationCount = rows.Count(x => x.HasPendingVerification);

        var status = "Not calculated";
        var statusClass = "bg-secondary";
        var lastCalculated = "";

        if (currentResults.TryGetValue(category.Id, out var currentResult))
        {
            lastCalculated = FormatMalaysiaDateTimeWithAgo(currentResult.CalculatedAt, nowUtc);
            var isCurrent = scheme != null &&
                            currentResult.ScoreSheetId == scoreSheet.Id &&
                            currentResult.ScoreSheetVersionNo == scoreSheet.VersionNo &&
                            currentResult.PointsSchemeId == scheme.Id &&
                            currentResult.WinnerCount == scheme.WinnerCount &&
                            currentResult.ResultStatus == ResultCalculationEnums.ResultStatusCalculated;

            status = isCurrent ? "Calculated/current" : "Needs recalculation";
            statusClass = isCurrent ? "bg-success" : "bg-warning text-dark";
        }

        var pointRows = schemeItems.Select(x => new ResultsCalculationPointRow
        {
            PlacingNo = x.PlacingNo,
            PlacingDisplay = Ordinal(x.PlacingNo) + " place",
            PointsAwarded = x.PointsAwarded
        }).ToList();

        return new ResultsCalculationSheet
        {
            ScoreSheetId = scoreSheet.Id,
            CompetitionCode = competition.Code,
            CompetitionName = competition.NameEnglish,
            CategoryCode = category.CategoryCode,
            EventType = category.EventType,
            VersionNo = scoreSheet.VersionNo,
            VerifiedAt = FormatMalaysiaDateTimeWithAgo(scoreSheet.VerifiedAt, nowUtc),
            PointsSchemeName = scheme?.SchemeName ?? "Not configured",
            WinnerCount = scheme?.WinnerCount ?? 0,
            PointsSchemeDisplay = scheme == null ? "Not configured" : scheme.SchemeName + " - Top " + scheme.WinnerCount,
            PointsAllocationDisplay = schemeItems.Count == 0 ? "No allocation rows" : string.Join(", ", schemeItems.Select(x => Ordinal(x.PlacingNo) + " = " + x.PointsAwarded)),
            PointRows = pointRows,
            EntryCount = rows.Count,
            AwardedEntryCount = rows.Count - pendingVerificationCount,
            PendingVerificationCount = pendingVerificationCount,
            HasResolvedPointsScheme = scheme != null,
            HasCompletePointsRows = hasCompletePointsRows,
            IsCalculationReady = scheme != null && hasCompletePointsRows && rows.Count > 0 && pendingVerificationCount == 0,
            IsCompleted = status == "Calculated/current",
            ResultStatus = status,
            ResultStatusClass = statusClass,
            LastCalculatedAt = lastCalculated,
            Rows = rows
        };
    }

    private static ResultsCalculationEntryRow BuildEntryRow(ScoreEntry entry, int rankNo, PointsScheme? scheme, List<PointsSchemeItem> schemeItems)
    {
        var item = schemeItems.FirstOrDefault(x => x.PlacingNo == rankNo);
        var hasPendingVerification = entry.ScoreEntryScores.Any(x => x.IsActive && x.IsPendingVerification);
        var calculatedPoints = scheme != null && item != null && rankNo <= scheme.WinnerCount
            ? item.PointsAwarded
            : 0;
        var points = hasPendingVerification ? 0 : calculatedPoints;
        var members = entry.RegistrationEntry.RegistrationEntryMembers
            .Where(x => x.IsActive && x.Participant.IsActive)
            .OrderBy(x => x.MemberOrder)
            .ToList();

        return new ResultsCalculationEntryRow
        {
            RankNo = rankNo,
            EntryType = entry.RegistrationEntry.EntryType,
            EntryDisplayName = entry.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeTeam
                ? entry.RegistrationEntry.EntryName ?? entry.RegistrationEntry.Team.Code
                : members.FirstOrDefault()?.Participant.FullNameAsPerId ?? entry.RegistrationEntry.EntryName ?? entry.RegistrationEntry.Team.Code,
            TeamCode = entry.RegistrationEntry.Team.Code,
            TeamName = entry.RegistrationEntry.Team.NameEnglish,
            ParticipantSummary = members.Count == 0 ? "No active participants" : string.Join(", ", members.Select(x => x.Participant.FullNameAsPerId)),
            EntryTotalScore = entry.EntryTotalScore,
            PointsAwarded = points,
            HasPendingVerification = hasPendingVerification,
            IsAwarded = !hasPendingVerification,
            Participants = members.Select(x => new ResultsCalculationParticipantRow
            {
                MemberOrder = x.MemberOrder,
                ParticipantName = x.Participant.FullNameAsPerId,
                TahapCode = x.Participant.TahapCode,
                PointsAwarded = points,
                IsAwarded = !hasPendingVerification
            }).ToList()
        };
    }

    private static CompetitionResult BuildCompetitionResult(ScoreSheet scoreSheet, Guid calculationRunId, Guid actorId, DateTime now)
    {
        var category = scoreSheet.CompetitionCategory;
        var competition = category.Competition;
        var scheme = competition.PointsScheme ?? competition.CompetitionYear.DefaultPointsScheme;

        if (scheme == null)
        {
            throw new InvalidOperationException("Cannot build competition result without a resolved points scheme.");
        }

        var schemeItems = scheme.PointsSchemeItems.OrderBy(x => x.PlacingNo).ToList();
        var activeEntries = scoreSheet.ScoreEntries
            .Where(x => x.IsActive && x.RegistrationEntry.IsActive)
            .OrderByDescending(x => x.EntryTotalScore)
            .ThenBy(x => x.RegistrationEntry.CreatedAt)
            .ThenBy(x => x.RegistrationEntryId)
            .ToList();

        var competitionResult = new CompetitionResult
        {
            Id = Guid.NewGuid(),
            CalculationRunId = calculationRunId,
            CompetitionYearId = competition.CompetitionYearId,
            CompetitionId = competition.Id,
            CompetitionCategoryId = category.Id,
            ScoreSheetId = scoreSheet.Id,
            ScoreSheetVersionNo = scoreSheet.VersionNo,
            SourceScoreSheetStatus = scoreSheet.ScoreSheetStatus,
            ScoreSheetVerifiedAt = scoreSheet.VerifiedAt,
            ScoreSheetVerifiedBy = scoreSheet.VerifiedBy,
            PointsSchemeId = scheme.Id,
            WinnerCount = scheme.WinnerCount,
            EligibleEntryCount = activeEntries.Count,
            AwardedEntryCount = activeEntries.Count,
            ResultStatus = ResultCalculationEnums.ResultStatusCalculated,
            IsCurrent = true,
            CalculatedAt = now,
            CalculatedBy = actorId,
            Notes = null,
            CreatedAt = now,
            CreatedBy = actorId,
            UpdatedAt = null,
            UpdatedBy = null
        };

        foreach (var entryWithIndex in activeEntries.Select((entry, index) => new { Entry = entry, RankNo = index + 1 }))
        {
            var scoreEntry = entryWithIndex.Entry;
            var rankNo = entryWithIndex.RankNo;
            var pointsItem = schemeItems.FirstOrDefault(x => x.PlacingNo == rankNo);
            var pointsAwarded = rankNo <= scheme.WinnerCount && pointsItem != null ? pointsItem.PointsAwarded : 0;

            var resultEntry = new CompetitionResultEntry
            {
                Id = Guid.NewGuid(),
                CompetitionResultId = competitionResult.Id,
                ScoreEntryId = scoreEntry.Id,
                RegistrationEntryId = scoreEntry.RegistrationEntryId,
                TeamId = scoreEntry.RegistrationEntry.TeamId,
                EntryType = scoreEntry.RegistrationEntry.EntryType,
                RankNo = rankNo,
                PlacingNo = rankNo,
                EntryTotalScore = scoreEntry.EntryTotalScore,
                PointsAwarded = pointsAwarded,
                IsAwarded = true,
                TieBreakerValuesJson = null,
                CreatedAt = now,
                CreatedBy = actorId,
                UpdatedAt = null,
                UpdatedBy = null
            };

            foreach (var member in scoreEntry.RegistrationEntry.RegistrationEntryMembers
                         .Where(x => x.IsActive && x.Participant.IsActive)
                         .OrderBy(x => x.MemberOrder))
            {
                var resultParticipant = new CompetitionResultParticipant
                {
                    Id = Guid.NewGuid(),
                    CompetitionResultId = competitionResult.Id,
                    CompetitionResultEntryId = resultEntry.Id,
                    RegistrationEntryMemberId = member.Id,
                    ParticipantId = member.ParticipantId,
                    TeamId = scoreEntry.RegistrationEntry.TeamId,
                    ParticipantTahapCode = member.Participant.TahapCode,
                    PointsAwarded = pointsAwarded,
                    IsAwarded = true,
                    CreatedAt = now,
                    CreatedBy = actorId,
                    UpdatedAt = null,
                    UpdatedBy = null
                };

                resultEntry.CompetitionResultParticipants.Add(resultParticipant);
                competitionResult.CompetitionResultParticipants.Add(resultParticipant);
            }

            competitionResult.CompetitionResultEntries.Add(resultEntry);
        }

        return competitionResult;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string FormatMalaysiaDateTimeWithAgo(DateTime? value, DateTime nowUtc)
    {
        if (!value.HasValue) return "-";

        var malaysiaTime = ConvertToMalaysiaTime(value.Value);
        var formatted = malaysiaTime.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
        var relativeAge = BuildRelativeAgeText(NormalizeToUtc(value.Value), nowUtc);

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

    private static string Ordinal(int value)
    {
        var mod100 = value % 100;
        if (mod100 is >= 11 and <= 13) return value + "th";

        return (value % 10) switch
        {
            1 => value + "st",
            2 => value + "nd",
            3 => value + "rd",
            _ => value + "th"
        };
    }

    private static object CreateCalculationRunHeaderSnapshot(ResultCalculationRun run)
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

    private static object CreateCalculationRunSnapshot(ResultCalculationRun run)
    {
        return new
        {
            Run = CreateCalculationRunHeaderSnapshot(run),
            Results = run.CompetitionResults
                .OrderBy(x => x.CompetitionId)
                .ThenBy(x => x.CompetitionCategoryId)
                .Select(CreateCompetitionResultSnapshot)
                .ToList()
        };
    }

    private static object CreateCompetitionResultSnapshot(CompetitionResult result)
    {
        return new
        {
            result.Id,
            result.CalculationRunId,
            result.CompetitionYearId,
            result.CompetitionId,
            result.CompetitionCategoryId,
            result.ScoreSheetId,
            result.ScoreSheetVersionNo,
            result.SourceScoreSheetStatus,
            result.ScoreSheetVerifiedAt,
            result.ScoreSheetVerifiedBy,
            result.PointsSchemeId,
            result.WinnerCount,
            result.EligibleEntryCount,
            result.AwardedEntryCount,
            result.ResultStatus,
            result.IsCurrent,
            result.CalculatedAt,
            result.CalculatedBy,
            result.Notes,
            result.CreatedAt,
            result.CreatedBy,
            result.UpdatedAt,
            result.UpdatedBy,
            Entries = result.CompetitionResultEntries
                .OrderBy(x => x.RankNo)
                .ThenBy(x => x.RegistrationEntryId)
                .Select(x => new
                {
                    x.Id,
                    x.CompetitionResultId,
                    x.ScoreEntryId,
                    x.RegistrationEntryId,
                    x.TeamId,
                    x.EntryType,
                    x.RankNo,
                    x.PlacingNo,
                    x.EntryTotalScore,
                    x.PointsAwarded,
                    x.IsAwarded,
                    x.TieBreakerValuesJson,
                    x.CreatedAt,
                    x.CreatedBy,
                    x.UpdatedAt,
                    x.UpdatedBy
                })
                .ToList(),
            Participants = result.CompetitionResultParticipants
                .OrderBy(x => x.CompetitionResultEntryId)
                .ThenBy(x => x.ParticipantId)
                .Select(x => new
                {
                    x.Id,
                    x.CompetitionResultId,
                    x.CompetitionResultEntryId,
                    x.RegistrationEntryMemberId,
                    x.ParticipantId,
                    x.TeamId,
                    x.ParticipantTahapCode,
                    x.PointsAwarded,
                    x.IsAwarded,
                    x.CreatedAt,
                    x.CreatedBy,
                    x.UpdatedAt,
                    x.UpdatedBy
                })
                .ToList()
        };
    }
}

public class ResultsCalculationWorkspace
{
    public Guid CompetitionYearId { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public int VerifiedSheetCount { get; set; }
    public int CurrentResultCount { get; set; }
    public int NeedsActionCount { get; set; }
    public bool CanCalculate { get; set; }
    public List<ResultsCalculationSheet> Sheets { get; set; } = [];
}

public class ResultsCalculationSheet
{
    public Guid ScoreSheetId { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public string VerifiedAt { get; set; } = string.Empty;
    public string PointsSchemeName { get; set; } = string.Empty;
    public int WinnerCount { get; set; }
    public string PointsSchemeDisplay { get; set; } = string.Empty;
    public string PointsAllocationDisplay { get; set; } = string.Empty;
    public List<ResultsCalculationPointRow> PointRows { get; set; } = [];
    public int EntryCount { get; set; }
    public int AwardedEntryCount { get; set; }
    public int PendingVerificationCount { get; set; }
    public bool HasResolvedPointsScheme { get; set; }
    public bool HasCompletePointsRows { get; set; }
    public bool IsCalculationReady { get; set; }
    public bool IsCompleted { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public string ResultStatusClass { get; set; } = string.Empty;
    public string LastCalculatedAt { get; set; } = string.Empty;
    public List<ResultsCalculationEntryRow> Rows { get; set; } = [];
}

public class ResultsCalculationPointRow
{
    public short PlacingNo { get; set; }
    public string PlacingDisplay { get; set; } = string.Empty;
    public short PointsAwarded { get; set; }
}

public class ResultsCalculationEntryRow
{
    public int RankNo { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string EntryDisplayName { get; set; } = string.Empty;
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string ParticipantSummary { get; set; } = string.Empty;
    public decimal EntryTotalScore { get; set; }
    public int PointsAwarded { get; set; }
    public bool HasPendingVerification { get; set; }
    public bool IsAwarded { get; set; }
    public List<ResultsCalculationParticipantRow> Participants { get; set; } = [];
}

public class ResultsCalculationParticipantRow
{
    public short MemberOrder { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string TahapCode { get; set; } = string.Empty;
    public int PointsAwarded { get; set; }
    public bool IsAwarded { get; set; }
}
