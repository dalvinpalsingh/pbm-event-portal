using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class ParticipantDraftDetailsResultDTO : ResultDTO
{
    public ParticipantDraftDetailsDTO? Draft { get; set; }
}
