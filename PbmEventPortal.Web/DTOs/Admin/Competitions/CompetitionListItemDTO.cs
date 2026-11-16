namespace PbmEventPortal.Web.ViewModels.Admin.Competitions;

public class CompetitionListItemDTO
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string NameEnglish { get; set; } = string.Empty;

    public string? NameGurmukhi { get; set; }

    public string CompetitionType { get; set; } = string.Empty;

    public short CriteriaCount { get; set; }

    public short MinJudges { get; set; }

    public short MaxJudges { get; set; }

    public bool IsActive { get; set; }

    public int CategoryCount { get; set; }

    public int ScoringCriteriaCount { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string CompetitionYearDisplay => $"{CompetitionYearNo} - {CompetitionYearDisplayName}";

    public string JudgeRangeDisplay => MinJudges == MaxJudges
        ? MinJudges.ToString()
        : $"{MinJudges} - {MaxJudges}";
}
