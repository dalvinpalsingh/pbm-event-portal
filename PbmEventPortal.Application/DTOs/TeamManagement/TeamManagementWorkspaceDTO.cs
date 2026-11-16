using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamManagementWorkspaceDTO
{
    public TeamSummaryDTO? TeamSummary { get; set; }
    public List<TeamManagerSummaryDTO> TeamManagers { get; set; } = [];
    public List<RegistrationStatusDTO> RegistrationStatuses { get; set; } = [];
    public List<TeamParticipantSummaryDTO> Participants { get; set; } = [];
    public List<TeamDraftSummaryDTO> Drafts { get; set; } = [];
    public ResultDTO ValidationResult { get; set; } = new ResultDTO();
}
