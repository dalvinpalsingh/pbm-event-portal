namespace PbmEventPortal.Application.Services.Security;

public class PasswordPolicyValidationResult
{
    public List<string> Errors { get; } = [];

    public bool IsValid => Errors.Count == 0;

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
        }
    }
}