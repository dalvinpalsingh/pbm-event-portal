using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.Venues;

public class VenueCreateInputDTO
{
    [Display(Name = "Competition year")]
    [Required(ErrorMessage = "Competition year is required.")]
    public Guid? CompetitionYearId { get; set; }

    [Display(Name = "Code")]
    [Required(ErrorMessage = "Venue code is required.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Name")]
    [Required(ErrorMessage = "Venue name is required.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Location")]
    public string? LocationText { get; set; }

    [Display(Name = "Capacity")]
    public int? Capacity { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
