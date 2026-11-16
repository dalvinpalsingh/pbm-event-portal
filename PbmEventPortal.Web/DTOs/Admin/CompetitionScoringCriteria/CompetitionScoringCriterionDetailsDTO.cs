using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

public class CompetitionScoringCriterionDetailsViewModel
{
    public Guid CompetitionId { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionCode { get; set; } = string.Empty;

    public string CompetitionNameEnglish { get; set; } = string.Empty;

    public string? CompetitionNameGurmukhi { get; set; }

    public string CompetitionType { get; set; } = string.Empty;

    public short CompetitionCriteriaCount { get; set; }

    public List<CompetitionScoringCriterionDetailsRowViewModel> Criteria { get; set; } = [];

    public string CompetitionDisplay => string.IsNullOrWhiteSpace(CompetitionNameGurmukhi)
        ? CompetitionNameEnglish
        : $"{CompetitionNameEnglish} / {CompetitionNameGurmukhi}";

    public string CompetitionTypeDisplay => CompetitionType == CompetitionEnums.CompetitionTypeIndividual
        ? "Individual"
        : CompetitionType == CompetitionEnums.CompetitionTypeTeam
            ? "Team"
            : CompetitionType;
}

public class CompetitionScoringCriterionDetailsRowViewModel
{
    public Guid Id { get; set; }

    public short CriterionNo { get; set; }

    public string CriterionName { get; set; } = string.Empty;

    public decimal? MaxScore { get; set; }

    public short SortOrder { get; set; }

    public bool IsActive { get; set; }

    public string CriterionNameDisplay => string.IsNullOrWhiteSpace(CriterionName)
        ? "-"
        : CriterionName;

    public string MaxScoreDisplay => MaxScore.HasValue
        ? MaxScore.Value.ToString("0.##")
        : "-";

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}
