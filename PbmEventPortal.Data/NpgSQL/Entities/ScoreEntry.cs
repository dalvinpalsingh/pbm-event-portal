using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class ScoreEntry
{
    public Guid Id { get; set; }

    public Guid ScoreSheetId { get; set; }

    public Guid RegistrationEntryId { get; set; }

    public decimal EntryTotalScore { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<CompetitionResultEntry> CompetitionResultEntries { get; set; } = new List<CompetitionResultEntry>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual RegistrationEntry RegistrationEntry { get; set; } = null!;

    public virtual ICollection<ScoreEntryScore> ScoreEntryScores { get; set; } = new List<ScoreEntryScore>();

    public virtual ScoreSheet ScoreSheet { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
