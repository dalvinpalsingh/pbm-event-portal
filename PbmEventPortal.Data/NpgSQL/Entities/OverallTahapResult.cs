using System;
using System.Collections.Generic;

namespace PbmEventPortal.Data.NpgSQL.Entities;

public partial class OverallTahapResult
{
    public Guid Id { get; set; }

    public Guid CompetitionYearId { get; set; }

    public string TahapCode { get; set; } = null!;

    public Guid ParticipantId { get; set; }

    public int TotalPoints { get; set; }

    public int EventsParticipated { get; set; }

    public decimal AveragePoints { get; set; }

    public int IndividualEventPoints { get; set; }

    public int TeamEventPoints { get; set; }

    public int? RankingNo { get; set; }

    public int CalculationRunNo { get; set; }

    public DateTime CalculatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public Guid CalculationRunId { get; set; }

    public Guid TeamId { get; set; }

    public bool IsCurrent { get; set; }

    public string ResultStatus { get; set; } = null!;

    public Guid? TieBreakerPolicyId { get; set; }

    public string? TieBreakerValuesJson { get; set; }

    public string? PointsBreakdownJson { get; set; }

    public virtual ResultCalculationRun CalculationRun { get; set; } = null!;

    public virtual CompetitionYear CompetitionYear { get; set; } = null!;

    public virtual UserProfile? CreatedByNavigation { get; set; }

    public virtual Participant Participant { get; set; } = null!;

    public virtual CategoryType TahapCodeNavigation { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual TieBreakerPolicy? TieBreakerPolicy { get; set; }

    public virtual UserProfile? UpdatedByNavigation { get; set; }
}
