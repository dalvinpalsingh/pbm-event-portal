using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class JudgeConflict
{
    public Guid Id { get; set; }

    public Guid JudgeUserId { get; set; }

    public string ConflictType { get; set; } = null!;

    public Guid? TeamId { get; set; }

    public Guid? ParticipantId { get; set; }

    public Guid? CompetitionCategoryId { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionCategory? CompetitionCategory { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile JudgeUser { get; set; } = null!;

    public virtual Participant? Participant { get; set; }

    public virtual Team? Team { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
