using Microsoft.AspNetCore.Mvc.Rendering;
using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.RegistrationEntries;

public class RegistrationEntryListItemViewModel
{
    public Guid Id { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string TeamDisplay { get; set; } = string.Empty;
    public string EntryNameDisplay { get; set; } = string.Empty;
    public string EntryStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ActiveMemberCount { get; set; }
    public string MemberSummary { get; set; } = string.Empty;

    public string StatusDisplay => IsActive ? EntryStatus : $"Inactive / {EntryStatus}";
}

public class RegistrationEntryDetailsViewModel
{
    public Guid Id { get; set; }
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string TeamDisplay { get; set; } = string.Empty;
    public string EntryNameDisplay { get; set; } = string.Empty;
    public string EntryStatus { get; set; } = string.Empty;
    public string ApprovedByDisplay { get; set; } = string.Empty;
    public string RejectedReasonDisplay { get; set; } = string.Empty;
    public string NotesDisplay { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByDisplay { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedByDisplay { get; set; } = string.Empty;
    public List<RegistrationEntryParticipantViewModel> Participants { get; set; } = [];

    public string StatusDisplay => IsActive ? EntryStatus : $"Inactive / {EntryStatus}";
}

public class RegistrationEntryParticipantViewModel
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public short ParticipantOrder { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantTahapCode { get; set; } = string.Empty;
    public string ParticipantTeamCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}

public class RegistrationEntryCreateInputViewModel
{
    public Guid? CompetitionCategoryId { get; set; }
    public Guid? TeamId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string? EntryName { get; set; }
    public string EntryStatus { get; set; } = RegistrationEnums.EntryStatusDraft;
    public Guid? ApprovedBy { get; set; }
    public string? RejectedReason { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid?> ParticipantIds { get; set; } = [];
}

public class RegistrationEntryEditInputViewModel : RegistrationEntryCreateInputViewModel
{
    public Guid Id { get; set; }
}

public class RegistrationEntryLookupOptionsViewModel
{
    public List<SelectListItem> CompetitionCategoryOptions { get; set; } = [];
    public List<SelectListItem> TeamOptions { get; set; } = [];
    public List<SelectListItem> EntryStatusOptions { get; set; } = [];
    public List<SelectListItem> ApprovedByOptions { get; set; } = [];
    public List<RegistrationEntryCategoryOptionViewModel> CategoryMetadata { get; set; } = [];
    public List<RegistrationEntryParticipantOptionViewModel> ParticipantMetadata { get; set; } = [];
}

public class RegistrationEntryCategoryOptionViewModel
{
    public Guid Id { get; set; }
    public Guid CompetitionYearId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int RequiredParticipantCount { get; set; }
}

public class RegistrationEntryParticipantOptionViewModel
{
    public Guid Id { get; set; }
    public Guid CompetitionYearId { get; set; }
    public Guid TeamId { get; set; }
    public string TahapCode { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
}
