using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.ScheduleItems;

public class ScheduleItemEditInputDTO
{
    public Guid Id { get; set; }

    [Display(Name = "Competition year")]
    [Required(ErrorMessage = "Competition year is required.")]
    public Guid? CompetitionYearId { get; set; }

    [Display(Name = "Schedule date")]
    [Required(ErrorMessage = "Schedule date is required.")]
    public DateOnly? ScheduleDate { get; set; }

    [Display(Name = "Start time")]
    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "End time")]
    [Required(ErrorMessage = "End time is required.")]
    public TimeOnly? EndTime { get; set; }

    [Display(Name = "Reporting time")]
    public TimeOnly? ReportingTime { get; set; }

    [Display(Name = "Display title")]
    [Required(ErrorMessage = "Display title is required.")]
    public string DisplayTitle { get; set; } = string.Empty;

    [Display(Name = "Schedule type")]
    [Required(ErrorMessage = "Schedule type is required.")]
    public string ScheduleType { get; set; } = string.Empty;

    [Display(Name = "Competition category")]
    public Guid? CompetitionCategoryId { get; set; }

    [Display(Name = "Venue")]
    public Guid? VenueId { get; set; }

    [Display(Name = "Session group")]
    public string? SessionGroup { get; set; }

    [Display(Name = "Sequence number")]
    public short? SequenceNo { get; set; }

    [Display(Name = "Status")]
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Published")]
    public bool IsPublished { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
