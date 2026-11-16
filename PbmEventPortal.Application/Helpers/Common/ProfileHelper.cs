using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Application.Helpers.Common;

public class ProfileHelper
{
    public static string BuildPhoneNumber(string? countryCode, string? localNumber)
    {
        if (string.IsNullOrWhiteSpace(localNumber)) return string.Empty;

        var normalizedCountryCode = string.IsNullOrWhiteSpace(countryCode) ? "+60" : countryCode.Trim();
        return $"{normalizedCountryCode}{localNumber}".Replace(" ", "");
    }

    public static string ExtractCountryCode(string? fullNumber)
    {
        if (string.IsNullOrWhiteSpace(fullNumber)) return "+60";

        var normalizedNumber = fullNumber.Trim();
        return ProfileEnums.SupportedCountryCodes.FirstOrDefault(normalizedNumber.StartsWith) ?? "+60";
    }

    public static string? ExtractLocalNumber(string? fullNumber)
    {
        if (string.IsNullOrWhiteSpace(fullNumber)) return null;

        var normalizedNumber = fullNumber.Trim();
        var countryCode = ExtractCountryCode(normalizedNumber);
        return normalizedNumber.StartsWith(countryCode) ? normalizedNumber[countryCode.Length..] : normalizedNumber;
    }

    public static string GetGenderDisplayName(string gender)
    {
        return gender.Trim().ToUpperInvariant() switch
        {
            "M" => "Male",
            "MALE" => "Male",
            "F" => "Female",
            "FEMALE" => "Female",
            _ => string.Empty
        };
    }

    public static string GetTahapDisplayName(string tahapCode)
    {
        return tahapCode.ToUpperInvariant() switch
        {
            "TAHAP_1" => "Tahap 1",
            "TAHAP_2" => "Tahap 2",
            "TAHAP_3" => "Tahap 3",
            "TAHAP_4" => "Tahap 4",
            _ => tahapCode
        };
    }
}
