using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class UserProfile
{
    public Guid Id { get; set; }

    public string FullNameAsPerId { get; set; } = null!;

    public string FullNameGurmukhi { get; set; } = null!;

    public string IdType { get; set; } = null!;

    public string? NricNumber { get; set; }

    public string? PassportNumber { get; set; }

    public DateOnly? PassportExpiryDate { get; set; }

    public string MobileNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public char Gender { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ICollection<AuditLog> AuditLogChangedByNavigations { get; set; } = new List<AuditLog>();

    public virtual ICollection<AuditLog> AuditLogCreatedByNavigations { get; set; } = new List<AuditLog>();

    public virtual ICollection<AuditLog> AuditLogUpdatedByNavigations { get; set; } = new List<AuditLog>();

    public virtual ICollection<CategoryType> CategoryTypeCreatedByNavigations { get; set; } = new List<CategoryType>();

    public virtual ICollection<CategoryType> CategoryTypeUpdatedByNavigations { get; set; } = new List<CategoryType>();

    public virtual ICollection<CompetitionCategory> CompetitionCategoryCreatedByNavigations { get; set; } = new List<CompetitionCategory>();

    public virtual ICollection<CompetitionCategory> CompetitionCategoryUpdatedByNavigations { get; set; } = new List<CompetitionCategory>();

    public virtual ICollection<Competition> CompetitionCreatedByNavigations { get; set; } = new List<Competition>();

    public virtual ICollection<CompetitionResult> CompetitionResultCalculatedByNavigations { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<CompetitionResult> CompetitionResultCreatedByNavigations { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<CompetitionResultEntry> CompetitionResultEntryCreatedByNavigations { get; set; } = new List<CompetitionResultEntry>();

    public virtual ICollection<CompetitionResultEntry> CompetitionResultEntryUpdatedByNavigations { get; set; } = new List<CompetitionResultEntry>();

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipantCreatedByNavigations { get; set; } = new List<CompetitionResultParticipant>();

    public virtual ICollection<CompetitionResultParticipant> CompetitionResultParticipantUpdatedByNavigations { get; set; } = new List<CompetitionResultParticipant>();

    public virtual ICollection<CompetitionResult> CompetitionResultScoreSheetVerifiedByNavigations { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<CompetitionResult> CompetitionResultUpdatedByNavigations { get; set; } = new List<CompetitionResult>();

    public virtual ICollection<CompetitionScoringCriterion> CompetitionScoringCriterionCreatedByNavigations { get; set; } = new List<CompetitionScoringCriterion>();

    public virtual ICollection<CompetitionScoringCriterion> CompetitionScoringCriterionUpdatedByNavigations { get; set; } = new List<CompetitionScoringCriterion>();

    public virtual ICollection<Competition> CompetitionUpdatedByNavigations { get; set; } = new List<Competition>();

    public virtual ICollection<CompetitionYear> CompetitionYearCreatedByNavigations { get; set; } = new List<CompetitionYear>();

    public virtual ICollection<CompetitionYear> CompetitionYearUpdatedByNavigations { get; set; } = new List<CompetitionYear>();

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual ICollection<UserProfile> InverseCreatedByNavigation { get; set; } = new List<UserProfile>();

    public virtual ICollection<UserProfile> InverseUpdatedByNavigation { get; set; } = new List<UserProfile>();

    public virtual ICollection<JudgeAssignment> JudgeAssignmentCreatedByNavigations { get; set; } = new List<JudgeAssignment>();

    public virtual ICollection<JudgeAssignment> JudgeAssignmentJudgeUsers { get; set; } = new List<JudgeAssignment>();

    public virtual ICollection<JudgeAssignment> JudgeAssignmentUpdatedByNavigations { get; set; } = new List<JudgeAssignment>();

    public virtual ICollection<JudgeConflict> JudgeConflictCreatedByNavigations { get; set; } = new List<JudgeConflict>();

    public virtual ICollection<JudgeConflict> JudgeConflictJudgeUsers { get; set; } = new List<JudgeConflict>();

    public virtual ICollection<JudgeConflict> JudgeConflictUpdatedByNavigations { get; set; } = new List<JudgeConflict>();

    public virtual ICollection<OverallTahapResult> OverallTahapResultCreatedByNavigations { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<OverallTahapResult> OverallTahapResultUpdatedByNavigations { get; set; } = new List<OverallTahapResult>();

    public virtual ICollection<Participant> ParticipantCreatedByNavigations { get; set; } = new List<Participant>();

    public virtual ICollection<Participant> ParticipantUpdatedByNavigations { get; set; } = new List<Participant>();

    public virtual ICollection<PasswordBlocklist> PasswordBlocklistCreatedByNavigations { get; set; } = new List<PasswordBlocklist>();

    public virtual ICollection<PasswordBlocklist> PasswordBlocklistUpdatedByNavigations { get; set; } = new List<PasswordBlocklist>();

    public virtual ICollection<PointsScheme> PointsSchemeCreatedByNavigations { get; set; } = new List<PointsScheme>();

    public virtual ICollection<PointsSchemeItem> PointsSchemeItemCreatedByNavigations { get; set; } = new List<PointsSchemeItem>();

    public virtual ICollection<PointsSchemeItem> PointsSchemeItemUpdatedByNavigations { get; set; } = new List<PointsSchemeItem>();

    public virtual ICollection<PointsScheme> PointsSchemeUpdatedByNavigations { get; set; } = new List<PointsScheme>();

    public virtual ICollection<Region> RegionCreatedByNavigations { get; set; } = new List<Region>();

    public virtual ICollection<Region> RegionUpdatedByNavigations { get; set; } = new List<Region>();

    public virtual ICollection<RegistrationEntry> RegistrationEntryApprovedByNavigations { get; set; } = new List<RegistrationEntry>();

    public virtual ICollection<RegistrationEntry> RegistrationEntryCreatedByNavigations { get; set; } = new List<RegistrationEntry>();

    public virtual ICollection<RegistrationEntryMember> RegistrationEntryMemberCreatedByNavigations { get; set; } = new List<RegistrationEntryMember>();

    public virtual ICollection<RegistrationEntryMember> RegistrationEntryMemberUpdatedByNavigations { get; set; } = new List<RegistrationEntryMember>();

    public virtual ICollection<RegistrationEntry> RegistrationEntryUpdatedByNavigations { get; set; } = new List<RegistrationEntry>();

    public virtual ICollection<ResultCalculationRun> ResultCalculationRunCalculatedByNavigations { get; set; } = new List<ResultCalculationRun>();

    public virtual ICollection<ResultCalculationRun> ResultCalculationRunCreatedByNavigations { get; set; } = new List<ResultCalculationRun>();

    public virtual ICollection<ResultCalculationRun> ResultCalculationRunUpdatedByNavigations { get; set; } = new List<ResultCalculationRun>();

    public virtual ICollection<RoleType> RoleTypeCreatedByNavigations { get; set; } = new List<RoleType>();

    public virtual ICollection<RoleType> RoleTypeUpdatedByNavigations { get; set; } = new List<RoleType>();

    public virtual ICollection<ScheduleItem> ScheduleItemCreatedByNavigations { get; set; } = new List<ScheduleItem>();

    public virtual ICollection<ScheduleItem> ScheduleItemUpdatedByNavigations { get; set; } = new List<ScheduleItem>();

    public virtual ICollection<ScoreEntry> ScoreEntryCreatedByNavigations { get; set; } = new List<ScoreEntry>();

    public virtual ICollection<ScoreEntryScore> ScoreEntryScoreChangeVerifiedByNavigations { get; set; } = new List<ScoreEntryScore>();

    public virtual ICollection<ScoreEntryScore> ScoreEntryScoreCreatedByNavigations { get; set; } = new List<ScoreEntryScore>();

    public virtual ICollection<ScoreEntryScore> ScoreEntryScoreOverrideByNavigations { get; set; } = new List<ScoreEntryScore>();

    public virtual ICollection<ScoreEntryScore> ScoreEntryScoreUpdatedByNavigations { get; set; } = new List<ScoreEntryScore>();

    public virtual ICollection<ScoreEntry> ScoreEntryUpdatedByNavigations { get; set; } = new List<ScoreEntry>();

    public virtual ICollection<ScoreSheet> ScoreSheetCreatedByNavigations { get; set; } = new List<ScoreSheet>();

    public virtual ICollection<ScoreSheet> ScoreSheetSubmittedByNavigations { get; set; } = new List<ScoreSheet>();

    public virtual ICollection<ScoreSheet> ScoreSheetUpdatedByNavigations { get; set; } = new List<ScoreSheet>();

    public virtual ICollection<ScoreSheet> ScoreSheetVerifiedByNavigations { get; set; } = new List<ScoreSheet>();

    public virtual ICollection<State> StateCreatedByNavigations { get; set; } = new List<State>();

    public virtual ICollection<State> StateUpdatedByNavigations { get; set; } = new List<State>();

    public virtual ICollection<TeamCompositionPattern> TeamCompositionPatternCreatedByNavigations { get; set; } = new List<TeamCompositionPattern>();

    public virtual ICollection<TeamCompositionPatternSlot> TeamCompositionPatternSlotCreatedByNavigations { get; set; } = new List<TeamCompositionPatternSlot>();

    public virtual ICollection<TeamCompositionPatternSlot> TeamCompositionPatternSlotUpdatedByNavigations { get; set; } = new List<TeamCompositionPatternSlot>();

    public virtual ICollection<TeamCompositionPattern> TeamCompositionPatternUpdatedByNavigations { get; set; } = new List<TeamCompositionPattern>();

    public virtual ICollection<Team> TeamCreatedByNavigations { get; set; } = new List<Team>();

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDraftCancelledByNavigations { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDraftCreatedByNavigations { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDraftSubmittedByNavigations { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual ICollection<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDraftUpdatedByNavigations { get; set; } = new List<TeamParticipantRegistrationDraft>();

    public virtual ICollection<Team> TeamUpdatedByNavigations { get; set; } = new List<Team>();

    public virtual ICollection<TieBreakerPolicy> TieBreakerPolicyCreatedByNavigations { get; set; } = new List<TieBreakerPolicy>();

    public virtual ICollection<TieBreakerPolicyRule> TieBreakerPolicyRuleCreatedByNavigations { get; set; } = new List<TieBreakerPolicyRule>();

    public virtual ICollection<TieBreakerPolicyRule> TieBreakerPolicyRuleUpdatedByNavigations { get; set; } = new List<TieBreakerPolicyRule>();

    public virtual ICollection<TieBreakerPolicy> TieBreakerPolicyUpdatedByNavigations { get; set; } = new List<TieBreakerPolicy>();

    public virtual UserProfile? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserCredential> UserCredentialCreatedByNavigations { get; set; } = new List<UserCredential>();

    public virtual ICollection<UserCredential> UserCredentialHardLockedByNavigations { get; set; } = new List<UserCredential>();

    public virtual ICollection<UserCredential> UserCredentialUpdatedByNavigations { get; set; } = new List<UserCredential>();

    public virtual UserCredential? UserCredentialUser { get; set; }

    public virtual ICollection<UserPasswordHistory> UserPasswordHistoryChangedByNavigations { get; set; } = new List<UserPasswordHistory>();

    public virtual ICollection<UserPasswordHistory> UserPasswordHistoryCreatedByNavigations { get; set; } = new List<UserPasswordHistory>();

    public virtual ICollection<UserPasswordHistory> UserPasswordHistoryUpdatedByNavigations { get; set; } = new List<UserPasswordHistory>();

    public virtual ICollection<UserPasswordHistory> UserPasswordHistoryUsers { get; set; } = new List<UserPasswordHistory>();

    public virtual ICollection<UserRoleAssignment> UserRoleAssignmentCreatedByNavigations { get; set; } = new List<UserRoleAssignment>();

    public virtual ICollection<UserRoleAssignment> UserRoleAssignmentUpdatedByNavigations { get; set; } = new List<UserRoleAssignment>();

    public virtual ICollection<UserRoleAssignment> UserRoleAssignmentUsers { get; set; } = new List<UserRoleAssignment>();

    public virtual ICollection<Venue> VenueCreatedByNavigations { get; set; } = new List<Venue>();

    public virtual ICollection<Venue> VenueUpdatedByNavigations { get; set; } = new List<Venue>();
}
