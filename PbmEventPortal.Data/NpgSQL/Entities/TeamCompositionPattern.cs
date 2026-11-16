using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class TeamCompositionPattern
{
    public Guid Id { get; set; }

    public Guid CompetitionCategoryId { get; set; }

    public string PatternCode { get; set; } = null!;

    public bool AllowYoungerSubstitution { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionCategory CompetitionCategory { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<TeamCompositionPatternSlot> TeamCompositionPatternSlots { get; set; } = new List<TeamCompositionPatternSlot>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
