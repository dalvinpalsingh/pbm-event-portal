using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class TieBreakerPolicyRule
{
    public Guid Id { get; set; }

    public Guid PolicyId { get; set; }

    public short PriorityNo { get; set; }

    public string RuleCode { get; set; } = null!;

    public string SortDirection { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual TieBreakerPolicy Policy { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
