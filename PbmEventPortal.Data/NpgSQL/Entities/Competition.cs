using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class Competition
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public string Code { get; set; } = null!;

    public string NameEnglish { get; set; } = null!;

    public string? NameGurmukhi { get; set; }

    public string CompetitionType { get; set; } = null!;

    public short CriteriaCount { get; set; }

    public short MinJudges { get; set; }

    public short MaxJudges { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Guid? PointsSchemeId { get; set; }

    public virtual ICollection<CompetitionCategory> CompetitionCategories { get; set; } = new List<CompetitionCategory>();

    public virtual ICollection<CompetitionResult> CompetitionResults { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<CompetitionScoringCriterion> CompetitionScoringCriteria { get; set; } = new List<CompetitionScoringCriterion>();

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual PointsScheme? PointsScheme { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
