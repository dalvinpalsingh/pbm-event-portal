using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class UserCredential
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public bool MustChangePassword { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastPasswordChangedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public int SoftLockCount { get; set; }

    public DateTime? LastSoftLockedAt { get; set; }

    public DateTime? HardLockedAt { get; set; }

    public Guid? HardLockedBy { get; set; }

    public string? HardLockReason { get; set; }

    public bool RequiresAdminUnlock { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile? HardLockedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
