namespace PbmEventPortal.Application.DTOs.Common;

public class UserAccessDTO
{
    public bool IsUserAuthenticated { get; set; } = false;
    public Guid? UserId { get; set; }
    public string? UserFullNameAsPerId { get; set; }
    public string? UserFullNameGurmukhi { get; set; }
    public string? UserEmail { get; set; }
    public List<string> UserRoles { get; set; } = [];
    public bool UserHasAnyAdminRole { get; set; } = false;
}
