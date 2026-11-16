using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class JudgeAssignment
{
    public Guid Id { get; set; }

    public Guid CompetitionCategoryId { get; set; }

    public Guid? ScheduleItemId { get; set; }

    public Guid JudgeUserId { get; set; }

    public Guid? CriterionId { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionCategory CompetitionCategory { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual CompetitionScoringCriterion? Criterion { get; set; }

    public virtual UserProfile JudgeUser { get; set; } = null!;

    public virtual ScheduleItem? ScheduleItem { get; set; }

    public virtual ICollection<ScoreEntryScore> ScoreEntryScores { get; set; } = new List<ScoreEntryScore>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
