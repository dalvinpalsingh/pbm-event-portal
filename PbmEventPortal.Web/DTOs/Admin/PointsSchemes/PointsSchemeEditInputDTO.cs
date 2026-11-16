using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

public class PointsSchemeEditInputDTO
{
    public Guid Id { get; set; }

    [Display(Name = "Scheme name")]
    [Required(ErrorMessage = "Scheme name is required.")]
    public string SchemeName { get; set; } = string.Empty;

    [Display(Name = "Winner count")]
    public short WinnerCount { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public List<PointsSchemeEditItemInputViewModel> Items { get; set; } = [];
}

public class PointsSchemeEditItemInputViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Placing")]
    public short PlacingNo { get; set; }

    [Display(Name = "Points awarded")]
    [Required(ErrorMessage = "Points awarded is required.")]
    public short? PointsAwarded { get; set; }
}
