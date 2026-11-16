namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class ParticipantCompetitionSummaryDTO
{
    public Guid ParticipantId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string EntryStatus { get; set; } = string.Empty;
}
