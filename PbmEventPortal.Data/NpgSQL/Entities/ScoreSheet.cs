using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class ScoreSheet
{
    public Guid Id { get; set; }

    public Guid CompetitionCategoryId { get; set; }

    public int VersionNo { get; set; }

    public string ScoreSheetStatus { get; set; } = null!;

    public bool IsCurrent { get; set; }

    public Guid? PreviousScoreSheetId { get; set; }

    public Guid? ReplacedByScoreSheetId { get; set; }

    public string? Notes { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public Guid? SubmittedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public Guid? VerifiedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionCategory CompetitionCategory { get; set; } = null!;

    public virtual ICollection<CompetitionResult> CompetitionResults { get; set; } = new List<CompetitionResult>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<ScoreSheet> InversePreviousScoreSheet { get; set; } = new List<ScoreSheet>();

    public virtual ICollection<ScoreSheet> InverseReplacedByScoreSheet { get; set; } = new List<ScoreSheet>();

    public virtual ScoreSheet? PreviousScoreSheet { get; set; }

    public virtual ScoreSheet? ReplacedByScoreSheet { get; set; }

    public virtual ICollection<ScoreEntry> ScoreEntries { get; set; } = new List<ScoreEntry>();

    public virtual UserProfile? SubmittedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual UserProfile? VerifiedByNavigation { get; set; }
}
