using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class Venue
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? LocationText { get; set; }

    public int? Capacity { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<ScheduleItem> ScheduleItems { get; set; } = new List<ScheduleItem>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
