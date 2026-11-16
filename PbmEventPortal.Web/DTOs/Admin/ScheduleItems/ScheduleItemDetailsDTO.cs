namespace PbmEventPortal.Web.ViewModels.Admin.ScheduleItems;

public class ScheduleItemDetailsDTO
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    public DateOnly ScheduleDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public TimeOnly? ReportingTime { get; set; }

    public string DisplayTitle { get; set; } = string.Empty;

    public string ScheduleType { get; set; } = string.Empty;

    public Guid? CompetitionCategoryId { get; set; }

    public string? CompetitionCode { get; set; }

    public string? CompetitionNameEnglish { get; set; }

    public string? CategoryCode { get; set; }

    public string? CategoryName { get; set; }

    public Guid? VenueId { get; set; }

    public string? VenueCode { get; set; }

    public string? VenueName { get; set; }

    public string? VenueLocationText { get; set; }

    public string? SessionGroup { get; set; }

    public short? SequenceNo { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public string? Notes { get; set; }

    public string CompetitionYearDisplay => string.IsNullOrWhiteSpace(CompetitionYearDisplayName)
        ? CompetitionYearNo.ToString()
        : CompetitionYearDisplayName;

    public string DateDisplay => ScheduleDate.ToString("dd MMM yyyy");

    public string TimeDisplay => $"{StartTime:HH\\:mm} - {EndTime:HH\\:mm}";

    public string ReportingTimeDisplay => ReportingTime.HasValue
        ? ReportingTime.Value.ToString("HH\\:mm")
        : "Not specified";

    public string ScheduleTypeDisplay => string.IsNullOrWhiteSpace(ScheduleType)
        ? "Not specified"
        : ScheduleType.Replace("_", " ");

    public string CompetitionCategoryDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CompetitionCode) && string.IsNullOrWhiteSpace(CategoryCode))
            {
                return "Not linked";
            }

            var competition = string.IsNullOrWhiteSpace(CompetitionNameEnglish)
                ? CompetitionCode
                : CompetitionNameEnglish;

            return $"{competition} / {CategoryCode}";
        }
    }

    public string VenueDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(VenueCode) && string.IsNullOrWhiteSpace(VenueName))
            {
                return "Not assigned";
            }

            return string.IsNullOrWhiteSpace(VenueLocationText)
                ? VenueName ?? VenueCode ?? "Not assigned"
                : $"{VenueName} ({VenueLocationText})";
        }
    }

    public string SessionGroupDisplay => string.IsNullOrWhiteSpace(SessionGroup)
        ? "Not specified"
        : SessionGroup;

    public string SequenceNoDisplay => SequenceNo.HasValue
        ? SequenceNo.Value.ToString()
        : "Not specified";

    public string StatusDisplay => string.IsNullOrWhiteSpace(Status)
        ? "Not specified"
        : Status.Replace("_", " ");

    public string PublishedDisplay => IsPublished
        ? "Published"
        : "Not published";

    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes)
        ? "No notes recorded."
        : Notes;
}
