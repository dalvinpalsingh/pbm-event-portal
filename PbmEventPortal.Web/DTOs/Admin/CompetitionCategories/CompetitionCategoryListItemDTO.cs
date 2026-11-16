using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionCategories;

public class CompetitionCategoryListItemDTO
{
    public Guid Id { get; set; }

    public Guid CompetitionId { get; set; }

    public string CompetitionNameEnglish { get; set; } = string.Empty;

    public string? CompetitionNameGurmukhi { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CategoryCode { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string GroupingType { get; set; } = string.Empty;

    public short SortOrder { get; set; }

    public short? IndividualEventMaxParticipants { get; set; }

    public short? TeamEventMaxEntriesPerTeam { get; set; }

    public short? TeamEventTeamSize { get; set; }

    public string? TeamEventGenderRestrictions { get; set; }

    public bool? TeamEventYoungerSubstitutionAllowed { get; set; }

    public short WinnerCount { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string CompetitionDisplay => string.IsNullOrWhiteSpace(CompetitionNameGurmukhi)
        ? CompetitionNameEnglish
        : $"{CompetitionNameEnglish} / {CompetitionNameGurmukhi}";

    public string EventTypeDisplay => EventType == CompetitionEnums.EventTypeIndividual
        ? "Individual"
        : EventType == CompetitionEnums.EventTypeTeam
            ? "Team"
            : EventType;

    public string IndividualMaxParticipantsDisplay => IndividualEventMaxParticipants?.ToString() ?? "-";

    public string TeamMaxEntriesDisplay => TeamEventMaxEntriesPerTeam?.ToString() ?? "-";

    public string TeamSizeDisplay => TeamEventTeamSize?.ToString() ?? "-";

    public string TeamGenderRestrictionsDisplay => TeamEventGenderRestrictions switch
    {
        CompetitionEnums.GenderNoRestriction => "No restriction",
        CompetitionEnums.GenderSameGenderOnly => "Same gender only",
        CompetitionEnums.GenderBoysOnly => "Boys only",
        CompetitionEnums.GenderGirlsOnly => "Girls only",
        CompetitionEnums.GenderOneBoyOneGirl => "1 Boy + 1 Girl",
        null or "" => "-",
        _ => TeamEventGenderRestrictions
    };

    public string YoungerSubstitutionDisplay => TeamEventYoungerSubstitutionAllowed switch
    {
        true => "Yes",
        false => "No",
        _ => "-"
    };
}
