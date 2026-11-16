using System.ComponentModel.DataAnnotations;

namespace PbmEventPortal.Web.ViewModels.Admin.JudgeConflicts;

public class JudgeConflictEditInputDTO
{
    public Guid Id { get; set; }

    [Display(Name = "Judge")]
    [Required(ErrorMessage = "Judge is required.")]
    public Guid? JudgeUserId { get; set; }

    [Display(Name = "Conflict type")]
    [Required(ErrorMessage = "Conflict type is required.")]
    public string ConflictType { get; set; } = string.Empty;

    [Display(Name = "Team")]
    public Guid? TeamId { get; set; }

    [Display(Name = "Participant")]
    public Guid? ParticipantId { get; set; }

    [Display(Name = "Competition category")]
    public Guid? CompetitionCategoryId { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}
