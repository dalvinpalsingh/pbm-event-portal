namespace PbmEventPortal.Application.Enums;

public class ProfileEnums
{
    public static readonly string[] SupportedCountryCodes = ["+65", "+91", "+61", "+44", "+46", "+1", "+62", "+66", "+60"];

    public const string IdTypeNric = "NRIC";
    public const string IdTypePassport = "PASSPORT";
    public static readonly string[] IdTypes = [IdTypeNric, IdTypePassport];

    public const string GenderMale = "M";
    public const string GenderFemale = "F";
    public static readonly string[] Genders = [GenderMale, GenderFemale];
}
