namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class ParticipantDraftSaveRequest
{
    public Guid? DraftId { get; set; }
    public Guid? TeamId { get; set; }
    public string TahapCode { get; set; } = string.Empty;
    public string FullNameAsPerId { get; set; } = string.Empty;
    public string FullNameGurmukhi { get; set; } = string.Empty;
    public string IdType { get; set; } = string.Empty;
    public string? NricNumber { get; set; }
    public string? PassportNumber { get; set; }
    public DateOnly? PassportExpiryDate { get; set; }
    public string? MobileCountryCode { get; set; }
    public string? MobileLocalNumber { get; set; }
    public string? Email { get; set; }
    public string ParentGuardianName { get; set; } = string.Empty;
    public string? ParentGuardianMobileCountryCode { get; set; }
    public string? ParentGuardianMobileLocalNumber { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public List<Guid> IndividualCompetitionCategoryIds { get; set; } = [];
    public List<Guid> TeamCompetitionCategoryIds { get; set; } = [];
}
