using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class Participant
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public Guid TeamId { get; set; }

    public string FullNameAsPerId { get; set; } = null!;

    public string FullNameGurmukhi { get; set; } = null!;

    public string IdType { get; set; } = null!;

    public string? NricNumber { get; set; }

    public string? PassportNumber { get; set; }

    public DateOnly? PassportExpiryDate { get; set; }

    public string? MobileNumber { get; set; }

    public string? Email { get; set; }

    public string ParentGuardianName { get; set; } = null!;

    public string ParentGuardianMobile { get; set; } = null!;

    public char Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string TahapCode { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipants { get; set; } = new List<CompetitionResultParticipant>();

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<JudgeConflict> JudgeConflicts { get; set; } = new List<JudgeConflict>();

    public virtual ICollection<OverallTahapResult> OverallTahapResults { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<RegistrationEntryMember> RegistrationEntryMembers { get; set; } = new List<RegistrationEntryMember>();

    public virtual CategoryType TahapCodeNavigation { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual TeamParticipantRegistrationDraft? TeamParticipantRegistrationDraft { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
