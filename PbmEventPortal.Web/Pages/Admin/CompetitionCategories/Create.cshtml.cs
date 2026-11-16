using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionCategories;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionCategories;

public class CreateModel : PageModel
{
    private static readonly IReadOnlyList<GenderRestrictionOption> GenderRestrictionOptionsSource =
    [
        new GenderRestrictionOption(CompetitionEnums.GenderNoRestriction, "No restriction"),
        new GenderRestrictionOption(CompetitionEnums.GenderSameGenderOnly, "Same gender only"),
        new GenderRestrictionOption(CompetitionEnums.GenderBoysOnly, "Boys only"),
        new GenderRestrictionOption(CompetitionEnums.GenderGirlsOnly, "Girls only"),
        new GenderRestrictionOption(CompetitionEnums.GenderOneBoyOneGirl, "1 Boy + 1 Girl")
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
    public CompetitionCategoryCreateInputDTO Input { get; set; } = new();

    public List<CompetitionOptionViewModel> CompetitionOptions { get; private set; } = [];

    public List<CategoryOptionViewModel> CategoryOptions { get; private set; } = [];

    public List<ExistingCompetitionCategoryLinkViewModel> ExistingCompetitionCategoryLinks { get; private set; } = [];

    public List<SelectListItem> TeamSizeOptions { get; private set; } = [];

    public List<GenderRestrictionOption> TeamGenderRestrictionOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedCompetitionCategoryId { get; private set; }

    public string? CreatedCompetitionCategoryDisplayName { get; private set; }

    public short IndividualMaxWinnerCount => GetConfiguredShort("CompetitionCategoryRules:Individual:MaxWinnerCount", 5);

    public short TeamMaxWinnerCount => GetConfiguredShort("CompetitionCategoryRules:Team:MaxWinnerCount", 5);

    public short IndividualDefaultMaxParticipantsPerTeam => GetConfiguredShort("CompetitionCategoryRules:Individual:DefaultMaxParticipantsPerTeam", 2);

    public short TeamDefaultMaxEntriesPerTeam => GetConfiguredShort("CompetitionCategoryRules:Team:DefaultMaxEntriesPerTeam", 1);

    public IReadOnlyList<short> AllowedTeamSizes => GetAllowedTeamSizes();

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
        var categoryType = await LoadSelectedCategoryTypeAsync();

        ValidateCompetitionCategory(competition, categoryType);

        if (!ModelState.IsValid || competition == null || categoryType == null)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var now = DateTime.UtcNow;
        var eventType = competition.CompetitionType.Trim().ToUpperInvariant();
        var isIndividualEvent = eventType == CompetitionEnums.EventTypeIndividual;

        var competitionCategory = new CompetitionCategory
        {
            Id = Guid.NewGuid(),
            CompetitionId = competition.Id,
            CategoryCode = categoryType.Code,
            EventType = eventType,
            SortOrder = categoryType.SortOrder,
            IndividualEventMaxParticipants = isIndividualEvent
                ? IndividualDefaultMaxParticipantsPerTeam
                : null,
            TeamEventMaxEntriesPerTeam = isIndividualEvent
                ? null
                : TeamDefaultMaxEntriesPerTeam,
            TeamEventTeamSize = isIndividualEvent
                ? null
                : Input.TeamEventTeamSize,
            TeamEventGenderRestrictions = isIndividualEvent
                ? null
                : Input.TeamEventGenderRestrictions,
            TeamEventYoungerSubstitutionAllowed = isIndividualEvent
                ? null
                : Input.TeamEventYoungerSubstitutionAllowed,
            WinnerCount = Input.WinnerCount!.Value,
            Notes = Input.Notes,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.CompetitionCategories.Add(competitionCategory);

        await _auditLogService.LogAsync(
            entityName: "competition_categories",
            entityId: competitionCategory.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created competition category.",
            oldValues: null,
            newValues: CreateCompetitionCategorySnapshot(competitionCategory));

        CreatedCompetitionCategoryId = competitionCategory.Id;
        CreatedCompetitionCategoryDisplayName = $"{competition.NameEnglish} - {categoryType.Name}";
        ResultIsSuccess = true;
        ResultTitle = "Competition category created";
        ResultMessage = $"{CreatedCompetitionCategoryDisplayName} has been created successfully.";

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
        Input.CategoryCode = string.IsNullOrWhiteSpace(Input.CategoryCode)
            ? string.Empty
            : Input.CategoryCode.Trim().ToUpperInvariant();

        Input.TeamEventGenderRestrictions = string.IsNullOrWhiteSpace(Input.TeamEventGenderRestrictions)
            ? null
            : Input.TeamEventGenderRestrictions.Trim().ToUpperInvariant();

        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes)
            ? null
            : Input.Notes.Trim();
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

    private async Task<CategoryType?> LoadSelectedCategoryTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.CategoryCode))
        {
            return null;
        }

        return await _dbContext.CategoryTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(categoryType => categoryType.Code == Input.CategoryCode);
    }

    private void ValidateCompetitionCategory(Competition? competition, CategoryType? categoryType)
    {
        if (!Input.CompetitionId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionId), "Competition is required.");
        }
        else if (competition == null || !competition.IsActive)
        {
            ModelState.AddModelError(nameof(Input.CompetitionId), "Selected competition is not valid or inactive.");
        }

        if (string.IsNullOrWhiteSpace(Input.CategoryCode))
        {
            ModelState.AddModelError(nameof(Input.CategoryCode), "Category is required.");
        }
        else if (categoryType == null)
        {
            ModelState.AddModelError(nameof(Input.CategoryCode), "Selected category is not valid.");
        }

        var eventType = competition?.CompetitionType.Trim().ToUpperInvariant();

        if (eventType is not null && eventType != CompetitionEnums.EventTypeIndividual && eventType != CompetitionEnums.EventTypeTeam)
        {
            ModelState.AddModelError(nameof(Input.CompetitionId), "Selected competition has an unsupported event type.");
        }

        var maxWinnerCount = eventType == CompetitionEnums.EventTypeTeam
            ? TeamMaxWinnerCount
            : IndividualMaxWinnerCount;

        if (!Input.WinnerCount.HasValue)
        {
            ModelState.AddModelError(nameof(Input.WinnerCount), "Winner count is required.");
        }
        else if (Input.WinnerCount.Value < 1 || Input.WinnerCount.Value > maxWinnerCount)
        {
            ModelState.AddModelError(nameof(Input.WinnerCount), $"Winner count must be between 1 and {maxWinnerCount}.");
        }

        if (eventType == CompetitionEnums.EventTypeTeam)
        {
            ValidateTeamEventFields();
        }

        if (Input.CompetitionId.HasValue &&
            !string.IsNullOrWhiteSpace(Input.CategoryCode))
        {
            var categoryAlreadyLinked = _dbContext.CompetitionCategories
                .AsNoTracking()
                .Any(competitionCategory =>
                    competitionCategory.CompetitionId == Input.CompetitionId.Value &&
                    competitionCategory.CategoryCode == Input.CategoryCode);

            if (categoryAlreadyLinked)
            {
                ModelState.AddModelError(nameof(Input.CategoryCode), "This category is already configured for the selected competition.");
            }
        }
    }

    private void ValidateTeamEventFields()
    {
        if (!Input.TeamEventTeamSize.HasValue)
        {
            ModelState.AddModelError(nameof(Input.TeamEventTeamSize), "Team size is required for team events.");
        }
        else if (!AllowedTeamSizes.Contains(Input.TeamEventTeamSize.Value))
        {
            ModelState.AddModelError(nameof(Input.TeamEventTeamSize), "Selected team size is not valid.");
        }

        if (string.IsNullOrWhiteSpace(Input.TeamEventGenderRestrictions))
        {
            ModelState.AddModelError(nameof(Input.TeamEventGenderRestrictions), "Team gender restrictions is required for team events.");
        }
        else if (!GenderRestrictionOptionsSource.Any(option => option.Code == Input.TeamEventGenderRestrictions))
        {
            ModelState.AddModelError(nameof(Input.TeamEventGenderRestrictions), "Selected team gender restriction is not valid.");
        }

        if (Input.TeamEventGenderRestrictions == CompetitionEnums.GenderOneBoyOneGirl &&
            Input.TeamEventTeamSize != 2)
        {
            ModelState.AddModelError(nameof(Input.TeamEventGenderRestrictions), "1 Boy + 1 Girl is only valid when team size is 2.");
        }

        if (!Input.TeamEventYoungerSubstitutionAllowed.HasValue)
        {
            ModelState.AddModelError(nameof(Input.TeamEventYoungerSubstitutionAllowed), "Younger substitution allowed is required for team events.");
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        CompetitionOptions = await _dbContext.Competitions
            .AsNoTracking()
            .Where(competition => competition.IsActive)
            .OrderByDescending(competition => competition.CompetitionYear.YearNo)
            .ThenBy(competition => competition.NameEnglish)
            .Select(competition => new CompetitionOptionViewModel
            {
                Id = competition.Id,
                DisplayName = string.IsNullOrWhiteSpace(competition.NameGurmukhi)
                    ? competition.NameEnglish
                    : $"{competition.NameEnglish} / {competition.NameGurmukhi}",
                EventType = competition.CompetitionType,
                EventTypeDisplay = competition.CompetitionType == CompetitionEnums.CompetitionTypeIndividual
                    ? "Individual"
                    : competition.CompetitionType == CompetitionEnums.CompetitionTypeTeam
                        ? "Team"
                        : competition.CompetitionType
            })
            .ToListAsync();

        CategoryOptions = await _dbContext.CategoryTypes
            .AsNoTracking()
            .OrderBy(categoryType => categoryType.SortOrder)
            .ThenBy(categoryType => categoryType.Name)
            .Select(categoryType => new CategoryOptionViewModel
            {
                Code = categoryType.Code,
                DisplayName = categoryType.Name,
                SortOrder = categoryType.SortOrder
            })
            .ToListAsync();

        ExistingCompetitionCategoryLinks = await _dbContext.CompetitionCategories
            .AsNoTracking()
            .Select(competitionCategory => new ExistingCompetitionCategoryLinkViewModel
            {
                CompetitionId = competitionCategory.CompetitionId,
                CategoryCode = competitionCategory.CategoryCode
            })
            .ToListAsync();

        TeamSizeOptions = AllowedTeamSizes
            .Select(teamSize => new SelectListItem
            {
                Value = teamSize.ToString(),
                Text = teamSize.ToString()
            })
            .ToList();

        TeamSizeOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select team size..."
        });

        TeamGenderRestrictionOptions = GenderRestrictionOptionsSource.ToList();
    }

    private short GetConfiguredShort(string key, short fallbackValue)
    {
        var configuredValue = _configuration.GetValue<short?>(key);

        return configuredValue is > 0
            ? configuredValue.Value
            : fallbackValue;
    }

    private IReadOnlyList<short> GetAllowedTeamSizes()
    {
        var configuredTeamSizes = _configuration
            .GetSection("CompetitionCategoryRules:Team:AllowedTeamSizes")
            .Get<short[]>();

        if (configuredTeamSizes is not { Length: > 0 })
        {
            return [2, 3];
        }

        return configuredTeamSizes
            .Where(teamSize => teamSize > 0)
            .Distinct()
            .OrderBy(teamSize => teamSize)
            .ToList();
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Competition category creation failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreateCompetitionCategorySnapshot(CompetitionCategory competitionCategory)
    {
        return new
        {
            competitionCategory.Id,
            competitionCategory.CompetitionId,
            competitionCategory.CategoryCode,
            competitionCategory.EventType,
            competitionCategory.SortOrder,
            competitionCategory.IndividualEventMaxParticipants,
            competitionCategory.TeamEventMaxEntriesPerTeam,
            competitionCategory.TeamEventTeamSize,
            competitionCategory.TeamEventGenderRestrictions,
            competitionCategory.TeamEventYoungerSubstitutionAllowed,
            competitionCategory.WinnerCount,
            competitionCategory.Notes,
            competitionCategory.IsActive,
            competitionCategory.CreatedAt,
            competitionCategory.CreatedBy,
            competitionCategory.UpdatedAt,
            competitionCategory.UpdatedBy
        };
    }

    public class CompetitionOptionViewModel
    {
        public Guid Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string EventTypeDisplay { get; set; } = string.Empty;
    }

    public class CategoryOptionViewModel
    {
        public string Code { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public short SortOrder { get; set; }
    }

    public class ExistingCompetitionCategoryLinkViewModel
    {
        public Guid CompetitionId { get; set; }

        public string CategoryCode { get; set; } = string.Empty;
    }

    public record GenderRestrictionOption(string Code, string DisplayName);
}
