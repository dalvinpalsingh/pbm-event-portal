using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.Competitions;

public class CompetitionCreateInputDTO
{
    [Display(Name = "Competition year")]
    [Required(ErrorMessage = "Competition year is required.")]
    public Guid? CompetitionYearId { get; set; }

    [Display(Name = "Competition code")]
    [Required(ErrorMessage = "Competition code is required.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "English name")]
    [Required(ErrorMessage = "English name is required.")]
    public string NameEnglish { get; set; } = string.Empty;

    [Display(Name = "Gurmukhi name")]
    public string? NameGurmukhi { get; set; }

    [Display(Name = "Competition type")]
    [Required(ErrorMessage = "Competition type is required.")]
    public string CompetitionType { get; set; } = string.Empty;

    [Display(Name = "Number of Judging Criteria")]
    [Required(ErrorMessage = "Number of Judging Criteria is required.")]
    public short? CriteriaCount { get; set; }

    [Display(Name = "Minimum judges")]
    [Required(ErrorMessage = "Minimum judges is required.")]
    public short? MinJudges { get; set; } = 2;

    [Display(Name = "Maximum judges")]
    [Required(ErrorMessage = "Maximum judges is required.")]
    public short? MaxJudges { get; set; } = 3;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
