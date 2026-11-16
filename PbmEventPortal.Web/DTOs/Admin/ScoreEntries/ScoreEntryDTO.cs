using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.ScoreEntries;

public class ScoreEntryListItemViewModel
{
    public Guid Id { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string TeamDisplay { get; set; } = string.Empty;
    public string EntryDisplay { get; set; } = string.Empty;
    public string JudgeDisplay { get; set; } = string.Empty;
    public string CriterionDisplay { get; set; } = string.Empty;
    public string ScoreStatus { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public bool IsActive { get; set; }
    public string StatusDisplay => IsActive ? ScoreStatus : $"{ScoreStatus} / Inactive";
}

public class ScoreEntryDetailsViewModel
{
    public Guid Id { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string TeamDisplay { get; set; } = string.Empty;
    public string EntryDisplay { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public string JudgeDisplay { get; set; } = string.Empty;
    public string CriterionDisplay { get; set; } = string.Empty;
    public string ScoreStatus { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public string NotesDisplay { get; set; } = "-";
    public bool IsActive { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string SubmittedByDisplay { get; set; } = "Not submitted yet";
    public DateTime? VerifiedAt { get; set; }
    public string VerifiedByDisplay { get; set; } = "Not verified yet";
    public DateTime CreatedAt { get; set; }
    public string CreatedByDisplay { get; set; } = "System / unknown";
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedByDisplay { get; set; } = "Not updated yet";
    public List<ScoreEntryMemberScoreGroupViewModel> MemberScores { get; set; } = [];
    public string StatusDisplay => IsActive ? ScoreStatus : $"{ScoreStatus} / Inactive";
}

public class ScoreEntryMemberScoreGroupViewModel
{
    public Guid RegistrationEntryMemberId { get; set; }
    public short MemberOrder { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantMetaDisplay { get; set; } = string.Empty;
    public decimal MemberTotal { get; set; }
    public List<ScoreEntryCriterionScoreViewModel> Scores { get; set; } = [];
}

public class ScoreEntryCriterionScoreViewModel
{
    public Guid CriterionId { get; set; }
    public string CriterionDisplay { get; set; } = string.Empty;
    public decimal? MaxScore { get; set; }
    public decimal ScoreValue { get; set; }
    public string NotesDisplay { get; set; } = "-";
    public bool IsActive { get; set; }
}

public class ScoreEntryCreateInputViewModel
{
    [Display(Name = "Registration entry")]
    [Required(ErrorMessage = "Registration entry is required.")]
    public Guid? RegistrationEntryId { get; set; }

    [Display(Name = "Judge assignment")]
    [Required(ErrorMessage = "Judge assignment is required.")]
    public Guid? JudgeAssignmentId { get; set; }

    [Display(Name = "Score status")]
    [Required(ErrorMessage = "Score status is required.")]
    public string ScoreStatus { get; set; } = ScoreEnums.ScoreSheetStatusDraft;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<ScoreEntryScoreInputViewModel> Scores { get; set; } = [];
}

public class ScoreEntryEditInputViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Score status")]
    [Required(ErrorMessage = "Score status is required.")]
    public string ScoreStatus { get; set; } = ScoreEnums.ScoreSheetStatusDraft;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    public List<ScoreEntryScoreInputViewModel> Scores { get; set; } = [];
}

public class ScoreEntryScoreInputViewModel
{
    public Guid RegistrationEntryMemberId { get; set; }
    public Guid CriterionId { get; set; }

    [Display(Name = "Score")]
    public decimal? ScoreValue { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

public class ScoreEntryLookupOptionsViewModel
{
    public List<SelectListItem> RegistrationEntryOptions { get; set; } = [];
    public List<SelectListItem> JudgeAssignmentOptions { get; set; } = [];
    public List<SelectListItem> ScoreStatusOptions { get; set; } = [];
    public List<ScoreEntryRegistrationEntryOptionViewModel> RegistrationEntryMetadata { get; set; } = [];
    public List<ScoreEntryJudgeAssignmentOptionViewModel> JudgeAssignmentMetadata { get; set; } = [];
    public List<ScoreEntryCriterionOptionViewModel> CriterionMetadata { get; set; } = [];
}

public class ScoreEntryRegistrationEntryOptionViewModel
{
    public Guid Id { get; set; }
    public Guid CompetitionCategoryId { get; set; }
    public Guid CompetitionId { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public List<ScoreEntryMemberOptionViewModel> Members { get; set; } = [];
}

public class ScoreEntryMemberOptionViewModel
{
    public Guid RegistrationEntryMemberId { get; set; }
    public short MemberOrder { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantMetaDisplay { get; set; } = string.Empty;
}

public class ScoreEntryJudgeAssignmentOptionViewModel
{
    public Guid Id { get; set; }
    public Guid CompetitionCategoryId { get; set; }
    public Guid? CriterionId { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}

public class ScoreEntryCriterionOptionViewModel
{
    public Guid Id { get; set; }
    public Guid CompetitionId { get; set; }
    public short CriterionNo { get; set; }
    public string CriterionName { get; set; } = string.Empty;
    public decimal? MaxScore { get; set; }
    public string DisplayText => MaxScore.HasValue
        ? $"{CriterionNo}. {CriterionName} / {MaxScore.Value:0.##}"
        : $"{CriterionNo}. {CriterionName}";
}
