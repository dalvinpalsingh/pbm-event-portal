using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class PasswordBlocklist
{
    public Guid Id { get; set; }

    public string BlockedValue { get; set; } = null!;

    public string NormalizedValue { get; set; } = null!;

    public string? Reason { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
