using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.TieBreakerPolicies;

public class TieBreakerPolicyEditInputDTO
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    [Display(Name = "Policy name")]
    [Required(ErrorMessage = "Policy name is required.")]
    public string PolicyName { get; set; } = string.Empty;

    [Display(Name = "Applies to")]
    [Required(ErrorMessage = "Applies to is required.")]
    public string AppliesTo { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public List<TieBreakerPolicyRuleEditInputViewModel> Rules { get; set; } = [];

    public string CompetitionYearDisplay => string.IsNullOrWhiteSpace(CompetitionYearDisplayName)
        ? CompetitionYearNo.ToString()
        : CompetitionYearDisplayName;
}

public class TieBreakerPolicyRuleEditInputViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Priority")]
    public short PriorityNo { get; set; }

    [Display(Name = "Rule")]
    [Required(ErrorMessage = "Rule is required.")]
    public string RuleCode { get; set; } = string.Empty;

    [Display(Name = "Sort direction")]
    [Required(ErrorMessage = "Sort direction is required.")]
    public string SortDirection { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}
