namespace PbmEventPortal.Web.ViewModels.Admin.Teams;

public class TeamListItemDTO
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string NameEnglish { get; set; } = string.Empty;

    public string? NameGurmukhi { get; set; }

    public string StateName { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int TeamManagerCount { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}