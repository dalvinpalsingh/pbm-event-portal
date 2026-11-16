namespace PbmEventPortal.Application.Services.Security;

public interface IPasswordPolicyService
{
    Task<PasswordPolicyValidationResult> ValidateAsync(
        PasswordPolicyValidationRequest request,
        CancellationToken cancellationToken = default);
}