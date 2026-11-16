namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamCompetitionAssignmentOptionDTO
{
    public Guid CompetitionCategoryId { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryShortName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int TargetCount { get; set; }
    public int CurrentCount { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}
