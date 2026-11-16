using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

public class UserProfileEditInputDTO
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Full name as per ID")]
    public string FullNameAsPerId { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Full name in Gurmukhi")]
    public string? FullNameGurmukhi { get; set; }

    [Required]
    [Display(Name = "ID type")]
    public string IdType { get; set; } = PbmEventPortal.Application.Enums.ProfileEnums.IdTypeNric;

    public string? NricNumber { get; set; }

    [Display(Name = "NRIC number")]
    public string? NricNumberDisplay { get; set; }

    [StringLength(20)]
    [Display(Name = "Passport number")]
    public string? PassportNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Passport expiry date")]
    public DateOnly? PassportExpiryDate { get; set; }

    public string MobileNumber { get; set; } = string.Empty;

    [Display(Name = "Country code")]
    public string MobileCountryCode { get; set; } = "+60";

    [Display(Name = "Mobile number")]
    public string? MobileLocalNumber { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Gender { get; set; } = PbmEventPortal.Application.Enums.ProfileEnums.GenderMale;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Active user")]
    public bool IsActive { get; set; } = true;
}
