using System.ComponentModel.DataAnnotations;
using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.TieBreakerPolicies;

public class TieBreakerPolicyCreateInputDTO
{
    [Display(Name = "Competition year")]
    [Required(ErrorMessage = "Competition year is required.")]
    public Guid? CompetitionYearId { get; set; }

    [Display(Name = "Policy name")]
    [Required(ErrorMessage = "Policy name is required.")]
    public string PolicyName { get; set; } = string.Empty;

    [Display(Name = "Applies to")]
    [Required(ErrorMessage = "Applies to is required.")]
    public string AppliesTo { get; set; } = ResultCalculationEnums.OverallTahapWinnerPolicyAppliesTo;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Rule count")]
    [Required(ErrorMessage = "Rule count is required.")]
    public short? RuleCount { get; set; } = 1;

    public List<TieBreakerPolicyRuleCreateInputViewModel> Rules { get; set; } = [];
}

public class TieBreakerPolicyRuleCreateInputViewModel
{
    [Display(Name = "Priority")]
    [Required(ErrorMessage = "Priority is required.")]
    public short? PriorityNo { get; set; }

    [Display(Name = "Rule")]
    [Required(ErrorMessage = "Rule is required.")]
    public string RuleCode { get; set; } = string.Empty;

    [Display(Name = "Sort direction")]
    [Required(ErrorMessage = "Sort direction is required.")]
    public string SortDirection { get; set; } = "DESC";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
