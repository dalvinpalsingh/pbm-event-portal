using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class UserRoleAssignment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string RoleCode { get; set; } = null!;

    public Guid? TeamId { get; set; }

    public bool IsVerified { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual RoleType RoleCodeNavigation { get; set; } = null!;

    public virtual Team? Team { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
