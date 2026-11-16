using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CompetitionResultEntry
{
    public Guid Id { get; set; }

    public Guid CompetitionResultId { get; set; }

    public Guid ScoreEntryId { get; set; }

    public Guid RegistrationEntryId { get; set; }

    public Guid TeamId { get; set; }

    public string EntryType { get; set; } = null!;

    public int RankNo { get; set; }

    public int PlacingNo { get; set; }

    public decimal EntryTotalScore { get; set; }

    public int PointsAwarded { get; set; }

    public bool IsAwarded { get; set; }

    public string? TieBreakerValuesJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionResult CompetitionResult { get; set; } = null!;

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipants { get; set; } = new List<CompetitionResultParticipant>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual RegistrationEntry RegistrationEntry { get; set; } = null!;

    public virtual ScoreEntry ScoreEntry { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
