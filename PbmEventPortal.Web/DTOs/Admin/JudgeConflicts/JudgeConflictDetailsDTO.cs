using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.JudgeConflicts;

public class JudgeConflictDetailsDTO
{
    public Guid Id { get; set; }

    public string JudgeName { get; set; } = string.Empty;

    public string JudgeEmail { get; set; } = string.Empty;

    public string JudgeMobileNumber { get; set; } = string.Empty;

    public string ConflictType { get; set; } = string.Empty;

    public string TargetDisplay { get; set; } = "Custom / not linked";

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedByName { get; set; }

    public string JudgeDisplay => string.IsNullOrWhiteSpace(JudgeEmail)
        ? JudgeName
        : $"{JudgeName} ({JudgeEmail})";

    public string ConflictTypeDisplay => ConflictType switch
    {
        JudgeEnums.ConflictTypeOwnTeam => "Own team",
        JudgeEnums.ConflictTypeRelatedParticipant => "Related participant",
        JudgeEnums.ConflictTypeCategoryBlock => "Category block",
        JudgeEnums.ConflictTypeCustom => "Custom",
        _ => ConflictType
    };

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "Not specified" : Notes;

    public string CreatedByDisplay => string.IsNullOrWhiteSpace(CreatedByName) ? "System / unknown" : CreatedByName;

    public string UpdatedByDisplay => string.IsNullOrWhiteSpace(UpdatedByName) ? "Not updated yet" : UpdatedByName;
}
