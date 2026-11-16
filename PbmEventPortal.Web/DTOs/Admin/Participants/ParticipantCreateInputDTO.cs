using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.Participants;

public class ParticipantCreateInputDTO
{
    [Display(Name = "Competition year")]
    [Required(ErrorMessage = "Competition year is required.")]
    public Guid? CompetitionYearId { get; set; }

    [Display(Name = "Team")]
    [Required(ErrorMessage = "Team is required.")]
    public Guid? TeamId { get; set; }

    [Display(Name = "Full name as per ID")]
    [Required(ErrorMessage = "Full name as per ID is required.")]
    public string FullNameAsPerId { get; set; } = string.Empty;

    [Display(Name = "Full name in Gurmukhi")]
    [Required(ErrorMessage = "Full name in Gurmukhi is required.")]
    public string FullNameGurmukhi { get; set; } = string.Empty;

    [Display(Name = "ID type")]
    [Required(ErrorMessage = "ID type is required.")]
    public string IdType { get; set; } = PbmEventPortal.Application.Enums.ProfileEnums.IdTypeNric;

    public string? NricNumber { get; set; }

    [Display(Name = "NRIC number")]
    public string? NricNumberDisplay { get; set; }

    [Display(Name = "Passport number")]
    public string? PassportNumber { get; set; }

    [Display(Name = "Passport expiry date")]
    public DateOnly? PassportExpiryDate { get; set; }

    public string? MobileNumber { get; set; }

    [Display(Name = "Country code")]
    public string MobileCountryCode { get; set; } = "+60";

    [Display(Name = "Mobile number")]
    public string? MobileLocalNumber { get; set; }

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "Parent / guardian name")]
    [Required(ErrorMessage = "Parent / guardian name is required.")]
    public string ParentGuardianName { get; set; } = string.Empty;

    public string ParentGuardianMobile { get; set; } = string.Empty;

    [Display(Name = "Country code")]
    public string ParentGuardianMobileCountryCode { get; set; } = "+60";

    [Display(Name = "Parent / guardian mobile")]
    public string? ParentGuardianMobileLocalNumber { get; set; }

    [Display(Name = "Gender")]
    [Required(ErrorMessage = "Gender is required.")]
    public string Gender { get; set; } = PbmEventPortal.Application.Enums.ProfileEnums.GenderMale;

    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Tahap")]
    [Required(ErrorMessage = "Tahap is required.")]
    public string TahapCode { get; set; } = PbmEventPortal.Application.Enums.TahapEnums.Tahap1;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
