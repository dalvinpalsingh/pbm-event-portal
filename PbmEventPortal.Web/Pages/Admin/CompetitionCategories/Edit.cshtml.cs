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

public class EditModel : PageModel
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

    public EditModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    [BindProperty]
    public CompetitionCategoryEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> TeamSizeOptions { get; private set; } = [];

    public List<GenderRestrictionOption> TeamGenderRestrictionOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public short IndividualMaxWinnerCount => GetConfiguredShort("CompetitionCategoryRules:Individual:MaxWinnerCount", 5);

    public short TeamMaxWinnerCount => GetConfiguredShort("CompetitionCategoryRules:Team:MaxWinnerCount", 5);

    public short IndividualMaxParticipantsPerTeam => GetConfiguredShort("CompetitionCategoryRules:Individual:DefaultMaxParticipantsPerTeam", 2);

    public short TeamMaxEntriesPerTeam => GetConfiguredShort("CompetitionCategoryRules:Team:DefaultMaxEntriesPerTeam", 1);

    public IReadOnlyList<short> AllowedTeamSizes => GetAllowedTeamSizes();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var competitionCategory = await LoadCompetitionCategoryAsync(id, asNoTracking: true);

        if (competitionCategory == null)
        {
            return NotFound();
        }

        Input = MapToInput(competitionCategory);

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

        var competitionCategory = await LoadCompetitionCategoryAsync(id, asNoTracking: false);

        if (competitionCategory == null)
        {
            return NotFound();
        }

        PopulateReadOnlyInputFields(competitionCategory);

        if (Input.Id != id)
        {
            ModelState.AddModelError(string.Empty, "Invalid competition category request.");
        }

        NormalizeInput();

        ValidateCompetitionCategory(competitionCategory);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var oldSnapshot = CreateCompetitionCategorySnapshot(competitionCategory);
        var oldIsActive = competitionCategory.IsActive;

        var now = DateTime.UtcNow;
        var eventType = competitionCategory.EventType.Trim().ToUpperInvariant();

        competitionCategory.SortOrder = Input.SortOrder!.Value;
        competitionCategory.WinnerCount = Input.WinnerCount!.Value;
        competitionCategory.Notes = Input.Notes;
        competitionCategory.IsActive = Input.IsActive;
        competitionCategory.UpdatedAt = now;
        competitionCategory.UpdatedBy = actorId.Value;

        if (eventType == CompetitionEnums.EventTypeIndividual)
        {
            competitionCategory.IndividualEventMaxParticipants = Input.IndividualEventMaxParticipants;
            competitionCategory.TeamEventMaxEntriesPerTeam = null;
            competitionCategory.TeamEventTeamSize = null;
            competitionCategory.TeamEventGenderRestrictions = null;
            competitionCategory.TeamEventYoungerSubstitutionAllowed = null;
        }
        else if (eventType == CompetitionEnums.EventTypeTeam)
        {
            competitionCategory.IndividualEventMaxParticipants = null;
            competitionCategory.TeamEventMaxEntriesPerTeam = Input.TeamEventMaxEntriesPerTeam;
            competitionCategory.TeamEventTeamSize = Input.TeamEventTeamSize;
            competitionCategory.TeamEventGenderRestrictions = Input.TeamEventGenderRestrictions;
            competitionCategory.TeamEventYoungerSubstitutionAllowed = Input.TeamEventYoungerSubstitutionAllowed;
        }

        var newSnapshot = CreateCompetitionCategorySnapshot(competitionCategory);

        var actionName = oldIsActive != competitionCategory.IsActive
            ? competitionCategory.IsActive
                ? "ACTIVATE"
                : "DEACTIVATE"
            : "UPDATE";

        var changeReason = actionName switch
        {
            "ACTIVATE" => "System Admin reactivated competition category.",
            "DEACTIVATE" => "System Admin deactivated competition category.",
            _ => "System Admin updated competition category."
        };

        await _auditLogService.LogAsync(
            entityName: "competition_categories",
            entityId: competitionCategory.Id,
            actionName: actionName,
            changedBy: actorId.Value,
            changeReason: changeReason,
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        Input = MapToInput(competitionCategory);

        ResultIsSuccess = true;
        ResultTitle = actionName switch
        {
            "ACTIVATE" => "Competition category reactivated",
            "DEACTIVATE" => "Competition category deactivated",
            _ => "Competition category updated"
        };
        ResultMessage = actionName switch
        {
            "ACTIVATE" => $"{Input.CompetitionDisplay} - {Input.CategoryDisplay} has been reactivated successfully.",
            "DEACTIVATE" => $"{Input.CompetitionDisplay} - {Input.CategoryDisplay} has been deactivated successfully.",
            _ => $"{Input.CompetitionDisplay} - {Input.CategoryDisplay} has been updated successfully."
        };

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

    private async Task<CompetitionCategory?> LoadCompetitionCategoryAsync(Guid id, bool asNoTracking)
    {
        var query = _dbContext.CompetitionCategories
            .Include(competitionCategory => competitionCategory.Competition)
                .ThenInclude(competition => competition.CompetitionYear)
            .Include(competitionCategory => competitionCategory.CategoryCodeNavigation)
            .Where(competitionCategory => competitionCategory.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private static CompetitionCategoryEditInputDTO MapToInput(CompetitionCategory competitionCategory)
    {
        return new CompetitionCategoryEditInputDTO
        {
            Id = competitionCategory.Id,
            CompetitionId = competitionCategory.CompetitionId,
            CompetitionDisplay = FormatCompetitionDisplay(competitionCategory.Competition),
            CompetitionYearNo = competitionCategory.Competition.CompetitionYear.YearNo,
            EventType = competitionCategory.EventType,
            CategoryCode = competitionCategory.CategoryCode,
            CategoryDisplay = competitionCategory.CategoryCodeNavigation.Name,
            GroupingType = competitionCategory.CategoryCodeNavigation.GroupingType,
            AgeRangeDisplay = FormatAgeRange(
                competitionCategory.CategoryCodeNavigation.MinAge,
                competitionCategory.CategoryCodeNavigation.MaxAge),
            SortOrder = competitionCategory.SortOrder,
            IndividualEventMaxParticipants = competitionCategory.IndividualEventMaxParticipants,
            TeamEventMaxEntriesPerTeam = competitionCategory.TeamEventMaxEntriesPerTeam,
            TeamEventTeamSize = competitionCategory.TeamEventTeamSize,
            TeamEventGenderRestrictions = competitionCategory.TeamEventGenderRestrictions,
            TeamEventYoungerSubstitutionAllowed = competitionCategory.TeamEventYoungerSubstitutionAllowed,
            WinnerCount = competitionCategory.WinnerCount,
            Notes = competitionCategory.Notes,
            IsActive = competitionCategory.IsActive
        };
    }

    private void PopulateReadOnlyInputFields(CompetitionCategory competitionCategory)
    {
        Input.CompetitionId = competitionCategory.CompetitionId;
        Input.CompetitionDisplay = FormatCompetitionDisplay(competitionCategory.Competition);
        Input.CompetitionYearNo = competitionCategory.Competition.CompetitionYear.YearNo;
        Input.EventType = competitionCategory.EventType;
        Input.CategoryCode = competitionCategory.CategoryCode;
        Input.CategoryDisplay = competitionCategory.CategoryCodeNavigation.Name;
        Input.GroupingType = competitionCategory.CategoryCodeNavigation.GroupingType;
        Input.AgeRangeDisplay = FormatAgeRange(
            competitionCategory.CategoryCodeNavigation.MinAge,
            competitionCategory.CategoryCodeNavigation.MaxAge);
    }

    private void NormalizeInput()
    {
        Input.TeamEventGenderRestrictions = string.IsNullOrWhiteSpace(Input.TeamEventGenderRestrictions)
            ? null
            : Input.TeamEventGenderRestrictions.Trim().ToUpperInvariant();

        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes)
            ? null
            : Input.Notes.Trim();
    }

    private void ValidateCompetitionCategory(CompetitionCategory competitionCategory)
    {
        var eventType = competitionCategory.EventType.Trim().ToUpperInvariant();

        if (eventType != CompetitionEnums.EventTypeIndividual && eventType != CompetitionEnums.EventTypeTeam)
        {
            ModelState.AddModelError(string.Empty, "This competition category has an unsupported event type.");
            return;
        }

        if (!Input.SortOrder.HasValue)
        {
            ModelState.AddModelError(nameof(Input.SortOrder), "Sort order is required.");
        }
        else if (Input.SortOrder.Value < 1)
        {
            ModelState.AddModelError(nameof(Input.SortOrder), "Sort order must be 1 or higher.");
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

        if (eventType == CompetitionEnums.EventTypeIndividual)
        {
            ValidateIndividualEventFields();
        }
        else if (eventType == CompetitionEnums.EventTypeTeam)
        {
            ValidateTeamEventFields();
        }
    }

    private void ValidateIndividualEventFields()
    {
        if (!Input.IndividualEventMaxParticipants.HasValue)
        {
            ModelState.AddModelError(nameof(Input.IndividualEventMaxParticipants), "Max participants per team is required for individual events.");
        }
        else if (Input.IndividualEventMaxParticipants.Value < 1 ||
                 Input.IndividualEventMaxParticipants.Value > IndividualMaxParticipantsPerTeam)
        {
            ModelState.AddModelError(
                nameof(Input.IndividualEventMaxParticipants),
                $"Max participants per team must be between 1 and {IndividualMaxParticipantsPerTeam}.");
        }
    }

    private void ValidateTeamEventFields()
    {
        if (!Input.TeamEventMaxEntriesPerTeam.HasValue)
        {
            ModelState.AddModelError(nameof(Input.TeamEventMaxEntriesPerTeam), "Max team entries per team is required for team events.");
        }
        else if (Input.TeamEventMaxEntriesPerTeam.Value < 1 ||
                 Input.TeamEventMaxEntriesPerTeam.Value > TeamMaxEntriesPerTeam)
        {
            ModelState.AddModelError(
                nameof(Input.TeamEventMaxEntriesPerTeam),
                $"Max team entries per team must be between 1 and {TeamMaxEntriesPerTeam}.");
        }

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

        await Task.CompletedTask;
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
        ResultTitle = "Competition category update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static string FormatCompetitionDisplay(Competition competition)
    {
        return string.IsNullOrWhiteSpace(competition.NameGurmukhi)
            ? competition.NameEnglish
            : $"{competition.NameEnglish} / {competition.NameGurmukhi}";
    }

    private static string FormatAgeRange(short? minAge, short? maxAge)
    {
        return minAge.HasValue && maxAge.HasValue
            ? $"{minAge} - {maxAge}"
            : "-";
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

    public record GenderRestrictionOption(string Code, string DisplayName);
}
