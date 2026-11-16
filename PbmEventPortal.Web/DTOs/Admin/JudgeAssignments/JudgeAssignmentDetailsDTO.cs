namespace PbmEventPortal.Web.ViewModels.Admin.JudgeAssignments;

public class JudgeAssignmentDetailsDTO
{
    public Guid Id { get; set; }

    public string CompetitionYearDisplay { get; set; } = string.Empty;

    public string CompetitionDisplay { get; set; } = string.Empty;

    public string CategoryDisplay { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string JudgeName { get; set; } = string.Empty;

    public string JudgeEmail { get; set; } = string.Empty;

    public string JudgeMobileNumber { get; set; } = string.Empty;

    public string CriterionDisplay { get; set; } = "All criteria";

    public string ScheduleDisplay { get; set; } = "Not linked";

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedByName { get; set; }

    public string JudgeDisplay => string.IsNullOrWhiteSpace(JudgeEmail)
        ? JudgeName
        : $"{JudgeName} ({JudgeEmail})";

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "Not specified" : Notes;

    public string CreatedByDisplay => string.IsNullOrWhiteSpace(CreatedByName) ? "System / unknown" : CreatedByName;

    public string UpdatedByDisplay => string.IsNullOrWhiteSpace(UpdatedByName) ? "Not updated yet" : UpdatedByName;
}
