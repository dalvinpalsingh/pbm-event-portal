using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Web.ViewModels.Admin.JudgeConflicts;

public class JudgeConflictListItemDTO
{
    public Guid Id { get; set; }

    public string JudgeName { get; set; } = string.Empty;

    public string JudgeEmail { get; set; } = string.Empty;

    public string ConflictType { get; set; } = string.Empty;

    public string? TeamCode { get; set; }

    public string? TeamName { get; set; }

    public string? ParticipantName { get; set; }

    public string? ParticipantTeamCode { get; set; }

    public string? CompetitionCode { get; set; }

    public string? CompetitionName { get; set; }

    public string? CategoryCode { get; set; }

    public bool IsActive { get; set; }

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

    public string TargetDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(TeamName)) return $"{TeamCode} - {TeamName}";
            if (!string.IsNullOrWhiteSpace(ParticipantName)) return string.IsNullOrWhiteSpace(ParticipantTeamCode) ? ParticipantName : $"{ParticipantName} / {ParticipantTeamCode}";
            if (!string.IsNullOrWhiteSpace(CompetitionCode)) return $"{CompetitionCode} / {CategoryCode} - {CompetitionName}";
            return "Custom / not linked";
        }
    }

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}
