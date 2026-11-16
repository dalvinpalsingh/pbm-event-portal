using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL.Entities;

namespace PbmEventPortal.Data.NpgSQL;

public partial class PbmDbContext : DbContext
{
    public PbmDbContext(DbContextOptions<PbmDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CategoryType> CategoryTypes { get; set; }

    public virtual DbSet<Competition> Competitions { get; set; }

    public virtual DbSet<CompetitionCategory> CompetitionCategories { get; set; }

    public virtual DbSet<CompetitionResult> CompetitionResults { get; set; }

    public virtual DbSet<CompetitionResultEntry> CompetitionResultEntries { get; set; }

    public virtual DbSet<CompetitionResultParticipant> CompetitionResultParticipants { get; set; }

    public virtual DbSet<CompetitionScoringCriterion> CompetitionScoringCriteria { get; set; }

    public virtual DbSet<CompetitionYear> CompetitionYears { get; set; }

    public virtual DbSet<JudgeAssignment> JudgeAssignments { get; set; }

    public virtual DbSet<JudgeConflict> JudgeConflicts { get; set; }

    public virtual DbSet<OverallTahapResult> OverallTahapResults { get; set; }

    public virtual DbSet<Participant> Participants { get; set; }

    public virtual DbSet<PasswordBlocklist> PasswordBlocklists { get; set; }

    public virtual DbSet<PointsScheme> PointsSchemes { get; set; }

    public virtual DbSet<PointsSchemeItem> PointsSchemeItems { get; set; }

    public virtual DbSet<Region> Regions { get; set; }

    public virtual DbSet<RegistrationEntry> RegistrationEntries { get; set; }

    public virtual DbSet<RegistrationEntryMember> RegistrationEntryMembers { get; set; }

    public virtual DbSet<ResultCalculationRun> ResultCalculationRuns { get; set; }

    public virtual DbSet<RoleType> RoleTypes { get; set; }

    public virtual DbSet<ScheduleItem> ScheduleItems { get; set; }

    public virtual DbSet<ScoreEntry> ScoreEntries { get; set; }

    public virtual DbSet<ScoreEntryScore> ScoreEntryScores { get; set; }

    public virtual DbSet<ScoreSheet> ScoreSheets { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamCompositionPattern> TeamCompositionPatterns { get; set; }

    public virtual DbSet<TeamCompositionPatternSlot> TeamCompositionPatternSlots { get; set; }

    public virtual DbSet<TeamParticipantRegistrationDraft> TeamParticipantRegistrationDrafts { get; set; }

    public virtual DbSet<TieBreakerPolicy> TieBreakerPolicies { get; set; }

    public virtual DbSet<TieBreakerPolicyRule> TieBreakerPolicyRules { get; set; }

    public virtual DbSet<UserCredential> UserCredentials { get; set; }

    public virtual DbSet<UserPasswordHistory> UserPasswordHistories { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }

    public virtual DbSet<Venue> Venues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs", "app");

            entity.HasIndex(e => e.ActionName, "ix_audit_logs_action_name");

            entity.HasIndex(e => e.ChangedAt, "ix_audit_logs_changed_at");

            entity.HasIndex(e => e.ChangedBy, "ix_audit_logs_changed_by");

            entity.HasIndex(e => new { e.ChangedBy, e.EntityName, e.ChangedAt }, "ix_audit_logs_changed_by_entity_changed_at");

            entity.HasIndex(e => e.CreatedBy, "ix_audit_logs_created_by");

            entity.HasIndex(e => e.EntityId, "ix_audit_logs_entity_id");

            entity.HasIndex(e => e.EntityName, "ix_audit_logs_entity_name");

            entity.HasIndex(e => new { e.EntityName, e.EntityId, e.ChangedAt }, "ix_audit_logs_entity_name_entity_id_changed_at");

            entity.HasIndex(e => e.UpdatedBy, "ix_audit_logs_updated_by");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActionName)
                .HasMaxLength(50)
                .HasColumnName("action_name");
            entity.Property(e => e.ChangeReason).HasColumnName("change_reason");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .HasColumnName("entity_name");
            entity.Property(e => e.NewValuesJson)
                .HasColumnType("jsonb")
                .HasColumnName("new_values_json");
            entity.Property(e => e.OldValuesJson)
                .HasColumnType("jsonb")
                .HasColumnName("old_values_json");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.AuditLogChangedByNavigations)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("fk_audit_logs_changed_by_user");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AuditLogCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_audit_logs_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.AuditLogUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_audit_logs_updated_by_user");
        });

        modelBuilder.Entity<CategoryType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("category_types_pkey");

            entity.ToTable("category_types", "app");

            entity.HasIndex(e => e.Name, "category_types_name_key").IsUnique();

            entity.HasIndex(e => e.CreatedBy, "ix_category_types_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_category_types_updated_by");

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.GroupingType)
                .HasMaxLength(20)
                .HasColumnName("grouping_type");
            entity.Property(e => e.MaxAge).HasColumnName("max_age");
            entity.Property(e => e.MinAge).HasColumnName("min_age");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CategoryTypeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_category_types_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CategoryTypeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_category_types_updated_by_user");
        });

        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competitions_pkey");

            entity.ToTable("competitions", "app");

            entity.HasIndex(e => e.CompetitionType, "ix_competitions_competition_type");

            entity.HasIndex(e => e.CompetitionYearId, "ix_competitions_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_competitions_created_by");

            entity.HasIndex(e => e.PointsSchemeId, "ix_competitions_points_scheme_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_competitions_updated_by");

            entity.HasIndex(e => new { e.CompetitionYearId, e.Code }, "uq_competitions_year_code").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .HasColumnName("code");
            entity.Property(e => e.CompetitionType)
                .HasMaxLength(20)
                .HasColumnName("competition_type");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CriteriaCount).HasColumnName("criteria_count");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxJudges)
                .HasDefaultValue((short)3)
                .HasColumnName("max_judges");
            entity.Property(e => e.MinJudges)
                .HasDefaultValue((short)2)
                .HasColumnName("min_judges");
            entity.Property(e => e.NameEnglish)
                .HasMaxLength(200)
                .HasColumnName("name_english");
            entity.Property(e => e.NameGurmukhi)
                .HasMaxLength(200)
                .HasColumnName("name_gurmukhi");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PointsSchemeId).HasColumnName("points_scheme_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.Competitions)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competitions_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competitions_created_by_user");

            entity.HasOne(d => d.PointsScheme).WithMany(p => p.Competitions)
                .HasForeignKey(d => d.PointsSchemeId)
                .HasConstraintName("fk_competitions_points_scheme");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competitions_updated_by_user");
        });

        modelBuilder.Entity<CompetitionCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competition_categories_pkey");

            entity.ToTable("competition_categories", "app");

            entity.HasIndex(e => e.CategoryCode, "ix_competition_categories_category_code");

            entity.HasIndex(e => e.CompetitionId, "ix_competition_categories_competition_id");

            entity.HasIndex(e => e.CreatedBy, "ix_competition_categories_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_competition_categories_updated_by");

            entity.HasIndex(e => new { e.CompetitionId, e.CategoryCode }, "uq_competition_categories_competition_category").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(20)
                .HasColumnName("category_code");
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EventType)
                .HasMaxLength(20)
                .HasColumnName("event_type");
            entity.Property(e => e.IndividualEventMaxParticipants).HasColumnName("individual_event_max_participants");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.SortOrder)
                .HasDefaultValue((short)1)
                .HasColumnName("sort_order");
            entity.Property(e => e.TeamEventGenderRestrictions)
                .HasMaxLength(30)
                .HasColumnName("team_event_gender_restrictions");
            entity.Property(e => e.TeamEventMaxEntriesPerTeam).HasColumnName("team_event_max_entries_per_team");
            entity.Property(e => e.TeamEventTeamSize).HasColumnName("team_event_team_size");
            entity.Property(e => e.TeamEventYoungerSubstitutionAllowed).HasColumnName("team_event_younger_substitution_allowed");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.WinnerCount).HasColumnName("winner_count");

            entity.HasOne(d => d.CategoryCodeNavigation).WithMany(p => p.CompetitionCategories)
                .HasForeignKey(d => d.CategoryCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_categories_category_code_fkey");

            entity.HasOne(d => d.Competition).WithMany(p => p.CompetitionCategories)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_categories_competition_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionCategoryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competition_categories_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionCategoryUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competition_categories_updated_by_user");
        });

        modelBuilder.Entity<CompetitionResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competition_results_pkey");

            entity.ToTable("competition_results", "app");

            entity.HasIndex(e => e.CalculatedAt, "ix_competition_results_calculated_at");

            entity.HasIndex(e => e.CalculatedBy, "ix_competition_results_calculated_by");

            entity.HasIndex(e => e.CalculationRunId, "ix_competition_results_calculation_run_id");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_competition_results_competition_category_id");

            entity.HasIndex(e => e.CompetitionId, "ix_competition_results_competition_id");

            entity.HasIndex(e => e.CompetitionYearId, "ix_competition_results_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_competition_results_created_by");

            entity.HasIndex(e => e.PointsSchemeId, "ix_competition_results_points_scheme_id");

            entity.HasIndex(e => e.ScoreSheetId, "ix_competition_results_score_sheet_id");

            entity.HasIndex(e => e.ResultStatus, "ix_competition_results_status");

            entity.HasIndex(e => e.UpdatedBy, "ix_competition_results_updated_by");

            entity.HasIndex(e => new { e.CalculationRunId, e.CompetitionCategoryId }, "uq_competition_results_run_category").IsUnique();

            entity.HasIndex(e => new { e.CalculationRunId, e.ScoreSheetId }, "uq_competition_results_run_score_sheet").IsUnique();

            entity.HasIndex(e => e.CompetitionCategoryId, "ux_competition_results_current_category")
                .IsUnique()
                .HasFilter("(is_current = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AwardedEntryCount)
                .HasDefaultValue(0)
                .HasColumnName("awarded_entry_count");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("calculated_at");
            entity.Property(e => e.CalculatedBy).HasColumnName("calculated_by");
            entity.Property(e => e.CalculationRunId).HasColumnName("calculation_run_id");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EligibleEntryCount)
                .HasDefaultValue(0)
                .HasColumnName("eligible_entry_count");
            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(true)
                .HasColumnName("is_current");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PointsSchemeId).HasColumnName("points_scheme_id");
            entity.Property(e => e.ResultStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'CALCULATED'::character varying")
                .HasColumnName("result_status");
            entity.Property(e => e.ScoreSheetId).HasColumnName("score_sheet_id");
            entity.Property(e => e.ScoreSheetVerifiedAt).HasColumnName("score_sheet_verified_at");
            entity.Property(e => e.ScoreSheetVerifiedBy).HasColumnName("score_sheet_verified_by");
            entity.Property(e => e.ScoreSheetVersionNo).HasColumnName("score_sheet_version_no");
            entity.Property(e => e.SourceScoreSheetStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'VERIFIED'::character varying")
                .HasColumnName("source_score_sheet_status");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.WinnerCount).HasColumnName("winner_count");

            entity.HasOne(d => d.CalculatedByNavigation).WithMany(p => p.CompetitionResultCalculatedByNavigations)
                .HasForeignKey(d => d.CalculatedBy)
                .HasConstraintName("fk_competition_results_calculated_by_user");

            entity.HasOne(d => d.CalculationRun).WithMany(p => p.CompetitionResults)
                .HasForeignKey(d => d.CalculationRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_results_calculation_run_id_fkey");

            entity.HasOne(d => d.CompetitionCategory).WithOne(p => p.CompetitionResult)
                .HasForeignKey<CompetitionResult>(d => d.CompetitionCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_results_competition_category_id_fkey");

            entity.HasOne(d => d.Competition).WithMany(p => p.CompetitionResults)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_results_competition_id_fkey");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.CompetitionResults)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_results_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionResultCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competition_results_created_by_user");

            entity.HasOne(d => d.PointsScheme).WithMany(p => p.CompetitionResults)
                .HasForeignKey(d => d.PointsSchemeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_results_points_scheme_id_fkey");

            entity.HasOne(d => d.ScoreSheet).WithMany(p => p.CompetitionResults)
                .HasForeignKey(d => d.ScoreSheetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_results_score_sheet_id_fkey");

            entity.HasOne(d => d.ScoreSheetVerifiedByNavigation).WithMany(p => p.CompetitionResultScoreSheetVerifiedByNavigations)
                .HasForeignKey(d => d.ScoreSheetVerifiedBy)
                .HasConstraintName("fk_competition_results_verified_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionResultUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competition_results_updated_by_user");
        });

        modelBuilder.Entity<CompetitionResultEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competition_result_entries_pkey");

            entity.ToTable("competition_result_entries", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_competition_result_entries_created_by");

            entity.HasIndex(e => e.EntryType, "ix_competition_result_entries_entry_type");

            entity.HasIndex(e => e.IsAwarded, "ix_competition_result_entries_is_awarded");

            entity.HasIndex(e => e.RankNo, "ix_competition_result_entries_rank_no");

            entity.HasIndex(e => e.RegistrationEntryId, "ix_competition_result_entries_reg_entry_id");

            entity.HasIndex(e => e.CompetitionResultId, "ix_competition_result_entries_result_id");

            entity.HasIndex(e => e.ScoreEntryId, "ix_competition_result_entries_score_entry_id");

            entity.HasIndex(e => e.TeamId, "ix_competition_result_entries_team_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_competition_result_entries_updated_by");

            entity.HasIndex(e => new { e.CompetitionResultId, e.RegistrationEntryId }, "uq_competition_result_entries_result_reg_entry").IsUnique();

            entity.HasIndex(e => new { e.CompetitionResultId, e.ScoreEntryId }, "uq_competition_result_entries_result_score_entry").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionResultId).HasColumnName("competition_result_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EntryTotalScore)
                .HasPrecision(10, 2)
                .HasColumnName("entry_total_score");
            entity.Property(e => e.EntryType)
                .HasMaxLength(20)
                .HasColumnName("entry_type");
            entity.Property(e => e.IsAwarded)
                .HasDefaultValue(false)
                .HasColumnName("is_awarded");
            entity.Property(e => e.PlacingNo).HasColumnName("placing_no");
            entity.Property(e => e.PointsAwarded)
                .HasDefaultValue(0)
                .HasColumnName("points_awarded");
            entity.Property(e => e.RankNo).HasColumnName("rank_no");
            entity.Property(e => e.RegistrationEntryId).HasColumnName("registration_entry_id");
            entity.Property(e => e.ScoreEntryId).HasColumnName("score_entry_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.TieBreakerValuesJson)
                .HasColumnType("jsonb")
                .HasColumnName("tie_breaker_values_json");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionResult).WithMany(p => p.CompetitionResultEntries)
                .HasForeignKey(d => d.CompetitionResultId)
                .HasConstraintName("competition_result_entries_result_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionResultEntryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competition_result_entries_created_by_user");

            entity.HasOne(d => d.RegistrationEntry).WithMany(p => p.CompetitionResultEntries)
                .HasForeignKey(d => d.RegistrationEntryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_result_entries_registration_entry_id_fkey");

            entity.HasOne(d => d.ScoreEntry).WithMany(p => p.CompetitionResultEntries)
                .HasForeignKey(d => d.ScoreEntryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_result_entries_score_entry_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.CompetitionResultEntries)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_result_entries_team_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionResultEntryUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competition_result_entries_updated_by_user");
        });

        modelBuilder.Entity<CompetitionResultParticipant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competition_result_participants_pkey");

            entity.ToTable("competition_result_participants", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_competition_result_participants_created_by");

            entity.HasIndex(e => e.RegistrationEntryMemberId, "ix_competition_result_participants_entry_member");

            entity.HasIndex(e => e.IsAwarded, "ix_competition_result_participants_is_awarded");

            entity.HasIndex(e => e.ParticipantId, "ix_competition_result_participants_participant_id");

            entity.HasIndex(e => e.CompetitionResultEntryId, "ix_competition_result_participants_result_entry");

            entity.HasIndex(e => e.CompetitionResultId, "ix_competition_result_participants_result_id");

            entity.HasIndex(e => e.ParticipantTahapCode, "ix_competition_result_participants_tahap_code");

            entity.HasIndex(e => e.TeamId, "ix_competition_result_participants_team_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_competition_result_participants_updated_by");

            entity.HasIndex(e => new { e.CompetitionResultEntryId, e.RegistrationEntryMemberId }, "uq_competition_result_participants_entry_member").IsUnique();

            entity.HasIndex(e => new { e.CompetitionResultEntryId, e.ParticipantId }, "uq_competition_result_participants_entry_participant").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionResultEntryId).HasColumnName("competition_result_entry_id");
            entity.Property(e => e.CompetitionResultId).HasColumnName("competition_result_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsAwarded)
                .HasDefaultValue(false)
                .HasColumnName("is_awarded");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.ParticipantTahapCode)
                .HasMaxLength(20)
                .HasColumnName("participant_tahap_code");
            entity.Property(e => e.PointsAwarded)
                .HasDefaultValue(0)
                .HasColumnName("points_awarded");
            entity.Property(e => e.RegistrationEntryMemberId).HasColumnName("registration_entry_member_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionResultEntry).WithMany(p => p.CompetitionResultParticipants)
                .HasForeignKey(d => d.CompetitionResultEntryId)
                .HasConstraintName("fk_competition_result_participants_result_entry");

            entity.HasOne(d => d.CompetitionResult).WithMany(p => p.CompetitionResultParticipants)
                .HasForeignKey(d => d.CompetitionResultId)
                .HasConstraintName("competition_result_participants_result_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionResultParticipantCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competition_result_participants_created_by_user");

            entity.HasOne(d => d.Participant).WithMany(p => p.CompetitionResultParticipants)
                .HasForeignKey(d => d.ParticipantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_result_participants_participant_id_fkey");

            entity.HasOne(d => d.ParticipantTahapCodeNavigation).WithMany(p => p.CompetitionResultParticipants)
                .HasForeignKey(d => d.ParticipantTahapCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_result_participants_tahap_code_fkey");

            entity.HasOne(d => d.RegistrationEntryMember).WithMany(p => p.CompetitionResultParticipants)
                .HasForeignKey(d => d.RegistrationEntryMemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_competition_result_participants_entry_member");

            entity.HasOne(d => d.Team).WithMany(p => p.CompetitionResultParticipants)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_result_participants_team_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionResultParticipantUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competition_result_participants_updated_by_user");
        });

        modelBuilder.Entity<CompetitionScoringCriterion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competition_scoring_criteria_pkey");

            entity.ToTable("competition_scoring_criteria", "app");

            entity.HasIndex(e => e.CompetitionId, "ix_competition_scoring_criteria_competition_id");

            entity.HasIndex(e => e.CreatedBy, "ix_competition_scoring_criteria_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_competition_scoring_criteria_updated_by");

            entity.HasIndex(e => new { e.CompetitionId, e.CriterionNo }, "uq_competition_scoring_criteria_competition_criterion_no").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionId).HasColumnName("competition_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CriterionName)
                .HasMaxLength(100)
                .HasColumnName("criterion_name");
            entity.Property(e => e.CriterionNo).HasColumnName("criterion_no");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxScore)
                .HasPrecision(10, 2)
                .HasColumnName("max_score");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Competition).WithMany(p => p.CompetitionScoringCriteria)
                .HasForeignKey(d => d.CompetitionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("competition_scoring_criteria_competition_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionScoringCriterionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competition_scoring_criteria_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionScoringCriterionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competition_scoring_criteria_updated_by_user");
        });

        modelBuilder.Entity<CompetitionYear>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("competition_years_pkey");

            entity.ToTable("competition_years", "app");

            entity.HasIndex(e => e.YearNo, "competition_years_year_no_key").IsUnique();

            entity.HasIndex(e => e.CreatedBy, "ix_competition_years_created_by");

            entity.HasIndex(e => e.DefaultPointsSchemeId, "ix_competition_years_default_points_scheme_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_competition_years_updated_by");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DefaultPointsSchemeId).HasColumnName("default_points_scheme_id");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(100)
                .HasColumnName("display_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RegistrationCloseAt).HasColumnName("registration_close_at");
            entity.Property(e => e.RegistrationOpenAt).HasColumnName("registration_open_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.YearNo).HasColumnName("year_no");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CompetitionYearCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_competition_years_created_by_user");

            entity.HasOne(d => d.DefaultPointsScheme).WithMany(p => p.CompetitionYears)
                .HasForeignKey(d => d.DefaultPointsSchemeId)
                .HasConstraintName("fk_competition_years_default_points_scheme");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.CompetitionYearUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_competition_years_updated_by_user");
        });

        modelBuilder.Entity<JudgeAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_assignments_pkey");

            entity.ToTable("judge_assignments", "app");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_judge_assignments_competition_category_id");

            entity.HasIndex(e => e.CreatedBy, "ix_judge_assignments_created_by");

            entity.HasIndex(e => e.CriterionId, "ix_judge_assignments_criterion_id");

            entity.HasIndex(e => e.JudgeUserId, "ix_judge_assignments_judge_user_id");

            entity.HasIndex(e => e.ScheduleItemId, "ix_judge_assignments_schedule_item_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_judge_assignments_updated_by");

            entity.HasIndex(e => new { e.CompetitionCategoryId, e.JudgeUserId, e.CriterionId }, "ux_judge_assignments_category_judge_criterion_no_schedule")
                .IsUnique()
                .HasFilter("((schedule_item_id IS NULL) AND (criterion_id IS NOT NULL))");

            entity.HasIndex(e => new { e.CompetitionCategoryId, e.JudgeUserId }, "ux_judge_assignments_category_judge_no_schedule")
                .IsUnique()
                .HasFilter("((schedule_item_id IS NULL) AND (criterion_id IS NULL))");

            entity.HasIndex(e => new { e.ScheduleItemId, e.JudgeUserId, e.CriterionId }, "ux_judge_assignments_schedule_judge_criterion")
                .IsUnique()
                .HasFilter("((schedule_item_id IS NOT NULL) AND (criterion_id IS NOT NULL))");

            entity.HasIndex(e => new { e.ScheduleItemId, e.JudgeUserId }, "ux_judge_assignments_schedule_judge_no_criterion")
                .IsUnique()
                .HasFilter("((schedule_item_id IS NOT NULL) AND (criterion_id IS NULL))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CriterionId).HasColumnName("criterion_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.JudgeUserId).HasColumnName("judge_user_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ScheduleItemId).HasColumnName("schedule_item_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionCategory).WithMany(p => p.JudgeAssignments)
                .HasForeignKey(d => d.CompetitionCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("judge_assignments_competition_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.JudgeAssignmentCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_judge_assignments_created_by_user");

            entity.HasOne(d => d.Criterion).WithMany(p => p.JudgeAssignments)
                .HasForeignKey(d => d.CriterionId)
                .HasConstraintName("judge_assignments_criterion_id_fkey");

            entity.HasOne(d => d.JudgeUser).WithMany(p => p.JudgeAssignmentJudgeUsers)
                .HasForeignKey(d => d.JudgeUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("judge_assignments_judge_user_id_fkey");

            entity.HasOne(d => d.ScheduleItem).WithMany(p => p.JudgeAssignments)
                .HasForeignKey(d => d.ScheduleItemId)
                .HasConstraintName("judge_assignments_schedule_item_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.JudgeAssignmentUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_judge_assignments_updated_by_user");
        });

        modelBuilder.Entity<JudgeConflict>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_conflicts_pkey");

            entity.ToTable("judge_conflicts", "app");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_judge_conflicts_competition_category_id");

            entity.HasIndex(e => e.CreatedBy, "ix_judge_conflicts_created_by");

            entity.HasIndex(e => e.JudgeUserId, "ix_judge_conflicts_judge_user_id");

            entity.HasIndex(e => e.ParticipantId, "ix_judge_conflicts_participant_id");

            entity.HasIndex(e => e.TeamId, "ix_judge_conflicts_team_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_judge_conflicts_updated_by");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.ConflictType)
                .HasMaxLength(30)
                .HasColumnName("conflict_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.JudgeUserId).HasColumnName("judge_user_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionCategory).WithMany(p => p.JudgeConflicts)
                .HasForeignKey(d => d.CompetitionCategoryId)
                .HasConstraintName("judge_conflicts_competition_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.JudgeConflictCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_judge_conflicts_created_by_user");

            entity.HasOne(d => d.JudgeUser).WithMany(p => p.JudgeConflictJudgeUsers)
                .HasForeignKey(d => d.JudgeUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("judge_conflicts_judge_user_id_fkey");

            entity.HasOne(d => d.Participant).WithMany(p => p.JudgeConflicts)
                .HasForeignKey(d => d.ParticipantId)
                .HasConstraintName("judge_conflicts_participant_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.JudgeConflicts)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("judge_conflicts_team_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.JudgeConflictUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_judge_conflicts_updated_by_user");
        });

        modelBuilder.Entity<OverallTahapResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("overall_tahap_results_pkey");

            entity.ToTable("overall_tahap_results", "app");

            entity.HasIndex(e => e.CalculationRunId, "ix_overall_tahap_results_calc_run");

            entity.HasIndex(e => e.CompetitionYearId, "ix_overall_tahap_results_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_overall_tahap_results_created_by");

            entity.HasIndex(e => new { e.CompetitionYearId, e.TahapCode, e.RankingNo }, "ix_overall_tahap_results_current_ranking").HasFilter("(is_current = true)");

            entity.HasIndex(e => e.IsCurrent, "ix_overall_tahap_results_is_current");

            entity.HasIndex(e => e.ParticipantId, "ix_overall_tahap_results_participant_id");

            entity.HasIndex(e => e.ResultStatus, "ix_overall_tahap_results_status");

            entity.HasIndex(e => e.TahapCode, "ix_overall_tahap_results_tahap_code");

            entity.HasIndex(e => e.TieBreakerPolicyId, "ix_overall_tahap_results_tb_policy_id");

            entity.HasIndex(e => e.TeamId, "ix_overall_tahap_results_team_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_overall_tahap_results_updated_by");

            entity.HasIndex(e => new { e.CalculationRunId, e.TahapCode, e.ParticipantId }, "uq_overall_tahap_results_run_participant").IsUnique();

            entity.HasIndex(e => new { e.CompetitionYearId, e.TahapCode, e.ParticipantId }, "ux_overall_tahap_results_current_participant")
                .IsUnique()
                .HasFilter("(is_current = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AveragePoints)
                .HasPrecision(10, 4)
                .HasColumnName("average_points");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("calculated_at");
            entity.Property(e => e.CalculationRunId).HasColumnName("calculation_run_id");
            entity.Property(e => e.CalculationRunNo)
                .HasDefaultValue(1)
                .HasColumnName("calculation_run_no");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EventsParticipated)
                .HasDefaultValue(0)
                .HasColumnName("events_participated");
            entity.Property(e => e.IndividualEventPoints)
                .HasDefaultValue(0)
                .HasColumnName("individual_event_points");
            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(true)
                .HasColumnName("is_current");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.PointsBreakdownJson)
                .HasColumnType("jsonb")
                .HasColumnName("points_breakdown_json");
            entity.Property(e => e.RankingNo).HasColumnName("ranking_no");
            entity.Property(e => e.ResultStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'CALCULATED'::character varying")
                .HasColumnName("result_status");
            entity.Property(e => e.TahapCode)
                .HasMaxLength(20)
                .HasColumnName("tahap_code");
            entity.Property(e => e.TeamEventPoints)
                .HasDefaultValue(0)
                .HasColumnName("team_event_points");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.TieBreakerPolicyId).HasColumnName("tie_breaker_policy_id");
            entity.Property(e => e.TieBreakerValuesJson)
                .HasColumnType("jsonb")
                .HasColumnName("tie_breaker_values_json");
            entity.Property(e => e.TotalPoints)
                .HasDefaultValue(0)
                .HasColumnName("total_points");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CalculationRun).WithMany(p => p.OverallTahapResults)
                .HasForeignKey(d => d.CalculationRunId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_overall_tahap_results_calc_run");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.OverallTahapResults)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("overall_tahap_results_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.OverallTahapResultCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_overall_tahap_results_created_by_user");

            entity.HasOne(d => d.Participant).WithMany(p => p.OverallTahapResults)
                .HasForeignKey(d => d.ParticipantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("overall_tahap_results_participant_id_fkey");

            entity.HasOne(d => d.TahapCodeNavigation).WithMany(p => p.OverallTahapResults)
                .HasForeignKey(d => d.TahapCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("overall_tahap_results_tahap_code_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.OverallTahapResults)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_overall_tahap_results_team");

            entity.HasOne(d => d.TieBreakerPolicy).WithMany(p => p.OverallTahapResults)
                .HasForeignKey(d => d.TieBreakerPolicyId)
                .HasConstraintName("fk_overall_tahap_results_tb_policy");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.OverallTahapResultUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_overall_tahap_results_updated_by_user");
        });

        modelBuilder.Entity<Participant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("participants_pkey");

            entity.ToTable("participants", "app");

            entity.HasIndex(e => e.CompetitionYearId, "ix_participants_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_participants_created_by");

            entity.HasIndex(e => e.TahapCode, "ix_participants_tahap_code");

            entity.HasIndex(e => e.TeamId, "ix_participants_team_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_participants_updated_by");

            entity.HasIndex(e => new { e.CompetitionYearId, e.NricNumber }, "ux_participants_year_nric")
                .IsUnique()
                .HasFilter("(nric_number IS NOT NULL)");

            entity.HasIndex(e => new { e.CompetitionYearId, e.PassportNumber }, "ux_participants_year_passport")
                .IsUnique()
                .HasFilter("(passport_number IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.FullNameAsPerId)
                .HasMaxLength(200)
                .HasColumnName("full_name_as_per_id");
            entity.Property(e => e.FullNameGurmukhi)
                .HasMaxLength(200)
                .HasColumnName("full_name_gurmukhi");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");
            entity.Property(e => e.IdType)
                .HasMaxLength(20)
                .HasColumnName("id_type");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(30)
                .HasColumnName("mobile_number");
            entity.Property(e => e.NricNumber)
                .HasMaxLength(12)
                .HasColumnName("nric_number");
            entity.Property(e => e.ParentGuardianMobile)
                .HasMaxLength(30)
                .HasColumnName("parent_guardian_mobile");
            entity.Property(e => e.ParentGuardianName)
                .HasMaxLength(200)
                .HasColumnName("parent_guardian_name");
            entity.Property(e => e.PassportExpiryDate).HasColumnName("passport_expiry_date");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(20)
                .HasColumnName("passport_number");
            entity.Property(e => e.TahapCode)
                .HasMaxLength(20)
                .HasColumnName("tahap_code");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.Participants)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("participants_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ParticipantCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_participants_created_by_user");

            entity.HasOne(d => d.TahapCodeNavigation).WithMany(p => p.Participants)
                .HasForeignKey(d => d.TahapCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("participants_tahap_code_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.Participants)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("participants_team_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ParticipantUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_participants_updated_by_user");
        });

        modelBuilder.Entity<PasswordBlocklist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("password_blocklist_pkey");

            entity.ToTable("password_blocklist", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_password_blocklist_created_by");

            entity.HasIndex(e => e.IsActive, "ix_password_blocklist_is_active");

            entity.HasIndex(e => e.UpdatedBy, "ix_password_blocklist_updated_by");

            entity.HasIndex(e => e.NormalizedValue, "uq_password_blocklist_normalized_value").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BlockedValue)
                .HasMaxLength(256)
                .HasColumnName("blocked_value");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.NormalizedValue)
                .HasMaxLength(256)
                .HasColumnName("normalized_value");
            entity.Property(e => e.Reason)
                .HasMaxLength(200)
                .HasColumnName("reason");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PasswordBlocklistCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_password_blocklist_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PasswordBlocklistUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_password_blocklist_updated_by_user");
        });

        modelBuilder.Entity<PointsScheme>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("points_schemes_pkey");

            entity.ToTable("points_schemes", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_points_schemes_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_points_schemes_updated_by");

            entity.HasIndex(e => e.SchemeName, "points_schemes_scheme_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SchemeName)
                .HasMaxLength(100)
                .HasColumnName("scheme_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.WinnerCount).HasColumnName("winner_count");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PointsSchemeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_points_schemes_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PointsSchemeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_points_schemes_updated_by_user");
        });

        modelBuilder.Entity<PointsSchemeItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("points_scheme_items_pkey");

            entity.ToTable("points_scheme_items", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_points_scheme_items_created_by");

            entity.HasIndex(e => e.PointsSchemeId, "ix_points_scheme_items_points_scheme_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_points_scheme_items_updated_by");

            entity.HasIndex(e => new { e.PointsSchemeId, e.PlacingNo }, "uq_points_scheme_items_scheme_placing").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PlacingNo).HasColumnName("placing_no");
            entity.Property(e => e.PointsAwarded).HasColumnName("points_awarded");
            entity.Property(e => e.PointsSchemeId).HasColumnName("points_scheme_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PointsSchemeItemCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_points_scheme_items_created_by_user");

            entity.HasOne(d => d.PointsScheme).WithMany(p => p.PointsSchemeItems)
                .HasForeignKey(d => d.PointsSchemeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("points_scheme_items_points_scheme_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.PointsSchemeItemUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_points_scheme_items_updated_by_user");
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("regions_pkey");

            entity.ToTable("regions", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_regions_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_regions_updated_by");

            entity.HasIndex(e => e.Code, "regions_code_key").IsUnique();

            entity.HasIndex(e => e.Name, "regions_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RegionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_regions_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.RegionUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_regions_updated_by_user");
        });

        modelBuilder.Entity<RegistrationEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("registration_entries_pkey");

            entity.ToTable("registration_entries", "app");

            entity.HasIndex(e => new { e.TeamId, e.CompetitionCategoryId, e.EntryType, e.IsActive }, "ix_registration_entries_active_lookup");

            entity.HasIndex(e => e.ApprovedBy, "ix_registration_entries_approved_by");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_registration_entries_competition_category_id");

            entity.HasIndex(e => e.CreatedBy, "ix_registration_entries_created_by");

            entity.HasIndex(e => e.EntryStatus, "ix_registration_entries_entry_status");

            entity.HasIndex(e => e.EntryType, "ix_registration_entries_entry_type");

            entity.HasIndex(e => e.TeamId, "ix_registration_entries_team_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_registration_entries_updated_by");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EntryName)
                .HasMaxLength(200)
                .HasColumnName("entry_name");
            entity.Property(e => e.EntryStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("entry_status");
            entity.Property(e => e.EntryType)
                .HasMaxLength(20)
                .HasColumnName("entry_type");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.RejectedReason).HasColumnName("rejected_reason");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.RegistrationEntryApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("registration_entries_approved_by_fkey");

            entity.HasOne(d => d.CompetitionCategory).WithMany(p => p.RegistrationEntries)
                .HasForeignKey(d => d.CompetitionCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("registration_entries_competition_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RegistrationEntryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("registration_entries_created_by_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.RegistrationEntries)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("registration_entries_team_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.RegistrationEntryUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("registration_entries_updated_by_fkey");
        });

        modelBuilder.Entity<RegistrationEntryMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("registration_entry_members_pkey");

            entity.ToTable("registration_entry_members", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_registration_entry_members_created_by");

            entity.HasIndex(e => e.ParticipantId, "ix_registration_entry_members_participant_id");

            entity.HasIndex(e => e.RegistrationEntryId, "ix_registration_entry_members_registration_entry_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_registration_entry_members_updated_by");

            entity.HasIndex(e => new { e.RegistrationEntryId, e.MemberOrder }, "uq_registration_entry_members_entry_member_order").IsUnique();

            entity.HasIndex(e => new { e.RegistrationEntryId, e.ParticipantId }, "uq_registration_entry_members_entry_participant").IsUnique();

            entity.HasIndex(e => new { e.Id, e.RegistrationEntryId }, "ux_registration_entry_members_id_entry_id").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MemberOrder).HasColumnName("member_order");
            entity.Property(e => e.ParticipantId).HasColumnName("participant_id");
            entity.Property(e => e.RegistrationEntryId).HasColumnName("registration_entry_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RegistrationEntryMemberCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("registration_entry_members_created_by_fkey");

            entity.HasOne(d => d.Participant).WithMany(p => p.RegistrationEntryMembers)
                .HasForeignKey(d => d.ParticipantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("registration_entry_members_participant_id_fkey");

            entity.HasOne(d => d.RegistrationEntry).WithMany(p => p.RegistrationEntryMembers)
                .HasForeignKey(d => d.RegistrationEntryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("registration_entry_members_registration_entry_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.RegistrationEntryMemberUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("registration_entry_members_updated_by_fkey");
        });

        modelBuilder.Entity<ResultCalculationRun>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("result_calculation_runs_pkey");

            entity.ToTable("result_calculation_runs", "app");

            entity.HasIndex(e => e.CalculatedAt, "ix_result_calculation_runs_calculated_at");

            entity.HasIndex(e => e.CalculatedBy, "ix_result_calculation_runs_calculated_by");

            entity.HasIndex(e => e.CompetitionYearId, "ix_result_calculation_runs_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_result_calculation_runs_created_by");

            entity.HasIndex(e => e.CalculationScope, "ix_result_calculation_runs_scope");

            entity.HasIndex(e => e.RunStatus, "ix_result_calculation_runs_status");

            entity.HasIndex(e => e.UpdatedBy, "ix_result_calculation_runs_updated_by");

            entity.HasIndex(e => new { e.CompetitionYearId, e.CalculationScope, e.RunNo }, "uq_result_calculation_runs_year_scope_run_no").IsUnique();

            entity.HasIndex(e => new { e.CompetitionYearId, e.CalculationScope }, "ux_result_calculation_runs_current_year_scope")
                .IsUnique()
                .HasFilter("(is_current = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("calculated_at");
            entity.Property(e => e.CalculatedBy).HasColumnName("calculated_by");
            entity.Property(e => e.CalculationScope)
                .HasMaxLength(50)
                .HasDefaultValueSql("'COMPETITION_RESULTS'::character varying")
                .HasColumnName("calculation_scope");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(true)
                .HasColumnName("is_current");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.RunNo).HasColumnName("run_no");
            entity.Property(e => e.RunStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'COMPLETED'::character varying")
                .HasColumnName("run_status");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CalculatedByNavigation).WithMany(p => p.ResultCalculationRunCalculatedByNavigations)
                .HasForeignKey(d => d.CalculatedBy)
                .HasConstraintName("fk_result_calculation_runs_calculated_by_user");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.ResultCalculationRuns)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("result_calculation_runs_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ResultCalculationRunCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_result_calculation_runs_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ResultCalculationRunUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_result_calculation_runs_updated_by_user");
        });

        modelBuilder.Entity<RoleType>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("role_types_pkey");

            entity.ToTable("role_types", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_role_types_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_role_types_updated_by");

            entity.HasIndex(e => e.Name, "role_types_name_key").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RoleScope)
                .HasMaxLength(20)
                .HasColumnName("role_scope");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RoleTypeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_role_types_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.RoleTypeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_role_types_updated_by_user");
        });

        modelBuilder.Entity<ScheduleItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("schedule_items_pkey");

            entity.ToTable("schedule_items", "app");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_schedule_items_competition_category_id");

            entity.HasIndex(e => e.CompetitionYearId, "ix_schedule_items_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_schedule_items_created_by");

            entity.HasIndex(e => e.ScheduleDate, "ix_schedule_items_schedule_date");

            entity.HasIndex(e => e.ScheduleType, "ix_schedule_items_schedule_type");

            entity.HasIndex(e => e.UpdatedBy, "ix_schedule_items_updated_by");

            entity.HasIndex(e => e.VenueId, "ix_schedule_items_venue_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DisplayTitle)
                .HasMaxLength(200)
                .HasColumnName("display_title");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.IsPublished)
                .HasDefaultValue(false)
                .HasColumnName("is_published");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ReportingTime).HasColumnName("reporting_time");
            entity.Property(e => e.ScheduleDate).HasColumnName("schedule_date");
            entity.Property(e => e.ScheduleType)
                .HasMaxLength(30)
                .HasColumnName("schedule_type");
            entity.Property(e => e.SequenceNo).HasColumnName("sequence_no");
            entity.Property(e => e.SessionGroup)
                .HasMaxLength(100)
                .HasColumnName("session_group");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.VenueId).HasColumnName("venue_id");

            entity.HasOne(d => d.CompetitionCategory).WithMany(p => p.ScheduleItems)
                .HasForeignKey(d => d.CompetitionCategoryId)
                .HasConstraintName("schedule_items_competition_category_id_fkey");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.ScheduleItems)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("schedule_items_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ScheduleItemCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_schedule_items_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ScheduleItemUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_schedule_items_updated_by_user");

            entity.HasOne(d => d.Venue).WithMany(p => p.ScheduleItems)
                .HasForeignKey(d => d.VenueId)
                .HasConstraintName("schedule_items_venue_id_fkey");
        });

        modelBuilder.Entity<ScoreEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("score_entries_pkey");

            entity.ToTable("score_entries", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_score_entries_created_by");

            entity.HasIndex(e => e.IsActive, "ix_score_entries_is_active");

            entity.HasIndex(e => e.RegistrationEntryId, "ix_score_entries_registration_entry_id");

            entity.HasIndex(e => e.ScoreSheetId, "ix_score_entries_score_sheet_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_score_entries_updated_by");

            entity.HasIndex(e => new { e.ScoreSheetId, e.RegistrationEntryId }, "uq_score_entries_sheet_registration_entry").IsUnique();

            entity.HasIndex(e => new { e.Id, e.RegistrationEntryId }, "ux_score_entries_id_registration_entry_id").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EntryTotalScore)
                .HasPrecision(10, 2)
                .HasColumnName("entry_total_score");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RegistrationEntryId).HasColumnName("registration_entry_id");
            entity.Property(e => e.ScoreSheetId).HasColumnName("score_sheet_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ScoreEntryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_score_entries_created_by_user");

            entity.HasOne(d => d.RegistrationEntry).WithMany(p => p.ScoreEntries)
                .HasForeignKey(d => d.RegistrationEntryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("score_entries_registration_entry_id_fkey");

            entity.HasOne(d => d.ScoreSheet).WithMany(p => p.ScoreEntries)
                .HasForeignKey(d => d.ScoreSheetId)
                .HasConstraintName("score_entries_score_sheet_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ScoreEntryUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_score_entries_updated_by_user");
        });

        modelBuilder.Entity<ScoreEntryScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("score_entry_scores_pkey");

            entity.ToTable("score_entry_scores", "app");

            entity.HasIndex(e => e.ChangeVerifiedBy, "ix_score_entry_scores_change_verified_by");

            entity.HasIndex(e => e.CreatedBy, "ix_score_entry_scores_created_by");

            entity.HasIndex(e => e.CriterionId, "ix_score_entry_scores_criterion_id");

            entity.HasIndex(e => e.IsActive, "ix_score_entry_scores_is_active");

            entity.HasIndex(e => e.JudgeAssignmentId, "ix_score_entry_scores_judge_assignment_id");

            entity.HasIndex(e => e.OverrideBy, "ix_score_entry_scores_override_by");

            entity.HasIndex(e => e.IsPendingVerification, "ix_score_entry_scores_pending_verification");

            entity.HasIndex(e => e.RegistrationEntryId, "ix_score_entry_scores_registration_entry_id");

            entity.HasIndex(e => e.RegistrationEntryMemberId, "ix_score_entry_scores_registration_entry_member_id");

            entity.HasIndex(e => e.ScoreEntryId, "ix_score_entry_scores_score_entry_id");

            entity.HasIndex(e => e.ScoreSourceType, "ix_score_entry_scores_score_source_type");

            entity.HasIndex(e => e.UpdatedBy, "ix_score_entry_scores_updated_by");

            entity.HasIndex(e => new { e.ScoreEntryId, e.RegistrationEntryMemberId, e.CriterionId }, "uq_score_entry_scores_entry_member_criterion").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChangeVerifiedAt).HasColumnName("change_verified_at");
            entity.Property(e => e.ChangeVerifiedBy).HasColumnName("change_verified_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CriterionId).HasColumnName("criterion_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsPendingVerification)
                .HasDefaultValue(false)
                .HasColumnName("is_pending_verification");
            entity.Property(e => e.JudgeAssignmentId).HasColumnName("judge_assignment_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OverrideAt).HasColumnName("override_at");
            entity.Property(e => e.OverrideBy).HasColumnName("override_by");
            entity.Property(e => e.OverrideReason).HasColumnName("override_reason");
            entity.Property(e => e.PreviousScoreValue)
                .HasPrecision(10, 2)
                .HasColumnName("previous_score_value");
            entity.Property(e => e.RegistrationEntryId).HasColumnName("registration_entry_id");
            entity.Property(e => e.RegistrationEntryMemberId).HasColumnName("registration_entry_member_id");
            entity.Property(e => e.ScoreEntryId).HasColumnName("score_entry_id");
            entity.Property(e => e.ScoreSourceType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'JUDGE'::character varying")
                .HasColumnName("score_source_type");
            entity.Property(e => e.ScoreValue)
                .HasPrecision(10, 2)
                .HasColumnName("score_value");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.ChangeVerifiedByNavigation).WithMany(p => p.ScoreEntryScoreChangeVerifiedByNavigations)
                .HasForeignKey(d => d.ChangeVerifiedBy)
                .HasConstraintName("fk_score_entry_scores_change_verified_by_user");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ScoreEntryScoreCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_score_entry_scores_created_by_user");

            entity.HasOne(d => d.Criterion).WithMany(p => p.ScoreEntryScores)
                .HasForeignKey(d => d.CriterionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("score_entry_scores_criterion_id_fkey");

            entity.HasOne(d => d.JudgeAssignment).WithMany(p => p.ScoreEntryScores)
                .HasForeignKey(d => d.JudgeAssignmentId)
                .HasConstraintName("score_entry_scores_judge_assignment_id_fkey");

            entity.HasOne(d => d.OverrideByNavigation).WithMany(p => p.ScoreEntryScoreOverrideByNavigations)
                .HasForeignKey(d => d.OverrideBy)
                .HasConstraintName("fk_score_entry_scores_override_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ScoreEntryScoreUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_score_entry_scores_updated_by_user");

            entity.HasOne(d => d.RegistrationEntryMember).WithMany(p => p.ScoreEntryScores)
                .HasPrincipalKey(p => new { p.Id, p.RegistrationEntryId })
                .HasForeignKey(d => new { d.RegistrationEntryMemberId, d.RegistrationEntryId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_score_entry_scores_member_registration_entry");

            entity.HasOne(d => d.ScoreEntry).WithMany(p => p.ScoreEntryScores)
                .HasPrincipalKey(p => new { p.Id, p.RegistrationEntryId })
                .HasForeignKey(d => new { d.ScoreEntryId, d.RegistrationEntryId })
                .HasConstraintName("fk_score_entry_scores_score_entry_registration_entry");
        });

        modelBuilder.Entity<ScoreSheet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("score_sheets_pkey");

            entity.ToTable("score_sheets", "app");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_score_sheets_competition_category_id");

            entity.HasIndex(e => e.CreatedBy, "ix_score_sheets_created_by");

            entity.HasIndex(e => e.PreviousScoreSheetId, "ix_score_sheets_previous_score_sheet_id");

            entity.HasIndex(e => e.ReplacedByScoreSheetId, "ix_score_sheets_replaced_by_score_sheet_id");

            entity.HasIndex(e => e.ScoreSheetStatus, "ix_score_sheets_score_sheet_status");

            entity.HasIndex(e => e.SubmittedBy, "ix_score_sheets_submitted_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_score_sheets_updated_by");

            entity.HasIndex(e => e.VerifiedBy, "ix_score_sheets_verified_by");

            entity.HasIndex(e => new { e.CompetitionCategoryId, e.VersionNo }, "uq_score_sheets_category_version").IsUnique();

            entity.HasIndex(e => e.CompetitionCategoryId, "ux_score_sheets_current_category")
                .IsUnique()
                .HasFilter("(is_current = true)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(true)
                .HasColumnName("is_current");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PreviousScoreSheetId).HasColumnName("previous_score_sheet_id");
            entity.Property(e => e.ReplacedByScoreSheetId).HasColumnName("replaced_by_score_sheet_id");
            entity.Property(e => e.ScoreSheetStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'NOT_STARTED'::character varying")
                .HasColumnName("score_sheet_status");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.VersionNo)
                .HasDefaultValue(1)
                .HasColumnName("version_no");

            entity.HasOne(d => d.CompetitionCategory).WithOne(p => p.ScoreSheet)
                .HasForeignKey<ScoreSheet>(d => d.CompetitionCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("score_sheets_competition_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ScoreSheetCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_score_sheets_created_by_user");

            entity.HasOne(d => d.PreviousScoreSheet).WithMany(p => p.InversePreviousScoreSheet)
                .HasForeignKey(d => d.PreviousScoreSheetId)
                .HasConstraintName("score_sheets_previous_score_sheet_id_fkey");

            entity.HasOne(d => d.ReplacedByScoreSheet).WithMany(p => p.InverseReplacedByScoreSheet)
                .HasForeignKey(d => d.ReplacedByScoreSheetId)
                .HasConstraintName("score_sheets_replaced_by_score_sheet_id_fkey");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.ScoreSheetSubmittedByNavigations)
                .HasForeignKey(d => d.SubmittedBy)
                .HasConstraintName("fk_score_sheets_submitted_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ScoreSheetUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_score_sheets_updated_by_user");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.ScoreSheetVerifiedByNavigations)
                .HasForeignKey(d => d.VerifiedBy)
                .HasConstraintName("fk_score_sheets_verified_by_user");
        });

        modelBuilder.Entity<State>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("states_pkey");

            entity.ToTable("states", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_states_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_states_updated_by");

            entity.HasIndex(e => e.Code, "states_code_key").IsUnique();

            entity.HasIndex(e => e.Name, "states_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StateCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_states_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.StateUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_states_updated_by_user");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("teams_pkey");

            entity.ToTable("teams", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_teams_created_by");

            entity.HasIndex(e => e.NameEnglish, "ix_teams_name_english").IsUnique();

            entity.HasIndex(e => e.UpdatedBy, "ix_teams_updated_by");

            entity.HasIndex(e => e.Code, "teams_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(30)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.NameEnglish)
                .HasMaxLength(200)
                .HasColumnName("name_english");
            entity.Property(e => e.NameGurmukhi)
                .HasMaxLength(200)
                .HasColumnName("name_gurmukhi");
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.StateId).HasColumnName("state_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TeamCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_teams_created_by_user");

            entity.HasOne(d => d.Region).WithMany(p => p.Teams)
                .HasForeignKey(d => d.RegionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teams_region_id_fkey");

            entity.HasOne(d => d.State).WithMany(p => p.Teams)
                .HasForeignKey(d => d.StateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("teams_state_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TeamUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_teams_updated_by_user");
        });

        modelBuilder.Entity<TeamCompositionPattern>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_composition_patterns_pkey");

            entity.ToTable("team_composition_patterns", "app");

            entity.HasIndex(e => e.CompetitionCategoryId, "ix_team_composition_patterns_competition_category_id");

            entity.HasIndex(e => e.CreatedBy, "ix_team_composition_patterns_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_team_composition_patterns_updated_by");

            entity.HasIndex(e => new { e.CompetitionCategoryId, e.PatternCode }, "uq_team_composition_patterns_competition_category_pattern").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AllowYoungerSubstitution)
                .HasDefaultValue(false)
                .HasColumnName("allow_younger_substitution");
            entity.Property(e => e.CompetitionCategoryId).HasColumnName("competition_category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PatternCode)
                .HasMaxLength(200)
                .HasColumnName("pattern_code");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionCategory).WithMany(p => p.TeamCompositionPatterns)
                .HasForeignKey(d => d.CompetitionCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("team_composition_patterns_competition_category_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TeamCompositionPatternCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_team_composition_patterns_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TeamCompositionPatternUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_team_composition_patterns_updated_by_user");
        });

        modelBuilder.Entity<TeamCompositionPatternSlot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_composition_pattern_slots_pkey");

            entity.ToTable("team_composition_pattern_slots", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_team_composition_pattern_slots_created_by");

            entity.HasIndex(e => e.TeamCompositionPatternId, "ix_team_composition_pattern_slots_pattern_id");

            entity.HasIndex(e => e.TahapCode, "ix_team_composition_pattern_slots_tahap_code");

            entity.HasIndex(e => e.UpdatedBy, "ix_team_composition_pattern_slots_updated_by");

            entity.HasIndex(e => new { e.TeamCompositionPatternId, e.SlotNo }, "ux_team_composition_pattern_slots_active_pattern_slot_no")
                .IsUnique()
                .HasFilter("is_active");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SlotNo).HasColumnName("slot_no");
            entity.Property(e => e.TahapCode)
                .HasMaxLength(20)
                .HasColumnName("tahap_code");
            entity.Property(e => e.TeamCompositionPatternId).HasColumnName("team_composition_pattern_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TeamCompositionPatternSlotCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_team_composition_pattern_slots_created_by_user");

            entity.HasOne(d => d.TahapCodeNavigation).WithMany(p => p.TeamCompositionPatternSlots)
                .HasForeignKey(d => d.TahapCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("team_composition_pattern_slots_tahap_code_fkey");

            entity.HasOne(d => d.TeamCompositionPattern).WithMany(p => p.TeamCompositionPatternSlots)
                .HasForeignKey(d => d.TeamCompositionPatternId)
                .HasConstraintName("team_composition_pattern_slots_pattern_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TeamCompositionPatternSlotUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_team_composition_pattern_slots_updated_by_user");
        });

        modelBuilder.Entity<TeamParticipantRegistrationDraft>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_participant_registration_drafts_pkey");

            entity.ToTable("team_participant_registration_drafts", "app");

            entity.HasIndex(e => new { e.TeamId, e.DraftStatus, e.IsActive }, "ix_tprd_active_lookup");

            entity.HasIndex(e => e.CompetitionYearId, "ix_tprd_comp_year");

            entity.HasIndex(e => e.CreatedBy, "ix_tprd_created_by");

            entity.HasIndex(e => e.DraftStatus, "ix_tprd_status");

            entity.HasIndex(e => e.TahapCode, "ix_tprd_tahap");

            entity.HasIndex(e => e.TeamId, "ix_tprd_team");

            entity.HasIndex(e => e.UpdatedBy, "ix_tprd_updated_by");

            entity.HasIndex(e => e.SubmittedParticipantId, "ux_tprd_submitted_participant")
                .IsUnique()
                .HasFilter("(submitted_participant_id IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CancelledReason).HasColumnName("cancelled_reason");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DraftPayloadJson)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("draft_payload_json");
            entity.Property(e => e.DraftStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("draft_status");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SelectedIndividualCompetitionCategoryIdsJson)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("selected_individual_competition_category_ids_json");
            entity.Property(e => e.SelectedTeamCompetitionCategoryIdsJson)
                .HasDefaultValueSql("'[]'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("selected_team_competition_category_ids_json");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
            entity.Property(e => e.SubmittedParticipantId).HasColumnName("submitted_participant_id");
            entity.Property(e => e.TahapCode)
                .HasMaxLength(20)
                .HasColumnName("tahap_code");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.TeamParticipantRegistrationDraftCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("fk_tprd_cancelled_by");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.TeamParticipantRegistrationDrafts)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tprd_comp_year");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TeamParticipantRegistrationDraftCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_tprd_created_by");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.TeamParticipantRegistrationDraftSubmittedByNavigations)
                .HasForeignKey(d => d.SubmittedBy)
                .HasConstraintName("fk_tprd_submitted_by");

            entity.HasOne(d => d.SubmittedParticipant).WithOne(p => p.TeamParticipantRegistrationDraft)
                .HasForeignKey<TeamParticipantRegistrationDraft>(d => d.SubmittedParticipantId)
                .HasConstraintName("fk_tprd_submitted_participant");

            entity.HasOne(d => d.TahapCodeNavigation).WithMany(p => p.TeamParticipantRegistrationDrafts)
                .HasForeignKey(d => d.TahapCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tprd_tahap");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamParticipantRegistrationDrafts)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tprd_team");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TeamParticipantRegistrationDraftUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_tprd_updated_by");
        });

        modelBuilder.Entity<TieBreakerPolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tie_breaker_policies_pkey");

            entity.ToTable("tie_breaker_policies", "app");

            entity.HasIndex(e => e.CompetitionYearId, "ix_tie_breaker_policies_competition_year_id");

            entity.HasIndex(e => e.CreatedBy, "ix_tie_breaker_policies_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_tie_breaker_policies_updated_by");

            entity.HasIndex(e => new { e.CompetitionYearId, e.PolicyName }, "uq_tie_breaker_policies_year_name").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AppliesTo)
                .HasMaxLength(30)
                .HasColumnName("applies_to");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PolicyName)
                .HasMaxLength(100)
                .HasColumnName("policy_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.TieBreakerPolicies)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tie_breaker_policies_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TieBreakerPolicyCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_tie_breaker_policies_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TieBreakerPolicyUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_tie_breaker_policies_updated_by_user");
        });

        modelBuilder.Entity<TieBreakerPolicyRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tie_breaker_policy_rules_pkey");

            entity.ToTable("tie_breaker_policy_rules", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_tie_breaker_policy_rules_created_by");

            entity.HasIndex(e => e.PolicyId, "ix_tie_breaker_policy_rules_policy_id");

            entity.HasIndex(e => e.UpdatedBy, "ix_tie_breaker_policy_rules_updated_by");

            entity.HasIndex(e => new { e.PolicyId, e.PriorityNo }, "uq_tie_breaker_policy_rules_policy_priority").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PolicyId).HasColumnName("policy_id");
            entity.Property(e => e.PriorityNo).HasColumnName("priority_no");
            entity.Property(e => e.RuleCode)
                .HasMaxLength(50)
                .HasColumnName("rule_code");
            entity.Property(e => e.SortDirection)
                .HasMaxLength(10)
                .HasColumnName("sort_direction");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TieBreakerPolicyRuleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_tie_breaker_policy_rules_created_by_user");

            entity.HasOne(d => d.Policy).WithMany(p => p.TieBreakerPolicyRules)
                .HasForeignKey(d => d.PolicyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("tie_breaker_policy_rules_policy_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TieBreakerPolicyRuleUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_tie_breaker_policy_rules_updated_by_user");
        });

        modelBuilder.Entity<UserCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_credentials_pkey");

            entity.ToTable("user_credentials", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_user_credentials_created_by");

            entity.HasIndex(e => e.HardLockedBy, "ix_user_credentials_hard_locked_by");

            entity.HasIndex(e => e.RequiresAdminUnlock, "ix_user_credentials_requires_admin_unlock");

            entity.HasIndex(e => e.UpdatedBy, "ix_user_credentials_updated_by");

            entity.HasIndex(e => e.UserId, "uq_user_credentials_user_id").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.FailedLoginCount)
                .HasDefaultValue(0)
                .HasColumnName("failed_login_count");
            entity.Property(e => e.HardLockReason)
                .HasMaxLength(300)
                .HasColumnName("hard_lock_reason");
            entity.Property(e => e.HardLockedAt).HasColumnName("hard_locked_at");
            entity.Property(e => e.HardLockedBy).HasColumnName("hard_locked_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.LastPasswordChangedAt).HasColumnName("last_password_changed_at");
            entity.Property(e => e.LastSoftLockedAt).HasColumnName("last_soft_locked_at");
            entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
            entity.Property(e => e.MustChangePassword)
                .HasDefaultValue(true)
                .HasColumnName("must_change_password");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.RequiresAdminUnlock)
                .HasDefaultValue(false)
                .HasColumnName("requires_admin_unlock");
            entity.Property(e => e.SoftLockCount)
                .HasDefaultValue(0)
                .HasColumnName("soft_lock_count");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserCredentialCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_user_credentials_created_by_user");

            entity.HasOne(d => d.HardLockedByNavigation).WithMany(p => p.UserCredentialHardLockedByNavigations)
                .HasForeignKey(d => d.HardLockedBy)
                .HasConstraintName("fk_user_credentials_hard_locked_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UserCredentialUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_user_credentials_updated_by_user");

            entity.HasOne(d => d.User).WithOne(p => p.UserCredentialUser)
                .HasForeignKey<UserCredential>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_credentials_user_profile");
        });

        modelBuilder.Entity<UserPasswordHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_password_history_pkey");

            entity.ToTable("user_password_history", "app");

            entity.HasIndex(e => e.ChangedBy, "ix_user_password_history_changed_by");

            entity.HasIndex(e => e.CreatedBy, "ix_user_password_history_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_user_password_history_updated_by");

            entity.HasIndex(e => new { e.UserId, e.ChangedAt }, "ix_user_password_history_user_changed_at").IsDescending(false, true);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChangeReason)
                .HasMaxLength(100)
                .HasColumnName("change_reason");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.UserPasswordHistoryChangedByNavigations)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("fk_user_password_history_changed_by_user");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserPasswordHistoryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_user_password_history_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UserPasswordHistoryUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_user_password_history_updated_by_user");

            entity.HasOne(d => d.User).WithMany(p => p.UserPasswordHistoryUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_password_history_user_profile");
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_profiles_pkey");

            entity.ToTable("user_profiles", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_user_profiles_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_user_profiles_updated_by");

            entity.HasIndex(e => e.NricNumber, "ux_user_profiles_nric")
                .IsUnique()
                .HasFilter("(nric_number IS NOT NULL)");

            entity.HasIndex(e => e.PassportNumber, "ux_user_profiles_passport")
                .IsUnique()
                .HasFilter("(passport_number IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(256)
                .HasColumnName("email");
            entity.Property(e => e.FullNameAsPerId)
                .HasMaxLength(200)
                .HasColumnName("full_name_as_per_id");
            entity.Property(e => e.FullNameGurmukhi)
                .HasMaxLength(200)
                .HasColumnName("full_name_gurmukhi");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");
            entity.Property(e => e.IdType)
                .HasMaxLength(20)
                .HasColumnName("id_type");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(30)
                .HasColumnName("mobile_number");
            entity.Property(e => e.NricNumber)
                .HasMaxLength(12)
                .HasColumnName("nric_number");
            entity.Property(e => e.PassportExpiryDate).HasColumnName("passport_expiry_date");
            entity.Property(e => e.PassportNumber)
                .HasMaxLength(20)
                .HasColumnName("passport_number");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_user_profiles_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.InverseUpdatedByNavigation)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_user_profiles_updated_by_user");
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_role_assignments_pkey");

            entity.ToTable("user_role_assignments", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_user_role_assignments_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_user_role_assignments_updated_by");

            entity.HasIndex(e => new { e.UserId, e.RoleCode }, "ux_user_role_assignments_user_role_noteam")
                .IsUnique()
                .HasFilter("(team_id IS NULL)");

            entity.HasIndex(e => new { e.UserId, e.RoleCode, e.TeamId }, "ux_user_role_assignments_user_role_team").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsPrimary)
                .HasDefaultValue(false)
                .HasColumnName("is_primary");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_verified");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(50)
                .HasColumnName("role_code");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.UserRoleAssignmentCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_user_role_assignments_created_by_user");

            entity.HasOne(d => d.RoleCodeNavigation).WithMany(p => p.UserRoleAssignments)
                .HasForeignKey(d => d.RoleCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_role_assignments_role_code_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.UserRoleAssignments)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("user_role_assignments_team_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.UserRoleAssignmentUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_user_role_assignments_updated_by_user");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleAssignmentUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_role_assignments_user_id_fkey");
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("venues_pkey");

            entity.ToTable("venues", "app");

            entity.HasIndex(e => e.CreatedBy, "ix_venues_created_by");

            entity.HasIndex(e => e.UpdatedBy, "ix_venues_updated_by");

            entity.HasIndex(e => new { e.CompetitionYearId, e.Code }, "uq_venues_competition_year_code").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CompetitionYearId).HasColumnName("competition_year_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LocationText)
                .HasMaxLength(300)
                .HasColumnName("location_text");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CompetitionYear).WithMany(p => p.Venues)
                .HasForeignKey(d => d.CompetitionYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("venues_competition_year_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.VenueCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_venues_created_by_user");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.VenueUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_venues_updated_by_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
