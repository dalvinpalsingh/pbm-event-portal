using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CompetitionYear
{
    public Guid Id { get; set; }

    public int YearNo { get; set; }

    public string DisplayName { get; set; } = null!;

    public DateTime? RegistrationOpenAt { get; set; }

    public DateTime? RegistrationCloseAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Guid? DefaultPointsSchemeId { get; set; }

    public virtual ICollection<CompetitionResult> CompetitionResults { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<Competition> Competitions { get; set; } = new List<Competition>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual PointsScheme? DefaultPointsScheme { get; set; }

    public virtual ICollection<OverallTahapResult> OverallTahapResults { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();

    public virtual ICollection<ResultCalculationRun> ResultCalculationRuns { get; set; } = new List<ResultCalculationRun>();

    public virtual ICollection<ScheduleItem> ScheduleItems { get; set; } = new List<ScheduleItem>();

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDrafts { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual ICollection<TieBreakerPolicy> TieBreakerPolicies { get; set; } = new List<TieBreakerPolicy>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual ICollection<Venue> Venues { get; set; } = new List<Venue>();
}
