using System.ComponentModel.DataAnnotations;
using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

public class CompetitionScoringCriteriaEditInputDTO
{
    public Guid SourceScoringCriterionId { get; set; }

    public Guid CompetitionId { get; set; }

    public string CompetitionDisplay { get; set; } = string.Empty;

    public int CompetitionYearNo { get; set; }

    public string CompetitionCode { get; set; } = string.Empty;

    public string CompetitionType { get; set; } = string.Empty;

    public short CompetitionCriteriaCount { get; set; }

    public List<CompetitionScoringCriteriaEditRowInputViewModel> Criteria { get; set; } = [];

    public string CompetitionTypeDisplay => CompetitionType == CompetitionEnums.CompetitionTypeIndividual
        ? "Individual"
        : CompetitionType == CompetitionEnums.CompetitionTypeTeam
            ? "Team"
            : CompetitionType;
}

public class CompetitionScoringCriteriaEditRowInputViewModel
{
    [Required]
    public Guid Id { get; set; }

    [Display(Name = "Criterion no.")]
    public short CriterionNo { get; set; }

    [Display(Name = "Criterion name")]
    public string CriterionName { get; set; } = string.Empty;

    [Display(Name = "Max score")]
    public decimal? MaxScore { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
