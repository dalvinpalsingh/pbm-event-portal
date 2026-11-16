namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class CompetitionAssignmentCategoryDTO
{
    public Guid Id { get; set; }
    public string CompetitionCode { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public short SortOrder { get; set; }
    public short? IndividualEventMaxParticipants { get; set; }
    public short? TeamEventTeamSize { get; set; }
}
