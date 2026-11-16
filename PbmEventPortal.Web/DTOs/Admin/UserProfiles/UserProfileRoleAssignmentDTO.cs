namespace PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

public class UserProfileRoleAssignmentDTO
{
    public Guid Id { get; set; }

    public string RoleCode { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string RoleScope { get; set; } = string.Empty;

    public string? RoleDescription { get; set; }

    public Guid? TeamId { get; set; }

    public string? TeamName { get; set; }

    public bool IsVerified { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }

    public string TeamDisplay => string.IsNullOrWhiteSpace(TeamName)
        ? "-"
        : TeamName;

    public string VerifiedDisplay => IsVerified ? "Yes" : "No";

    public string PrimaryDisplay => IsPrimary ? "Yes" : "No";

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}