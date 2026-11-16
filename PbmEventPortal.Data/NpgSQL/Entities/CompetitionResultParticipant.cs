using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CompetitionResultParticipant
{
    public Guid Id { get; set; }

    public Guid CompetitionResultId { get; set; }

    public Guid CompetitionResultEntryId { get; set; }

    public Guid RegistrationEntryMemberId { get; set; }

    public Guid ParticipantId { get; set; }

    public Guid TeamId { get; set; }

    public string ParticipantTahapCode { get; set; } = null!;

    public int PointsAwarded { get; set; }

    public bool IsAwarded { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionResult CompetitionResult { get; set; } = null!;

    public virtual CompetitionResultEntry CompetitionResultEntry { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual Participant Participant { get; set; } = null!;

    public virtual CategoryType ParticipantTahapCodeNavigation { get; set; } = null!;

    public virtual RegistrationEntryMember RegistrationEntryMember { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
