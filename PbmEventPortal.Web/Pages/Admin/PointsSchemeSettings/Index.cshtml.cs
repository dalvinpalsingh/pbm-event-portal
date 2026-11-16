using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;

namespace PbmEventPortal.Web.Pages.Admin.PointsSchemeSettings;

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

    [BindProperty]
    public PointsSchemeSettingsInputModel Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];
    public List<PointsSchemeOptionModel> PointsSchemeOptions { get; private set; } = [];
    public PointsSchemeSettingsWorkspaceModel Workspace { get; private set; } = new();

    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadPageAsync(preferPostedInput: false);
    }

    public async Task<IActionResult> OnGetYearSettingsAsync(Guid competitionYearId)
    {
        CompetitionYearId = competitionYearId;
        await LoadLookupOptionsAsync();
        await BuildWorkspaceAsync(preferPostedInput: false);

        if (Workspace.CompetitionYearId == Guid.Empty)
        {
            return NotFound();
        }

        return new JsonResult(new
        {
            competitionYearId = Workspace.CompetitionYearId,
            competitionYearDisplay = Workspace.CompetitionYearDisplay,
            defaultPointsSchemeId = Workspace.DefaultPointsSchemeId,
            defaultPointsSchemeDisplay = Workspace.DefaultPointsSchemeDisplay,
            competitionCount = Workspace.CompetitionCount,
            overrideCount = Workspace.OverrideCount,
            competitions = Workspace.Competitions.Select(x => new
            {
                competitionId = x.CompetitionId,
                competitionCode = x.CompetitionCode,
                competitionName = x.CompetitionName,
                competitionType = x.CompetitionType,
                isActive = x.IsActive,
                pointsSchemeId = x.PointsSchemeId,
                resolvedPointsSchemeId = x.ResolvedPointsSchemeId,
                resolvedPointsSchemeDisplay = x.ResolvedPointsSchemeDisplay,
                isOverride = x.IsOverride,
                inputIndex = x.InputIndex
            }).ToList()
        });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        CompetitionYearId = Input.CompetitionYearId;
        await LoadLookupOptionsAsync();

        var competitionYear = await _dbContext.CompetitionYears
            .Include(x => x.Competitions)
            .FirstOrDefaultAsync(x => x.Id == Input.CompetitionYearId);

        if (competitionYear == null)
        {
            ModelState.AddModelError(string.Empty, "Competition year could not be found.");
        }

        var activePointsSchemes = await _dbContext.PointsSchemes
            .Include(x => x.PointsSchemeItems)
            .Where(x => x.IsActive)
            .ToListAsync();

        NormalizeCompetitionOverridesToDefault();
        ValidateInput(competitionYear, activePointsSchemes);

        if (!ModelState.IsValid || competitionYear == null)
        {
            ResultIsSuccess = false;
            ResultTitle = "Save failed";
            ResultMessage = "Please review the points scheme settings and try again.";
            await BuildWorkspaceAsync(preferPostedInput: true);
            return Page();
        }

        var now = DateTime.UtcNow;
        var oldYearSnapshot = CreateCompetitionYearSnapshot(competitionYear);
        var yearChanged = competitionYear.DefaultPointsSchemeId != Input.DefaultPointsSchemeId;

        var competitionSettings = Input.CompetitionSettings.ToDictionary(x => x.CompetitionId, x => x.PointsSchemeId);
        var changedCompetitions = new List<(Competition Competition, object OldSnapshot)>();

        foreach (var competition in competitionYear.Competitions)
        {
            if (!competitionSettings.TryGetValue(competition.Id, out var newPointsSchemeId))
            {
                continue;
            }

            if (competition.PointsSchemeId == newPointsSchemeId)
            {
                continue;
            }

            changedCompetitions.Add((competition, CreateCompetitionSnapshot(competition)));
            competition.PointsSchemeId = newPointsSchemeId;
            competition.UpdatedAt = now;
            competition.UpdatedBy = actorId.Value;
        }

        if (!yearChanged && changedCompetitions.Count == 0)
        {
            ResultIsSuccess = true;
            ResultTitle = "No changes detected";
            ResultMessage = "No points scheme settings were changed.";
            await LoadPageAsync(preferPostedInput: false);
            return Page();
        }

        if (yearChanged)
        {
            competitionYear.DefaultPointsSchemeId = Input.DefaultPointsSchemeId;
            competitionYear.UpdatedAt = now;
            competitionYear.UpdatedBy = actorId.Value;
        }

        await _dbContext.SaveChangesAsync();

        if (yearChanged)
        {
            await _auditLogService.LogAsync(
                entityName: "competition_years",
                entityId: competitionYear.Id,
                actionName: "UPDATE",
                changedBy: actorId.Value,
                changeReason: "Updated event-level default points scheme.",
                oldValues: oldYearSnapshot,
                newValues: CreateCompetitionYearSnapshot(competitionYear));
        }

        foreach (var changedCompetition in changedCompetitions)
        {
            await _auditLogService.LogAsync(
                entityName: "competitions",
                entityId: changedCompetition.Competition.Id,
                actionName: "UPDATE",
                changedBy: actorId.Value,
                changeReason: "Updated competition-level points scheme override.",
                oldValues: changedCompetition.OldSnapshot,
                newValues: CreateCompetitionSnapshot(changedCompetition.Competition));
        }

        ResultIsSuccess = true;
        ResultTitle = "Points scheme settings saved";
        ResultMessage = "The event default and competition override points scheme settings were saved.";

        await LoadPageAsync(preferPostedInput: false);
        return Page();
    }

    private async Task LoadPageAsync(bool preferPostedInput)
    {
        await ResolveDefaultCompetitionYearAsync();
        await LoadLookupOptionsAsync();
        await BuildWorkspaceAsync(preferPostedInput);
    }

    private async Task LoadLookupOptionsAsync()
    {
        var competitionYears = await _dbContext.CompetitionYears.AsNoTracking()
            .OrderByDescending(x => x.YearNo)
            .ThenBy(x => x.DisplayName)
            .Select(x => new
            {
                x.Id,
                x.YearNo,
                x.DisplayName,
                x.IsActive
            })
            .ToListAsync();

        CompetitionYearOptions = competitionYears
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.YearNo} - {x.DisplayName}{(x.IsActive ? string.Empty : " (Inactive)")}",
                Selected = CompetitionYearId == x.Id
            })
            .ToList();

        var pointsSchemes = await _dbContext.PointsSchemes.AsNoTracking()
            .Include(x => x.PointsSchemeItems)
            .Where(x => x.IsActive)
            .OrderBy(x => x.WinnerCount)
            .ThenBy(x => x.SchemeName)
            .ToListAsync();

        PointsSchemeOptions = pointsSchemes
            .Select(x => new PointsSchemeOptionModel
            {
                Id = x.Id,
                SchemeName = x.SchemeName,
                WinnerCount = x.WinnerCount,
                Items = x.PointsSchemeItems
                    .OrderBy(item => item.PlacingNo)
                    .Select(item => new PointsSchemeItemOptionModel
                    {
                        PlacingNo = item.PlacingNo,
                        PointsAwarded = item.PointsAwarded
                    })
                    .ToList()
            })
            .ToList();
    }

    private async Task ResolveDefaultCompetitionYearAsync()
    {
        if (CompetitionYearId.HasValue)
        {
            return;
        }

        var currentYear = DateTime.Today.Year;

        CompetitionYearId = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(x => x.IsActive && x.YearNo == currentYear)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        if (CompetitionYearId.HasValue)
        {
            return;
        }

        CompetitionYearId = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.YearNo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        if (CompetitionYearId.HasValue)
        {
            return;
        }

        CompetitionYearId = await _dbContext.CompetitionYears.AsNoTracking()
            .OrderByDescending(x => x.YearNo)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    private async Task BuildWorkspaceAsync(bool preferPostedInput)
    {
        Workspace = new PointsSchemeSettingsWorkspaceModel();

        if (!CompetitionYearId.HasValue)
        {
            Input = new PointsSchemeSettingsInputModel();
            return;
        }

        var competitionYear = await _dbContext.CompetitionYears.AsNoTracking()
            .Include(x => x.Competitions)
            .Include(x => x.DefaultPointsScheme)
            .FirstOrDefaultAsync(x => x.Id == CompetitionYearId.Value);

        if (competitionYear == null)
        {
            Input = new PointsSchemeSettingsInputModel();
            return;
        }

        var competitions = competitionYear.Competitions
            .OrderBy(x => x.CompetitionType)
            .ThenBy(x => x.NameEnglish)
            .ToList();

        var postedCompetitionMap = preferPostedInput
            ? Input.CompetitionSettings.ToDictionary(x => x.CompetitionId, x => x.PointsSchemeId)
            : new Dictionary<Guid, Guid?>();

        var defaultPointsSchemeId = preferPostedInput ? Input.DefaultPointsSchemeId : competitionYear.DefaultPointsSchemeId;
        var defaultPointsSchemeName = ResolvePointsSchemeDisplay(defaultPointsSchemeId);

        Workspace = new PointsSchemeSettingsWorkspaceModel
        {
            CompetitionYearId = competitionYear.Id,
            CompetitionYearDisplay = $"{competitionYear.YearNo} - {competitionYear.DisplayName}",
            DefaultPointsSchemeId = defaultPointsSchemeId,
            DefaultPointsSchemeDisplay = defaultPointsSchemeName,
            CompetitionCount = competitions.Count,
            OverrideCount = competitions.Count(x =>
            {
                var pointsSchemeId = preferPostedInput && postedCompetitionMap.TryGetValue(x.Id, out var postedValue)
                    ? postedValue
                    : x.PointsSchemeId;
                return pointsSchemeId.HasValue && pointsSchemeId != defaultPointsSchemeId;
            })
        };

        Input = new PointsSchemeSettingsInputModel
        {
            CompetitionYearId = competitionYear.Id,
            DefaultPointsSchemeId = defaultPointsSchemeId,
            CompetitionSettings = competitions.Select(competition =>
            {
                var pointsSchemeId = preferPostedInput && postedCompetitionMap.TryGetValue(competition.Id, out var postedValue)
                    ? postedValue
                    : competition.PointsSchemeId;

                if (defaultPointsSchemeId.HasValue && pointsSchemeId == defaultPointsSchemeId)
                {
                    pointsSchemeId = null;
                }

                return new CompetitionPointsSchemeInputModel
                {
                    CompetitionId = competition.Id,
                    PointsSchemeId = pointsSchemeId
                };
            }).ToList()
        };

        Workspace.Competitions = competitions.Select((competition, index) =>
        {
            var pointsSchemeId = Input.CompetitionSettings[index].PointsSchemeId;
            var isOverride = pointsSchemeId.HasValue;
            var resolvedPointsSchemeId = pointsSchemeId ?? defaultPointsSchemeId;

            return new CompetitionPointsSchemeRowModel
            {
                CompetitionId = competition.Id,
                CompetitionCode = competition.Code,
                CompetitionName = competition.NameEnglish,
                CompetitionType = competition.CompetitionType,
                IsActive = competition.IsActive,
                PointsSchemeId = pointsSchemeId,
                ResolvedPointsSchemeId = resolvedPointsSchemeId,
                IsOverride = isOverride,
                ResolvedPointsSchemeDisplay = ResolvePointsSchemeDisplay(resolvedPointsSchemeId),
                InputIndex = index
            };
        }).ToList();
    }

    private void NormalizeCompetitionOverridesToDefault()
    {
        if (!Input.DefaultPointsSchemeId.HasValue)
        {
            return;
        }

        foreach (var competitionSetting in Input.CompetitionSettings)
        {
            if (competitionSetting.PointsSchemeId == Input.DefaultPointsSchemeId)
            {
                competitionSetting.PointsSchemeId = null;
            }
        }
    }

    private void ValidateInput(CompetitionYear? competitionYear, List<PointsScheme> activePointsSchemes)
    {
        if (competitionYear == null)
        {
            return;
        }

        if (!Input.DefaultPointsSchemeId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.DefaultPointsSchemeId), "Select an event-level default points scheme.");
            return;
        }

        var activeSchemeIds = activePointsSchemes.Select(x => x.Id).ToHashSet();
        if (!activeSchemeIds.Contains(Input.DefaultPointsSchemeId.Value))
        {
            ModelState.AddModelError(nameof(Input.DefaultPointsSchemeId), "Selected event-level default points scheme is not active.");
        }

        var validCompetitionIds = competitionYear.Competitions.Select(x => x.Id).ToHashSet();
        var duplicateCompetitionIds = Input.CompetitionSettings
            .GroupBy(x => x.CompetitionId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateCompetitionIds.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "Duplicate competition points scheme rows were submitted.");
        }

        foreach (var competitionSetting in Input.CompetitionSettings)
        {
            if (!validCompetitionIds.Contains(competitionSetting.CompetitionId))
            {
                ModelState.AddModelError(string.Empty, "Submitted competition setting does not belong to the selected competition year.");
                continue;
            }

            if (competitionSetting.PointsSchemeId.HasValue && !activeSchemeIds.Contains(competitionSetting.PointsSchemeId.Value))
            {
                ModelState.AddModelError(string.Empty, "Selected competition points scheme override is not active.");
            }
        }

        var selectedSchemeIds = Input.CompetitionSettings
            .Where(x => x.PointsSchemeId.HasValue)
            .Select(x => x.PointsSchemeId!.Value)
            .Append(Input.DefaultPointsSchemeId.Value)
            .Distinct()
            .ToList();

        foreach (var scheme in activePointsSchemes.Where(x => selectedSchemeIds.Contains(x.Id)))
        {
            var placingNumbers = scheme.PointsSchemeItems.Select(x => x.PlacingNo).ToHashSet();
            var missingPlacings = Enumerable.Range(1, scheme.WinnerCount)
                .Where(placingNo => !placingNumbers.Contains((short)placingNo))
                .ToList();

            if (missingPlacings.Count > 0)
            {
                ModelState.AddModelError(string.Empty, $"Points scheme '{scheme.SchemeName}' is missing placing rows: {string.Join(", ", missingPlacings)}.");
            }
        }
    }

    private string ResolvePointsSchemeDisplay(Guid? pointsSchemeId)
    {
        if (!pointsSchemeId.HasValue)
        {
            return "Not selected";
        }

        var scheme = PointsSchemeOptions.FirstOrDefault(x => x.Id == pointsSchemeId.Value);
        return scheme == null ? "Unknown / inactive scheme" : scheme.CompactDisplayName;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
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
            competitionYear.DefaultPointsSchemeId,
            competitionYear.CreatedAt,
            competitionYear.CreatedBy,
            competitionYear.UpdatedAt,
            competitionYear.UpdatedBy
        };
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

