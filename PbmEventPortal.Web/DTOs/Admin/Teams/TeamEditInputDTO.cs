using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.Teams;

public class TeamEditInputDTO
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Team code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "English name")]
    public string NameEnglish { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Gurmukhi name")]
    public string? NameGurmukhi { get; set; }

    [Required]
    [Display(Name = "State")]
    public Guid? StateId { get; set; }

    [Required]
    [Display(Name = "Region")]
    public Guid? RegionId { get; set; }

    [Display(Name = "Active team")]
    public bool IsActive { get; set; } = true;
}