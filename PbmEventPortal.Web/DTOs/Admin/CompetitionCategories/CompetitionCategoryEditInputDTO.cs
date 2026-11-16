using System.ComponentModel.DataAnnotations;
using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionCategories;

public class CompetitionCategoryEditInputDTO
{
    public Guid Id { get; set; }

    public Guid CompetitionId { get; set; }

    public string CompetitionDisplay { get; set; } = string.Empty;

    public int CompetitionYearNo { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventTypeDisplay => EventType == CompetitionEnums.EventTypeIndividual
        ? "Individual"
        : EventType == CompetitionEnums.EventTypeTeam
            ? "Team"
            : EventType;

    public string CategoryCode { get; set; } = string.Empty;

    public string CategoryDisplay { get; set; } = string.Empty;

    public string GroupingType { get; set; } = string.Empty;

    public string AgeRangeDisplay { get; set; } = "-";

    [Display(Name = "Sort order")]
    [Required(ErrorMessage = "Sort order is required.")]
    public short? SortOrder { get; set; }

    [Display(Name = "Max participants per team")]
    public short? IndividualEventMaxParticipants { get; set; }

    [Display(Name = "Max team entries per team")]
    public short? TeamEventMaxEntriesPerTeam { get; set; }

    [Display(Name = "Team size")]
    public short? TeamEventTeamSize { get; set; }

    [Display(Name = "Team gender restrictions")]
    public string? TeamEventGenderRestrictions { get; set; }

    [Display(Name = "Younger substitution allowed")]
    public bool? TeamEventYoungerSubstitutionAllowed { get; set; }

    [Display(Name = "Winner count")]
    [Required(ErrorMessage = "Winner count is required.")]
    public short? WinnerCount { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}
