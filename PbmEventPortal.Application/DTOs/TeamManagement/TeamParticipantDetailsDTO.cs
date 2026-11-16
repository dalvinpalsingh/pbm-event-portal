namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamParticipantDetailsDTO : TeamParticipantSummaryDTO
{
    public Guid TeamId { get; set; }
    public string MobileCountryCode { get; set; } = "+60";
    public string? MobileLocalNumber { get; set; }
    public string ParentGuardianMobileCountryCode { get; set; } = "+60";
    public string? ParentGuardianMobileLocalNumber { get; set; }
    public List<Guid> IndividualCompetitionCategoryIds { get; set; } = [];
    public List<Guid> TeamCompetitionCategoryIds { get; set; } = [];
}
