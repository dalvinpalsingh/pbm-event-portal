using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamParticipantSubmitResultDTO : ResultDTO
{
    public Guid? ParticipantId { get; set; }
}
