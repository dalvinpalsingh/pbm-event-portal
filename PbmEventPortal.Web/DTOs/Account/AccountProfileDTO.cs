namespace PbmEventPortal.Web.ViewModels.Account;

public class AccountProfileDTO
{
    public Guid Id { get; set; }

    public string FullNameAsPerId { get; set; } = string.Empty;

    public string? FullNameGurmukhi { get; set; }

    public string IdType { get; set; } = string.Empty;

    public string? NricNumber { get; set; }

    public string? PassportNumber { get; set; }

    public DateOnly? PassportExpiryDate { get; set; }

    public string MobileNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public char Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public bool IsActive { get; set; }

    public string IdNumber => IdType == PbmEventPortal.Application.Enums.ProfileEnums.IdTypeNric
        ? NricNumber ?? string.Empty
        : PassportNumber ?? string.Empty;

    public string GenderDisplay => Gender switch
    {
        'M' => "Male",
        'F' => "Female",
        _ => Gender.ToString()
    };

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string WhatsAppUrl => $"https://wa.me/{MobileNumber.Replace("+", "").Replace(" ", "").Replace("-", "")}";

    public string CallUrl => $"tel:{MobileNumber}";
}
