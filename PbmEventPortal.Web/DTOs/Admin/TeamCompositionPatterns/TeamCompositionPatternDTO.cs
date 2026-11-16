using Microsoft.AspNetCore.Mvc.Rendering;

namespace PbmEventPortal.Web.ViewModels.Admin.TeamCompositionPatterns;

public class TeamCompositionPatternListItemViewModel
{
    public Guid Id { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string PatternCode { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public string SlotSummary { get; set; } = string.Empty;
    public bool AllowYoungerSubstitution { get; set; }
    public bool IsActive { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
    public string YoungerSubstitutionDisplay => AllowYoungerSubstitution ? "Allowed" : "Disabled";
}

public class TeamCompositionPatternDetailsViewModel
{
    public Guid Id { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string PatternCode { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public List<TeamCompositionPatternSlotViewModel> Slots { get; set; } = [];
    public bool AllowYoungerSubstitution { get; set; }
    public string NotesDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByDisplay { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedByDisplay { get; set; } = string.Empty;

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
    public string YoungerSubstitutionDisplay => AllowYoungerSubstitution ? "Allowed" : "Disabled";
}

public class TeamCompositionPatternSlotViewModel
{
    public Guid Id { get; set; }
    public short SlotNo { get; set; }
    public string TahapCode { get; set; } = string.Empty;
    public string TahapDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}

public class TeamCompositionPatternCreateInputViewModel
{
    public Guid? CompetitionCategoryId { get; set; }
    public string PatternCode { get; set; } = string.Empty;
    public List<string?> SlotTahapCodes { get; set; } = [];
    public bool AllowYoungerSubstitution { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TeamCompositionPatternEditInputViewModel : TeamCompositionPatternCreateInputViewModel
{
    public Guid Id { get; set; }
}

public class TeamCompositionPatternLookupOptionsViewModel
{
    public List<SelectListItem> CompetitionCategoryOptions { get; set; } = [];
    public List<SelectListItem> TahapOptions { get; set; } = [];
    public List<TeamCompositionPatternCategoryOptionViewModel> CategoryMetadata { get; set; } = [];
    public int MaxTeamSize { get; set; }
}

public class TeamCompositionPatternCategoryOptionViewModel
{
    public Guid Id { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public bool AllowYoungerSubstitution { get; set; }
}
