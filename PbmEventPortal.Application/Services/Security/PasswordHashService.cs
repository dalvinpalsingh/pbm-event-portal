using Microsoft.AspNetCore.Identity;

namespace PbmEventPortal.Application.Services.Security;

public class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool VerifyPassword(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            new object(),
            passwordHash,
            providedPassword);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
