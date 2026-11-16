using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

public class CompetitionScoringCriterionCreateInputDTO
{
    [Display(Name = "Competition")]
    [Required(ErrorMessage = "Competition is required.")]
    public Guid? CompetitionId { get; set; }

    public List<CompetitionScoringCriterionCreateRowInputViewModel> Criteria { get; set; } = [];
}

public class CompetitionScoringCriterionCreateRowInputViewModel
{
    [Display(Name = "Criterion no.")]
    [Required(ErrorMessage = "Criterion no. is required.")]
    public short? CriterionNo { get; set; }

    [Display(Name = "Criterion name")]
    public string CriterionName { get; set; } = string.Empty;

    [Display(Name = "Max score")]
    public decimal? MaxScore { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
