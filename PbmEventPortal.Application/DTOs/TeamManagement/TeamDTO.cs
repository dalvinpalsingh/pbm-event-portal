namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamDTO
{
    public Guid TeamId { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
}