public class PointsSchemeSettingsInputModel
{
    public Guid CompetitionYearId { get; set; }
    public Guid? DefaultPointsSchemeId { get; set; }
    public List<CompetitionPointsSchemeInputModel> CompetitionSettings { get; set; } = [];
}

public class CompetitionPointsSchemeInputModel
{
    public Guid CompetitionId { get; set; }
    public Guid? PointsSchemeId { get; set; }
}

public class PointsSchemeSettingsWorkspaceModel
{
    public Guid CompetitionYearId { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public Guid? DefaultPointsSchemeId { get; set; }
    public string DefaultPointsSchemeDisplay { get; set; } = "Not selected";
    public int CompetitionCount { get; set; }
    public int OverrideCount { get; set; }
    public List<CompetitionPointsSchemeRowModel> Competitions { get; set; } = [];
}

public class CompetitionPointsSchemeRowModel
{
    public Guid CompetitionId { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CompetitionType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? PointsSchemeId { get; set; }
    public Guid? ResolvedPointsSchemeId { get; set; }
    public string ResolvedPointsSchemeDisplay { get; set; } = string.Empty;
    public bool IsOverride { get; set; }
    public int InputIndex { get; set; }
}

public class PointsSchemeOptionModel
{
    public Guid Id { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public short WinnerCount { get; set; }
    public List<PointsSchemeItemOptionModel> Items { get; set; } = [];

    public string CompactDisplayName => $"{SchemeName} — Top {WinnerCount} place{(WinnerCount == 1 ? string.Empty : "s")}";
}

public class PointsSchemeItemOptionModel
{
    public short PlacingNo { get; set; }
    public short PointsAwarded { get; set; }
}
