using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = null!;

    public Guid? EntityId { get; set; }

    public string ActionName { get; set; } = null!;

    public Guid? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ChangeReason { get; set; }

    public string? OldValuesJson { get; set; }

    public string? NewValuesJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? ChangedByNavigation { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
