using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

public class CompetitionScoringCriterionListItemDTO
{
    public Guid CompetitionId { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionCode { get; set; } = string.Empty;

    public string CompetitionNameEnglish { get; set; } = string.Empty;

    public string? CompetitionNameGurmukhi { get; set; }

    public string CompetitionType { get; set; } = string.Empty;

    public List<CompetitionScoringCriterionListCriterionViewModel> Criteria { get; set; } = [];

    public string CompetitionDisplay => string.IsNullOrWhiteSpace(CompetitionNameGurmukhi)
        ? CompetitionNameEnglish
        : $"{CompetitionNameEnglish} / {CompetitionNameGurmukhi}";

    public string CompetitionTypeDisplay => CompetitionType == CompetitionEnums.CompetitionTypeIndividual
        ? "Individual"
        : CompetitionType == CompetitionEnums.CompetitionTypeTeam
            ? "Team"
            : CompetitionType;
}

public class CompetitionScoringCriterionListCriterionViewModel
{
    public Guid Id { get; set; }

    public short CriterionNo { get; set; }

    public string CriterionName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string CriterionNameDisplay => string.IsNullOrWhiteSpace(CriterionName)
        ? $"Criterion {CriterionNo}"
        : CriterionName;
}
