namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class ParticipantDraftDetailsDTO
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string TahapCode { get; set; } = string.Empty;
    public string TahapName { get; set; } = string.Empty;
    public string DraftStatus { get; set; } = string.Empty;
    public string DraftPayloadJson { get; set; } = string.Empty;
    public List<Guid> IndividualCompetitionCategoryIds { get; set; } = [];
    public List<Guid> TeamCompetitionCategoryIds { get; set; } = [];
    public DateTime LastSavedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
