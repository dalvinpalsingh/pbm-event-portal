using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.Competitions;

public class CompetitionEditInputDTO
{
    public Guid Id { get; set; }

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

    [Display(Name = "Criteria count")]
    [Required(ErrorMessage = "Criteria count is required.")]
    [Range(1, 20, ErrorMessage = "Criteria count must be between 1 and 20.")]
    public short? CriteriaCount { get; set; }

    [Display(Name = "Minimum judges")]
    [Required(ErrorMessage = "Minimum judges is required.")]
    [Range(1, 10, ErrorMessage = "Minimum judges must be between 1 and 10.")]
    public short? MinJudges { get; set; }

    [Display(Name = "Maximum judges")]
    [Required(ErrorMessage = "Maximum judges is required.")]
    [Range(1, 10, ErrorMessage = "Maximum judges must be between 1 and 10.")]
    public short? MaxJudges { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
