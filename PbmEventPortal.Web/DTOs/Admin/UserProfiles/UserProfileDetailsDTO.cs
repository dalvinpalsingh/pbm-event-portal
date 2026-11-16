namespace PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

public class UserProfileDetailsDTO : UserProfileBaseDTO
{
    public DateOnly? PassportExpiryDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public UserProfileAuditActorDTO? CreatedByUser { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public UserProfileAuditActorDTO? UpdatedByUser { get; set; }

    public List<UserProfileRoleAssignmentDTO> RoleAssignments { get; set; } = [];
}