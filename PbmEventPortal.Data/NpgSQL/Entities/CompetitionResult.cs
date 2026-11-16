using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CompetitionResult
{
    public Guid Id { get; set; }

    public Guid CalculationRunId { get; set; }

    public Guid CompetitionYearId { get; set; }

    public Guid CompetitionId { get; set; }

    public Guid CompetitionCategoryId { get; set; }

    public Guid ScoreSheetId { get; set; }

    public int ScoreSheetVersionNo { get; set; }

    public string SourceScoreSheetStatus { get; set; } = null!;

    public DateTime? ScoreSheetVerifiedAt { get; set; }

    public Guid? ScoreSheetVerifiedBy { get; set; }

    public Guid PointsSchemeId { get; set; }

    public short WinnerCount { get; set; }

    public int EligibleEntryCount { get; set; }

    public int AwardedEntryCount { get; set; }

    public string ResultStatus { get; set; } = null!;

    public bool IsCurrent { get; set; }

    public DateTime CalculatedAt { get; set; }

    public Guid? CalculatedBy { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CalculatedByNavigation { get; set; }

    public virtual ResultCalculationRun CalculationRun { get; set; } = null!;

    public virtual Competition Competition { get; set; } = null!;

    public virtual CompetitionCategory CompetitionCategory { get; set; } = null!;

    public virtual ICollection<CompetitionResultEntry> CompetitionResultEntries { get; set; } = new List<CompetitionResultEntry>();

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipants { get; set; } = new List<CompetitionResultParticipant>();

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual PointsScheme PointsScheme { get; set; } = null!;

    public virtual ScoreSheet ScoreSheet { get; set; } = null!;

    public virtual UserProfile? ScoreSheetVerifiedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
