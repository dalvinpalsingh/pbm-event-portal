namespace PbmEventPortal.Web.ViewModels.Admin.Teams;

public class TeamDetailsDTO
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string NameEnglish { get; set; } = string.Empty;

    public string? NameGurmukhi { get; set; }

    public Guid StateId { get; set; }

    public string StateCode { get; set; } = string.Empty;

    public string StateName { get; set; } = string.Empty;

    public Guid RegionId { get; set; }

    public string RegionCode { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public TeamAuditActorDTO? CreatedByUser { get; set; }

    public TeamAuditActorDTO? UpdatedByUser { get; set; }

    public List<TeamManagerAssignmentDTO> TeamManagers { get; set; } = [];

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}