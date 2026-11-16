using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

public class PointsSchemeCreateInputDTO
{
    [Display(Name = "Scheme name")]
    [Required(ErrorMessage = "Scheme name is required.")]
    public string SchemeName { get; set; } = string.Empty;

    [Display(Name = "Winner count")]
    [Required(ErrorMessage = "Winner count is required.")]
    public short? WinnerCount { get; set; } = 3;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<PointsSchemeCreateItemInputViewModel> Items { get; set; } = [];
}

public class PointsSchemeCreateItemInputViewModel
{
    [Display(Name = "Placing")]
    [Required(ErrorMessage = "Placing is required.")]
    public short? PlacingNo { get; set; }

    [Display(Name = "Points awarded")]
    [Required(ErrorMessage = "Points awarded is required.")]
    public short? PointsAwarded { get; set; }
}
