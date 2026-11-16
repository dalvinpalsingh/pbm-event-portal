using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class UserPasswordHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public Guid? ChangedBy { get; set; }

    public string ChangeReason { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? ChangedByNavigation { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
