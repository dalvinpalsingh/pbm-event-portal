using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.TeamCompositionPatterns;

namespace PbmEventPortal.Web.Pages.Admin.TeamCompositionPatterns;

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public TeamCompositionPatternCreateInputViewModel Input { get; set; } = new();

    public TeamCompositionPatternLookupOptionsViewModel LookupOptions { get; private set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedPatternId { get; private set; }

    public async Task OnGetAsync()
    {
        Input = new TeamCompositionPatternCreateInputViewModel { IsActive = true };
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
        var selectedCategory = await LoadSelectedCategoryAsync();
        ApplyCategoryDerivedValues(selectedCategory);
        await ValidatePatternAsync(selectedCategory);
        await LoadLookupOptionsAsync();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var now = DateTime.UtcNow;
        var pattern = new TeamCompositionPattern
        {
            Id = Guid.NewGuid(),
            CompetitionCategoryId = selectedCategory!.Id,
            PatternCode = Input.PatternCode,
            AllowYoungerSubstitution = Input.AllowYoungerSubstitution,
            Notes = Input.Notes,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        foreach (var slot in Input.SlotTahapCodes.Select((tahapCode, index) => new { TahapCode = tahapCode, SlotNo = (short)(index + 1) }))
        {
            pattern.TeamCompositionPatternSlots.Add(new TeamCompositionPatternSlot
            {
                Id = Guid.NewGuid(),
                TeamCompositionPatternId = pattern.Id,
                SlotNo = slot.SlotNo,
                TahapCode = slot.TahapCode!,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = actorId.Value,
                UpdatedAt = null,
                UpdatedBy = null
            });
        }

        _dbContext.TeamCompositionPatterns.Add(pattern);

        await _auditLogService.LogAsync(
            entityName: "team_composition_patterns",
            entityId: pattern.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created team composition pattern.",
            oldValues: null,
            newValues: CreatePatternSnapshot(pattern));

        CreatedPatternId = pattern.Id;
        ResultIsSuccess = true;
        ResultTitle = "Team composition pattern created";
        ResultMessage = $"{pattern.PatternCode} has been created successfully.";
        Input = new TeamCompositionPatternCreateInputViewModel { IsActive = true };

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
        Input.PatternCode = string.Empty;
        Input.SlotTahapCodes = Input.SlotTahapCodes
            .Select(tahapCode => string.IsNullOrWhiteSpace(tahapCode) ? null : tahapCode.Trim().ToUpperInvariant())
            .ToList();

        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes)
            ? null
            : Input.Notes.Trim();
    }

    private async Task<CompetitionCategory?> LoadSelectedCategoryAsync()
    {
        if (!Input.CompetitionCategoryId.HasValue)
        {
            return null;
        }

        return await _dbContext.CompetitionCategories
            .AsNoTracking()
            .Include(category => category.Competition)
            .ThenInclude(competition => competition.CompetitionYear)
            .FirstOrDefaultAsync(category => category.Id == Input.CompetitionCategoryId.Value);
    }

    private void ApplyCategoryDerivedValues(CompetitionCategory? category)
    {
        if (category == null)
        {
            Input.AllowYoungerSubstitution = false;
            return;
        }

        Input.AllowYoungerSubstitution = category.TeamEventYoungerSubstitutionAllowed == true;
        Input.PatternCode = GeneratePatternCode(category, Input.SlotTahapCodes, Input.AllowYoungerSubstitution);
    }

    private async Task ValidatePatternAsync(CompetitionCategory? selectedCategory)
    {
        if (!Input.CompetitionCategoryId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Competition category is required.");
            return;
        }

        if (selectedCategory == null ||
            !selectedCategory.IsActive ||
            selectedCategory.EventType != RegistrationEnums.EntryTypeTeam ||
            !selectedCategory.Competition.IsActive ||
            !selectedCategory.Competition.CompetitionYear.IsActive ||
            !selectedCategory.TeamEventTeamSize.HasValue ||
            selectedCategory.TeamEventTeamSize.Value < 1)
        {
            ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category is not valid, inactive, or not a team category with a valid team size.");
            return;
        }

        var requiredSlotCount = selectedCategory.TeamEventTeamSize.Value;

        if (Input.SlotTahapCodes.Count < requiredSlotCount)
        {
            while (Input.SlotTahapCodes.Count < requiredSlotCount)
            {
                Input.SlotTahapCodes.Add(null);
            }
        }

        Input.SlotTahapCodes = Input.SlotTahapCodes.Take(requiredSlotCount).ToList();

        for (var slotIndex = 0; slotIndex < requiredSlotCount; slotIndex++)
        {
            await ValidateTahapCodeAsync(Input.SlotTahapCodes[slotIndex], $"Input.SlotTahapCodes[{slotIndex}]", $"Slot {slotIndex + 1} Tahap is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.PatternCode))
        {
            ModelState.AddModelError(nameof(Input.PatternCode), "Pattern code could not be generated. Please check the selected category and slots.");
        }
        else if (Input.PatternCode.Length > 200)
        {
            ModelState.AddModelError(nameof(Input.PatternCode), "Generated pattern code must not be more than 200 characters.");
        }

        if (!string.IsNullOrWhiteSpace(Input.PatternCode))
        {
            var duplicateExists = await _dbContext.TeamCompositionPatterns
                .AsNoTracking()
                .AnyAsync(pattern =>
                    pattern.CompetitionCategoryId == selectedCategory.Id &&
                    pattern.PatternCode == Input.PatternCode);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(Input.PatternCode), "A pattern with this generated code already exists for the selected competition category.");
            }
        }
    }

    private async Task ValidateTahapCodeAsync(string? tahapCode, string modelStateKey, string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(tahapCode))
        {
            ModelState.AddModelError(modelStateKey, requiredMessage);
            return;
        }

        var exists = await _dbContext.CategoryTypes
            .AsNoTracking()
            .AnyAsync(categoryType =>
                categoryType.Code == tahapCode &&
                categoryType.Code.StartsWith("TAHAP_"));

        if (!exists)
        {
            ModelState.AddModelError(modelStateKey, "Selected Tahap is not valid.");
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        var categoryMetadata = await _dbContext.CompetitionCategories
            .AsNoTracking()
            .Where(category =>
                category.IsActive &&
                category.EventType == RegistrationEnums.EntryTypeTeam &&
                category.TeamEventTeamSize.HasValue &&
                category.TeamEventTeamSize.Value > 0 &&
                category.Competition.IsActive &&
                category.Competition.CompetitionYear.IsActive)
            .OrderByDescending(category => category.Competition.CompetitionYear.YearNo)
            .ThenBy(category => category.Competition.Code)
            .ThenBy(category => category.SortOrder)
            .Select(category => new TeamCompositionPatternCategoryOptionViewModel
            {
                Id = category.Id,
                CompetitionCode = category.Competition.Code,
                CategoryCode = category.CategoryCode,
                TeamSize = category.TeamEventTeamSize ?? 0,
                AllowYoungerSubstitution = category.TeamEventYoungerSubstitutionAllowed == true
            })
            .ToListAsync();

        LookupOptions = new TeamCompositionPatternLookupOptionsViewModel
        {
            CategoryMetadata = categoryMetadata,
            CompetitionCategoryOptions = await _dbContext.CompetitionCategories
                .AsNoTracking()
                .Where(category =>
                    category.IsActive &&
                    category.EventType == RegistrationEnums.EntryTypeTeam &&
                    category.TeamEventTeamSize.HasValue &&
                    category.TeamEventTeamSize.Value > 0 &&
                    category.Competition.IsActive &&
                    category.Competition.CompetitionYear.IsActive)
                .OrderByDescending(category => category.Competition.CompetitionYear.YearNo)
                .ThenBy(category => category.Competition.Code)
                .ThenBy(category => category.SortOrder)
                .Select(category => new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.Competition.CompetitionYear.DisplayName + " / " + category.Competition.Code + " - " + category.Competition.NameEnglish + " / " + category.CategoryCode + " / Team size " + category.TeamEventTeamSize
                })
                .ToListAsync(),
            TahapOptions = await BuildTahapOptionsAsync(),
            MaxTeamSize = categoryMetadata.Count == 0 ? 0 : categoryMetadata.Max(category => category.TeamSize)
        };

        LookupOptions.CompetitionCategoryOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition category..."
        });
    }

    private Task<List<SelectListItem>> BuildTahapOptionsAsync()
    {
        return _dbContext.CategoryTypes
            .AsNoTracking()
            .Where(categoryType => categoryType.Code.StartsWith("TAHAP_"))
            .OrderBy(categoryType => categoryType.SortOrder)
            .Select(categoryType => new SelectListItem
            {
                Value = categoryType.Code,
                Text = categoryType.Name
            })
            .ToListAsync();
    }

    private static string GeneratePatternCode(CompetitionCategory category, IReadOnlyList<string?> slotTahapCodes, bool allowYoungerSubstitution)
    {
        var slotPart = string.Join("_", slotTahapCodes
            .Where(tahapCode => !string.IsNullOrWhiteSpace(tahapCode))
            .Select(ToPatternTahapPart)
            .OrderBy(GetPatternTahapSortOrder)
            .ThenBy(tahapPart => tahapPart));

        if (string.IsNullOrWhiteSpace(slotPart))
        {
            return string.Empty;
        }

        var substitutionPart = allowYoungerSubstitution ? "YS_ALLOWED" : "YS_DISABLED";

        return $"{ToPatternCodePart(category.Competition.Code)}_{ToPatternCodePart(category.CategoryCode)}_{slotPart}_{substitutionPart}";
    }

    private static string ToPatternTahapPart(string? tahapCode)
    {
        if (string.IsNullOrWhiteSpace(tahapCode))
        {
            return string.Empty;
        }

        return tahapCode.Trim().ToUpperInvariant().Replace("TAHAP_", "T");
    }

    private static int GetPatternTahapSortOrder(string tahapPart)
    {
        return int.TryParse(tahapPart.TrimStart('T'), out var tahapNo)
            ? tahapNo
            : int.MaxValue;
    }

    private static string ToPatternCodePart(string value)
    {
        var normalized = Regex.Replace(value.Trim().ToUpperInvariant(), "[^A-Z0-9]+", "_");
        return Regex.Replace(normalized, "_+", "_").Trim('_');
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Team composition pattern creation failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreatePatternSnapshot(TeamCompositionPattern pattern)
    {
        return new
        {
            pattern.Id,
            pattern.CompetitionCategoryId,
            pattern.PatternCode,
            pattern.AllowYoungerSubstitution,
            pattern.Notes,
            pattern.IsActive,
            pattern.CreatedAt,
            pattern.CreatedBy,
            pattern.UpdatedAt,
            pattern.UpdatedBy,
            Slots = pattern.TeamCompositionPatternSlots
                .OrderBy(slot => slot.SlotNo)
                .Select(slot => new
                {
                    slot.Id,
                    slot.TeamCompositionPatternId,
                    slot.SlotNo,
                    slot.TahapCode,
                    slot.IsActive,
                    slot.CreatedAt,
                    slot.CreatedBy,
                    slot.UpdatedAt,
                    slot.UpdatedBy
                })
                .ToList()
        };
    }
}
