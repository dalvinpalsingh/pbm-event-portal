using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class TeamCompositionPatternSlot
{
    public Guid Id { get; set; }

    public Guid TeamCompositionPatternId { get; set; }

    public short SlotNo { get; set; }

    public string TahapCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual CategoryType TahapCodeNavigation { get; set; } = null!;

    public virtual TeamCompositionPattern TeamCompositionPattern { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
