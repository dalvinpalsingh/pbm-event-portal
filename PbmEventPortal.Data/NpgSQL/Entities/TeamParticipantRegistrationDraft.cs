using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class TeamParticipantRegistrationDraft
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public Guid TeamId { get; set; }

    public string TahapCode { get; set; } = null!;

    public string DraftStatus { get; set; } = null!;

    public string DraftPayloadJson { get; set; } = null!;

    public string SelectedIndividualCompetitionCategoryIdsJson { get; set; } = null!;

    public string SelectedTeamCompetitionCategoryIdsJson { get; set; } = null!;

    public Guid? SubmittedParticipantId { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public Guid? SubmittedBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public Guid? CancelledBy { get; set; }

    public string? CancelledReason { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual UserProfile? CancelledByNavigation { get; set; }

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual UserProfile? SubmittedByNavigation { get; set; }

    public virtual Participant? SubmittedParticipant { get; set; }

    public virtual CategoryType TahapCodeNavigation { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
