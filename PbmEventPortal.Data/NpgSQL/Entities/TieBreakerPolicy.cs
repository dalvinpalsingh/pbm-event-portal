using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class TieBreakerPolicy
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public string PolicyName { get; set; } = null!;

    public string AppliesTo { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<OverallTahapResult> OverallTahapResults { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<TieBreakerPolicyRule> TieBreakerPolicyRules { get; set; } = new List<TieBreakerPolicyRule>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
