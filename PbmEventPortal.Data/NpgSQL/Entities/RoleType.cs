using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class RoleType
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string RoleScope { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? Description { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; } = new List<UserRoleAssignment>();
}
