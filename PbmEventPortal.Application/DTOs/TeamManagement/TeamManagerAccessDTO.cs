using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamManagerAccessDTO
{
    public UserAccessDTO UserAccess { get; set; } = new UserAccessDTO();
    public bool HasTeamManagerRole { get; set; } = false;
    public bool HasAssignedTeam { get; set; } = false;
    public Guid? SelectedTeamId { get; set; }
    public ResultDTO ValidationResult { get; set; } = new ResultDTO();
}
