namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class ParticipantDraftDeleteRequest
{
    public Guid? DraftId { get; set; }
    public Guid? TeamId { get; set; }
}
