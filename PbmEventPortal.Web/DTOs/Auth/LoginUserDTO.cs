namespace PbmEventPortal.Web.ViewModels.Auth;

public class LoginUserDTO
{
    public Guid Id { get; set; }

    public string FullNameAsPerId { get; set; } = string.Empty;

    public string FullNameGurmukhi { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}