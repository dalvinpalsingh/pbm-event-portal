namespace PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

public class PointsSchemeListItemDTO
{
    public Guid Id { get; set; }

    public string SchemeName { get; set; } = string.Empty;

    public short WinnerCount { get; set; }

    public bool IsActive { get; set; }

    public List<PointsSchemeItemListItemViewModel> Items { get; set; } = [];

    public string StatusDisplay => IsActive
        ? "Active"
        : "Inactive";

    public string SchemeNameDisplay => string.IsNullOrWhiteSpace(SchemeName)
        ? "Unnamed points scheme"
        : SchemeName.Replace("_", " ");
}

public class PointsSchemeItemListItemViewModel
{
    public short PlacingNo { get; set; }

    public short PointsAwarded { get; set; }

    public string PlacingDisplay => PlacingNo switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{PlacingNo}th"
    };

    public string PointsDisplay => PointsAwarded == 1
        ? "1 point"
        : $"{PointsAwarded} points";
}
