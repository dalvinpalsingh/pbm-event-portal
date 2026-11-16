namespace PbmEventPortal.Web.ViewModels.Admin.Venues;

public class VenueDetailsDTO
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? LocationText { get; set; }

    public int? Capacity { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public string CompetitionYearDisplay => string.IsNullOrWhiteSpace(CompetitionYearDisplayName)
        ? CompetitionYearNo.ToString()
        : CompetitionYearDisplayName;

    public string LocationDisplay => string.IsNullOrWhiteSpace(LocationText)
        ? "Not specified"
        : LocationText;

    public string CapacityDisplay => Capacity.HasValue
        ? Capacity.Value.ToString()
        : "Not specified";

    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes)
        ? "No notes recorded."
        : Notes;

    public string StatusDisplay => IsActive
        ? "Active"
        : "Inactive";
}
