using System.Globalization;
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

public class CreateModel : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> CompetitionTypeOptionsSource =
    [
        new SelectListItem { Value = CompetitionEnums.CompetitionTypeIndividual, Text = "Individual" },
        new SelectListItem { Value = CompetitionEnums.CompetitionTypeTeam, Text = "Team" }
    ];

    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;

    public CreateModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    [BindProperty]
    public CompetitionCreateInputDTO Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];

    public List<SelectListItem> CompetitionTypeOptions { get; private set; } = [];

    public List<SelectListItem> CriteriaCountOptions { get; private set; } = [];

    public List<SelectListItem> JudgeCountOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedCompetitionId { get; private set; }

    public IReadOnlyList<short> ConfiguredCriteriaCountOptions => GetConfiguredShortOptions(
        "CompetitionRules:JudgingCriteriaCountOptions",
        [0, 1, 2, 3, 4],
        allowZero: true);

    public IReadOnlyList<short> ConfiguredJudgeCountOptions => GetConfiguredShortOptions(
        "CompetitionRules:JudgeCountOptions",
        [1, 2, 3],
        allowZero: false);

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

        await ValidateCompetitionAsync();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var now = DateTime.UtcNow;

        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = Input.CompetitionYearId!.Value,
            Code = Input.Code,
            NameEnglish = Input.NameEnglish,
            NameGurmukhi = Input.NameGurmukhi,
            CompetitionType = Input.CompetitionType,
            CriteriaCount = Input.CriteriaCount!.Value,
            MinJudges = Input.MinJudges!.Value,
            MaxJudges = Input.MaxJudges!.Value,
            Notes = Input.Notes,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.Competitions.Add(competition);

        await _auditLogService.LogAsync(
            entityName: "competitions",
            entityId: competition.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created competition.",
            oldValues: null,
            newValues: CreateCompetitionSnapshot(competition));

        CreatedCompetitionId = competition.Id;
        ResultIsSuccess = true;
        ResultTitle = "Competition created";
        ResultMessage = "The competition has been created successfully.";

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
        Input.Code = string.IsNullOrWhiteSpace(Input.Code)
            ? string.Empty
            : Input.Code.Trim().ToUpperInvariant().Replace(" ", "_");

        Input.NameEnglish = string.IsNullOrWhiteSpace(Input.NameEnglish)
            ? string.Empty
            : ToTitleCase(Input.NameEnglish.Trim());

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

    private async Task ValidateCompetitionAsync()
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
            ModelState.AddModelError(nameof(Input.CriteriaCount), "Number of Judging Criteria is required.");
        }
        else if (!ConfiguredCriteriaCountOptions.Contains(Input.CriteriaCount.Value))
        {
            ModelState.AddModelError(nameof(Input.CriteriaCount), "Selected Number of Judging Criteria is not valid.");
        }

        if (!Input.MinJudges.HasValue)
        {
            ModelState.AddModelError(nameof(Input.MinJudges), "Minimum judges is required.");
        }
        else if (!ConfiguredJudgeCountOptions.Contains(Input.MinJudges.Value))
        {
            ModelState.AddModelError(nameof(Input.MinJudges), "Selected minimum judges value is not valid.");
        }

        if (!Input.MaxJudges.HasValue)
        {
            ModelState.AddModelError(nameof(Input.MaxJudges), "Maximum judges is required.");
        }
        else if (!ConfiguredJudgeCountOptions.Contains(Input.MaxJudges.Value))
        {
            ModelState.AddModelError(nameof(Input.MaxJudges), "Selected maximum judges value is not valid.");
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
                Text = competitionYear.YearNo.ToString()
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

        CriteriaCountOptions = ConfiguredCriteriaCountOptions
            .Select(criteriaCount => new SelectListItem
            {
                Value = criteriaCount.ToString(),
                Text = criteriaCount == 0 ? "N/A" : criteriaCount.ToString()
            })
            .ToList();

        CriteriaCountOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select number of criteria..."
        });

        JudgeCountOptions = ConfiguredJudgeCountOptions
            .Select(judgeCount => new SelectListItem
            {
                Value = judgeCount.ToString(),
                Text = judgeCount.ToString()
            })
            .ToList();

        JudgeCountOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select..."
        });
    }

    private IReadOnlyList<short> GetConfiguredShortOptions(
        string key,
        IReadOnlyList<short> fallbackValues,
        bool allowZero)
    {
        var configuredValues = _configuration.GetSection(key).Get<short[]>();

        if (configuredValues is not { Length: > 0 })
        {
            return fallbackValues;
        }

        return configuredValues
            .Where(value => allowZero ? value >= 0 : value > 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedValue = string.Join(
            " ",
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalizedValue.ToLowerInvariant());
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Competition creation failed";
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
