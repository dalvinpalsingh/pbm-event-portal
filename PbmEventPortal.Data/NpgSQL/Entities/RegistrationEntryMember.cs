using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class RegistrationEntryMember
{
    public Guid Id { get; set; }

    public Guid RegistrationEntryId { get; set; }

    public Guid ParticipantId { get; set; }

    public short MemberOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipants { get; set; } = new List<CompetitionResultParticipant>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual Participant Participant { get; set; } = null!;

    public virtual RegistrationEntry RegistrationEntry { get; set; } = null!;

    public virtual ICollection<ScoreEntryScore> ScoreEntryScores { get; set; } = new List<ScoreEntryScore>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
