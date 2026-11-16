namespace PbmEventPortal.Web.ViewModels.Admin.CompetitionYears;

public class CompetitionYearListItemDTO
{
    public Guid Id { get; set; }

    public int YearNo { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public DateTime? RegistrationOpenAt { get; set; }

    public DateTime? RegistrationCloseAt { get; set; }

    public bool IsActive { get; set; }

    public int CompetitionCount { get; set; }

    public int ParticipantCount { get; set; }

    public int VenueCount { get; set; }

    public int ScheduleItemCount { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string RegistrationWindowDisplay
    {
        get
        {
            if (RegistrationOpenAt == null && RegistrationCloseAt == null)
            {
                return "Not set";
            }

            var openText = RegistrationOpenAt?.ToString("dd MMM yyyy, hh:mm tt") ?? "No start";
            var closeText = RegistrationCloseAt?.ToString("dd MMM yyyy, hh:mm tt") ?? "No end";

            return $"{openText} → {closeText}";
        }
    }
}
