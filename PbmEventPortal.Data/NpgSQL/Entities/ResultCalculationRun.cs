using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class ResultCalculationRun
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public string CalculationScope { get; set; } = null!;

    public int RunNo { get; set; }

    public string RunStatus { get; set; } = null!;

    public bool IsCurrent { get; set; }

    public DateTime CalculatedAt { get; set; }

    public Guid? CalculatedBy { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CalculatedByNavigation { get; set; }

    public virtual ICollection<CompetitionResult> CompetitionResults { get; set; } = new List<CompetitionResult>();

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<OverallTahapResult> OverallTahapResults { get; set; } = new List<OverallTahapResult>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
