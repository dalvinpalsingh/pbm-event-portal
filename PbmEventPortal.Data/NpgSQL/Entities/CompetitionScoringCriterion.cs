using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CompetitionScoringCriterion
{
    public Guid Id { get; set; }

    public Guid CompetitionId { get; set; }

    public short CriterionNo { get; set; }

    public string CriterionName { get; set; } = null!;

    public decimal? MaxScore { get; set; }

    public short SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual Competition Competition { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();

    public virtual ICollection<ScoreEntryScore> ScoreEntryScores { get; set; } = new List<ScoreEntryScore>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
