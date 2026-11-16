using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionCategories;

public class CompetitionCategoryCreateInputDTO
{
    [Display(Name = "Competition")]
    [Required(ErrorMessage = "Competition is required.")]
    public Guid? CompetitionId { get; set; }

    [Display(Name = "Category")]
    [Required(ErrorMessage = "Category is required.")]
    public string CategoryCode { get; set; } = string.Empty;

    [Display(Name = "Winner count")]
    [Required(ErrorMessage = "Winner count is required.")]
    public short? WinnerCount { get; set; }

    [Display(Name = "Team size")]
    public short? TeamEventTeamSize { get; set; }

    [Display(Name = "Team gender restrictions")]
    public string? TeamEventGenderRestrictions { get; set; }

    [Display(Name = "Younger substitution allowed")]
    public bool? TeamEventYoungerSubstitutionAllowed { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
