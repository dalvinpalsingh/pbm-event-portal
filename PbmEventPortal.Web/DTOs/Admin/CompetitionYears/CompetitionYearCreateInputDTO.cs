using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionYears;

public class CompetitionYearCreateInputDTO
{
    [Display(Name = "Year")]
    [Required(ErrorMessage = "Year is required.")]
    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100.")]
    public int? YearNo { get; set; }

    [Display(Name = "Display name")]
    [Required(ErrorMessage = "Display name is required.")]
    [MaxLength(255, ErrorMessage = "Display name must not be more than 255 characters.")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Registration opens")]
    public DateTime? RegistrationOpenAt { get; set; }

    [Display(Name = "Registration closes")]
    public DateTime? RegistrationCloseAt { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
