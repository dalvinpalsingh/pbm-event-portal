using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class ScheduleItem
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public DateOnly ScheduleDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string DisplayTitle { get; set; } = null!;

    public string ScheduleType { get; set; } = null!;

    public Guid? CompetitionCategoryId { get; set; }

    public Guid? VenueId { get; set; }

    public string? SessionGroup { get; set; }

    public short? SequenceNo { get; set; }

    public TimeOnly? ReportingTime { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionCategory? CompetitionCategory { get; set; }

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual Venue? Venue { get; set; }
}
