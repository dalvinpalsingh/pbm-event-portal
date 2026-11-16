using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.TieBreakerPolicies;

public class TieBreakerPolicyListItemDTO
{
    public Guid Id { get; set; }

    public int CompetitionYearNo { get; set; }

    public string PolicyName { get; set; } = string.Empty;

    public string AppliesTo { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<TieBreakerPolicyRuleListItemViewModel> Rules { get; set; } = [];

    public string StatusDisplay => IsActive
        ? "Active"
        : "Inactive";

    public string AppliesToDisplay => AppliesTo switch
    {
        ResultCalculationEnums.OverallTahapWinnerPolicyAppliesTo => "Overall Tahap Winner",
        "CUSTOM" => "Custom",
        _ => string.IsNullOrWhiteSpace(AppliesTo) ? "Not specified" : AppliesTo.Replace("_", " ")
    };
}

public class TieBreakerPolicyRuleListItemViewModel
{
    public short PriorityNo { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string SortDirection { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string RuleDisplay => string.IsNullOrWhiteSpace(RuleCode)
        ? "Unnamed rule"
        : RuleCode.Replace("_", " ");

    public string DirectionDisplay => SortDirection switch
    {
        "ASC" => "Ascending",
        "DESC" => "Descending",
        _ => string.IsNullOrWhiteSpace(SortDirection) ? "Not specified" : SortDirection
    };
}
