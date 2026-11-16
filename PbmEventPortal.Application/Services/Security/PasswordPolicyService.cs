using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;

namespace PbmEventPortal.Application.Services.Security;

public class PasswordPolicyService : IPasswordPolicyService
{
    private const int MinimumPasswordLength = 8;
    private const int MaximumPasswordLength = 64;

    private readonly PbmDbContext _dbContext;

    public PasswordPolicyService(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PasswordPolicyValidationResult> ValidateAsync(
        PasswordPolicyValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new PasswordPolicyValidationResult();

        if (request == null)
        {
            result.AddError("Password validation request is required.");
            return result;
        }

        var password = request.NewPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(password))
        {
            result.AddError("New password is required.");
            return result;
        }

        if (password.Length < MinimumPasswordLength)
        {
            result.AddError($"New password must be at least {MinimumPasswordLength} characters long.");
        }

        if (password.Length > MaximumPasswordLength)
        {
            result.AddError($"New password must not be more than {MaximumPasswordLength} characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            result.AddError("New password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            result.AddError("New password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            result.AddError("New password must contain at least one number.");
        }

        if (!password.Any(IsSymbolOrPunctuation))
        {
            result.AddError("New password must contain at least one symbol.");
        }

        if (!string.IsNullOrEmpty(request.CurrentPassword) &&
            string.Equals(password, request.CurrentPassword, StringComparison.Ordinal))
        {
            result.AddError("New password cannot be the same as the current password.");
        }

        await ValidateAgainstPasswordBlocklistAsync(password, result, cancellationToken);

        ValidateAgainstEmailUsername(password, request.Email, result);
        ValidateAgainstMobileNumber(password, request.MobileNumber, result);
        ValidateAgainstNameParts(password, request.FullNameAsPerId, result);

        return result;
    }

    private async Task ValidateAgainstPasswordBlocklistAsync(
        string password,
        PasswordPolicyValidationResult result,
        CancellationToken cancellationToken)
    {
        var normalizedPassword = NormalizeBlockedPassword(password);

        var isBlocked = await _dbContext.PasswordBlocklists
            .AsNoTracking()
            .AnyAsync(
                blockedPassword =>
                    blockedPassword.IsActive &&
                    blockedPassword.NormalizedValue == normalizedPassword,
                cancellationToken);

        if (isBlocked)
        {
            result.AddError("New password is too common. Please choose a stronger password.");
        }
    }

    private static string NormalizeBlockedPassword(string password)
    {
        return password.Trim().ToLowerInvariant();
    }

    private static bool IsSymbolOrPunctuation(char character)
    {
        return char.IsSymbol(character) || char.IsPunctuation(character);
    }

    private static void ValidateAgainstEmailUsername(
        string password,
        string? email,
        PasswordPolicyValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return;
        }

        var emailUsername = email.Split('@')[0];

        if (emailUsername.Length < 3)
        {
            return;
        }

        if (ContainsIgnoreCase(password, emailUsername))
        {
            result.AddError("New password cannot contain your email username.");
        }
    }

    private static void ValidateAgainstMobileNumber(
        string password,
        string? mobileNumber,
        PasswordPolicyValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return;
        }

        var mobileDigits = new string(mobileNumber.Where(char.IsDigit).ToArray());

        if (mobileDigits.Length < 6)
        {
            return;
        }

        var passwordDigits = new string(password.Where(char.IsDigit).ToArray());

        if (passwordDigits.Contains(mobileDigits, StringComparison.Ordinal))
        {
            result.AddError("New password cannot contain your mobile number.");
        }
    }

    private static void ValidateAgainstNameParts(
        string password,
        string? fullNameAsPerId,
        PasswordPolicyValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(fullNameAsPerId))
        {
            return;
        }

        var normalizedPassword = NormalizeForPersonalInfoCheck(password);

        var blockedNameTokens = fullNameAsPerId
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(GetBlockedNameTokens)
            .Where(token => token.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var token in blockedNameTokens)
        {
            if (normalizedPassword.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("New password cannot contain obvious parts of your name.");
                return;
            }
        }
    }

    private static IEnumerable<string> GetBlockedNameTokens(string namePart)
    {
        var normalizedNamePart = NormalizeForPersonalInfoCheck(namePart);

        if (normalizedNamePart.Length < 3)
        {
            yield break;
        }

        // Full name part, e.g. dalvinpal, singh
        yield return normalizedNamePart;

        // Obvious prefix for longer names, e.g. dalvinpal -> dalvin
        if (normalizedNamePart.Length >= 6)
        {
            yield return normalizedNamePart[..6];
        }

        // Shorter obvious prefix, e.g. dalvinpal -> dalvi
        if (normalizedNamePart.Length >= 5)
        {
            yield return normalizedNamePart[..5];
        }
    }

    private static string NormalizeForPersonalInfoCheck(string value)
    {
        return new string(
            value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
