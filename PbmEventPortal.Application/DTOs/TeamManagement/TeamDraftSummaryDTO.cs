namespace PbmEventPortal.Application.DTOs.TeamManagement;

public class TeamDraftSummaryDTO
{
    public Guid DraftId { get; set; }
    public Guid TeamId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string FullNameGurmukhi { get; set; } = string.Empty;
    public string TahapCode { get; set; } = string.Empty;
    public string TahapName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }

    public string IdType { get; set; } = string.Empty;
    public string? IdNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }

    public string ParentGuardianName { get; set; } = string.Empty;
    public string ParentGuardianMobile { get; set; } = string.Empty;

    public DateTime LastSavedAt { get; set; }

    public List<Guid> IndividualCompetitionCategoryIds { get; set; } = [];
    public List<Guid> TeamCompetitionCategoryIds { get; set; } = [];

    public List<DraftApplicationCompetitionDTO> RegisteredCompetitions { get; set; } = [];
}
