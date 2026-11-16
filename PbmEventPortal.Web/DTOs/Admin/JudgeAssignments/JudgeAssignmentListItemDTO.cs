namespace PbmEventPortal.Web.ViewModels.Admin.JudgeAssignments;

public class JudgeAssignmentListItemDTO
{
    public Guid Id { get; set; }

    public int CompetitionYearNo { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    public string CompetitionCode { get; set; } = string.Empty;

    public string CompetitionNameEnglish { get; set; } = string.Empty;

    public string CategoryCode { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string JudgeName { get; set; } = string.Empty;

    public string JudgeEmail { get; set; } = string.Empty;

    public string? CriterionName { get; set; }

    public short? CriterionNo { get; set; }

    public DateOnly? ScheduleDate { get; set; }

    public TimeOnly? ScheduleStartTime { get; set; }

    public TimeOnly? ScheduleEndTime { get; set; }

    public string? ScheduleTitle { get; set; }

    public bool IsActive { get; set; }

    public string CompetitionYearDisplay => string.IsNullOrWhiteSpace(CompetitionYearDisplayName)
        ? CompetitionYearNo.ToString()
        : CompetitionYearDisplayName;

    public string CompetitionDisplay => string.IsNullOrWhiteSpace(CompetitionNameEnglish)
        ? CompetitionCode
        : $"{CompetitionCode} - {CompetitionNameEnglish}";

    public string CategoryDisplay => $"{CategoryCode} / {EventType}";

    public string JudgeDisplay => string.IsNullOrWhiteSpace(JudgeEmail)
        ? JudgeName
        : $"{JudgeName} ({JudgeEmail})";

    public string CriterionDisplay => string.IsNullOrWhiteSpace(CriterionName)
        ? "All criteria"
        : CriterionNo.HasValue
            ? $"{CriterionNo}. {CriterionName}"
            : CriterionName;

    public string ScheduleDisplay
    {
        get
        {
            if (ScheduleDate == null || ScheduleStartTime == null || ScheduleEndTime == null)
            {
                return "Not linked";
            }

            var title = string.IsNullOrWhiteSpace(ScheduleTitle) ? "Competition session" : ScheduleTitle;
            return $"{ScheduleDate:yyyy-MM-dd} {ScheduleStartTime:HH:mm}-{ScheduleEndTime:HH:mm} / {title}";
        }
    }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}
