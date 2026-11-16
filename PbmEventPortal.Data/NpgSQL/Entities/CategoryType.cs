using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CategoryType
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string GroupingType { get; set; } = null!;

    public short? MinAge { get; set; }

    public short? MaxAge { get; set; }

    public short SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<CompetitionCategory> CompetitionCategories { get; set; } = new List<CompetitionCategory>();

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipants { get; set; } = new List<CompetitionResultParticipant>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<OverallTahapResult> OverallTahapResults { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();

    public virtual ICollection<TeamCompositionPatternSlot> TeamCompositionPatternSlots { get; set; } = new List<TeamCompositionPatternSlot>();

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDrafts { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
