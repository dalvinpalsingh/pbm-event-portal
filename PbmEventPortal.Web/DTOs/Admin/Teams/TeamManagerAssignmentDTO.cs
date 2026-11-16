namespace PbmEventPortal.Web.ViewModels.Admin.Teams;

public class TeamManagerAssignmentDTO
{
    public Guid AssignmentId { get; set; }

    public Guid UserId { get; set; }

    public string FullNameAsPerId { get; set; } = string.Empty;

    public string? FullNameGurmukhi { get; set; }

    public string Email { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(FullNameGurmukhi)
        ? FullNameAsPerId
        : $"{FullNameAsPerId} / {FullNameGurmukhi}";

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string VerifiedDisplay => IsVerified ? "Yes" : "No";

    public string PrimaryDisplay => IsPrimary ? "Yes" : "No";
}