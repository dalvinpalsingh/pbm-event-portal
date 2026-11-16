namespace PbmEventPortal.Application.Services.Security;

public class PasswordPolicyValidationRequest
{
    public string NewPassword { get; set; } = string.Empty;

    public string? CurrentPassword { get; set; }

    public string? Email { get; set; }

    public string? MobileNumber { get; set; }

    public string? FullNameAsPerId { get; set; }
}