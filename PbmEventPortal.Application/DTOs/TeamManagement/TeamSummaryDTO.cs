namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamSummaryDTO
{
    public Guid TeamId { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string? TeamNameGurmukhi { get; set; } = string.Empty;
    public string StateName { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;

    public int ActiveParticipantCount { get; set; }
    public int ActiveRegistrationEntryCount { get; set; }
    public int ApprovedRegistrationEntryCount { get; set; }
    public int PendingRegistrationEntryCount { get; set; }
    public int RejectedRegistrationEntryCount { get; set; }
}
