using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class PointsScheme
{
    public Guid Id { get; set; }

    public string SchemeName { get; set; } = null!;

    public short WinnerCount { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<CompetitionResult> CompetitionResults { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<CompetitionYear> CompetitionYears { get; set; } = new List<CompetitionYear>();

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<PointsSchemeItem> PointsSchemeItems { get; set; } = new List<PointsSchemeItem>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
