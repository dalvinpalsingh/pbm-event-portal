using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class PointsSchemeItem
{
    public Guid Id { get; set; }

    public Guid PointsSchemeId { get; set; }

    public short PlacingNo { get; set; }

    public short PointsAwarded { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual PointsScheme PointsScheme { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
