using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class ScoreEntryScore
{
    public Guid Id { get; set; }

    public Guid ScoreEntryId { get; set; }

    public Guid RegistrationEntryId { get; set; }

    public Guid RegistrationEntryMemberId { get; set; }

    public Guid CriterionId { get; set; }

    public Guid? JudgeAssignmentId { get; set; }

    public decimal ScoreValue { get; set; }

    public string ScoreSourceType { get; set; } = null!;

    public decimal? PreviousScoreValue { get; set; }

    public bool IsPendingVerification { get; set; }

    public string? OverrideReason { get; set; }

    public DateTime? OverrideAt { get; set; }

    public Guid? OverrideBy { get; set; }

    public DateTime? ChangeVerifiedAt { get; set; }

    public Guid? ChangeVerifiedBy { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? ChangeVerifiedByNavigation { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual CompetitionScoringCriterion Criterion { get; set; } = null!;

    public virtual JudgeAssignment? JudgeAssignment { get; set; }

    public virtual UserProfile? OverrideByNavigation { get; set; }

    public virtual RegistrationEntryMember RegistrationEntryMember { get; set; } = null!;

    public virtual ScoreEntry ScoreEntry { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
