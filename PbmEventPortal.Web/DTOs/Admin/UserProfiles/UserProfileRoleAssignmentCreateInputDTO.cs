using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

public class UserProfileRoleAssignmentCreateInputDTO
{
    [Required]
    [Display(Name = "Role")]
    public string RoleCode { get; set; } = string.Empty;

    [Display(Name = "Verified")]
    public bool IsVerified { get; set; } = true;

    [Display(Name = "Primary role")]
    public bool IsPrimary { get; set; }
}