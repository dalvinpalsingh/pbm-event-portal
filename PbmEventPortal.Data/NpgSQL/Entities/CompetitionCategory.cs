using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class CompetitionCategory
{
    public Guid Id { get; set; }

    public Guid CompetitionId { get; set; }

    public string CategoryCode { get; set; } = null!;

    public short SortOrder { get; set; }

    public short? IndividualEventMaxParticipants { get; set; }

    public short? TeamEventMaxEntriesPerTeam { get; set; }

    public short? TeamEventTeamSize { get; set; }

    public string? TeamEventGenderRestrictions { get; set; }

    public bool? TeamEventYoungerSubstitutionAllowed { get; set; }

    public short WinnerCount { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string EventType { get; set; } = null!;

    public virtual CategoryType CategoryCodeNavigation { get; set; } = null!;

    public virtual Competition Competition { get; set; } = null!;

    public virtual CompetitionResult? CompetitionResult { get; set; }

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<JudgeAssignment> JudgeAssignments { get; set; } = new List<JudgeAssignment>();

    public virtual ICollection<JudgeConflict> JudgeConflicts { get; set; } = new List<JudgeConflict>();

    public virtual ICollection<RegistrationEntry> RegistrationEntries { get; set; } = new List<RegistrationEntry>();

    public virtual ICollection<ScheduleItem> ScheduleItems { get; set; } = new List<ScheduleItem>();

    public virtual ScoreSheet? ScoreSheet { get; set; }

    public virtual ICollection<TeamCompositionPattern> TeamCompositionPatterns { get; set; } = new List<TeamCompositionPattern>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
