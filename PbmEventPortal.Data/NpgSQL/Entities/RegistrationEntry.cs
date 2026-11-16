using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class RegistrationEntry
{
    public Guid Id { get; set; }

    public Guid CompetitionCategoryId { get; set; }

    public Guid TeamId { get; set; }

    public string EntryType { get; set; } = null!;

    public string? EntryName { get; set; }

    public string EntryStatus { get; set; } = null!;

    public Guid? ApprovedBy { get; set; }

    public string? RejectedReason { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? ApprovedByNavigation { get; set; }

    public virtual CompetitionCategory CompetitionCategory { get; set; } = null!;

    public virtual ICollection<CompetitionResultEntry> CompetitionResultEntries { get; set; } = new List<CompetitionResultEntry>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<RegistrationEntryMember> RegistrationEntryMembers { get; set; } = new List<RegistrationEntryMember>();

    public virtual ICollection<ScoreEntry> ScoreEntries { get; set; } = new List<ScoreEntry>();

    public virtual Team Team { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
