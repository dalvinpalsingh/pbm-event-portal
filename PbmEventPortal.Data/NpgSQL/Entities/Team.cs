using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class Team
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string NameEnglish { get; set; } = null!;

    public string? NameGurmukhi { get; set; }

    public Guid StateId { get; set; }

    public Guid RegionId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<CompetitionResultEntry> CompetitionResultEntries { get; set; } = new List<CompetitionResultEntry>();

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipants { get; set; } = new List<CompetitionResultParticipant>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<JudgeConflict> JudgeConflicts { get; set; } = new List<JudgeConflict>();

    public virtual ICollection<OverallTahapResult> OverallTahapResults { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<Participant> Participants { get; set; } = new List<Participant>();

    public virtual Region Region { get; set; } = null!;

    public virtual ICollection<RegistrationEntry> RegistrationEntries { get; set; } = new List<RegistrationEntry>();

    public virtual State State { get; set; } = null!;

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDrafts { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; } = new List<UserRoleAssignment>();
}
