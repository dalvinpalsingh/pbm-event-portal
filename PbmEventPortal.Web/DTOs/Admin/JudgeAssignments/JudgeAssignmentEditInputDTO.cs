using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.JudgeAssignments;

public class JudgeAssignmentEditInputDTO
{
    public Guid Id { get; set; }

    [Display(Name = "Competition category")]
    [Required(ErrorMessage = "Competition category is required.")]
    public Guid? CompetitionCategoryId { get; set; }

    [Display(Name = "Schedule item")]
    public Guid? ScheduleItemId { get; set; }

    [Display(Name = "Judge")]
    [Required(ErrorMessage = "Judge is required.")]
    public Guid? JudgeUserId { get; set; }

    [Display(Name = "Scoring criterion")]
    public Guid? CriterionId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
