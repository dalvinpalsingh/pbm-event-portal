namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class DraftApplicationCompetitionDTO
{
    public Guid DraftId { get; set; }
    public Guid CompetitionCategoryId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string CompetitionName { get; set; } = string.Empty;
    public string EntryStatus { get; set; } = string.Empty;
}
