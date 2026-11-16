using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class ParticipantDraftSaveResultDTO : ResultDTO
{
    public Guid? DraftId { get; set; }
}
