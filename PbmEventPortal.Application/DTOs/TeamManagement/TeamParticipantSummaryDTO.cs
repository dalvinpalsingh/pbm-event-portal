using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamParticipantSummaryDTO : ResultDTO
{
    public Guid ParticipantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string FullNameGurmukhi { get; set; } = string.Empty;
    public string TahapCode { get; set; } = string.Empty;
    public string TahapName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string IdType { get; set; } = string.Empty;
    public string? NricNumber { get; set; }
    public string? PassportNumber { get; set; }
    public DateOnly? PassportExpiryDate { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string ParentGuardianName { get; set; } = string.Empty;
    public string ParentGuardianMobile { get; set; } = string.Empty;
    public List<ParticipantCompetitionSummaryDTO> RegisteredCompetitions { get; set; } = [];
}
