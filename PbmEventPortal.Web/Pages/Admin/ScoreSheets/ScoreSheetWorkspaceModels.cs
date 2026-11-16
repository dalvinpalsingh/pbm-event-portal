namespace PbmEventPortal.Web.Pages.Admin.ScoreSheets;

public class ScoreSheetInputModel
{
    public Guid? CompetitionYearId { get; set; }
    public Guid? CompetitionCategoryId { get; set; }
    public string? Notes { get; set; }
    public List<ScoreSheetScoreInputModel> Scores { get; set; } = [];
}

public class ScoreSheetScoreInputModel
{
    public Guid RegistrationEntryId { get; set; }
    public Guid RegistrationEntryMemberId { get; set; }
    public Guid CriterionId { get; set; }
    public decimal? ScoreValue { get; set; }
    public string? Notes { get; set; }
}

public class ScoreSheetWorkspaceModel
{
    public bool IsLoaded { get; set; }
    public string? EmptyMessage { get; set; }
    public ScoreSheetContextModel? Context { get; set; }
    public ScoreSheetSummaryModel Summary { get; set; } = new();
    public List<ScoreSheetCriterionModel> Criteria { get; set; } = [];
    public List<ScoreSheetRowModel> Rows { get; set; } = [];
    public ScoreSheetInputModel Input { get; set; } = new();
}

public class ScoreSheetSummaryModel
{
    public int EntriesInSheet { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int PendingVerification { get; set; }
}

public class ScoreSheetContextModel
{
    public string CompetitionYearDisplay { get; set; } = string.Empty;
    public string CompetitionDisplay { get; set; } = string.Empty;
    public string CompetitionCode { get; set; } = string.Empty;
    public string CategoryDisplay { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ScoreSheetStatus { get; set; } = "NOT_STARTED";
    public string VersionDisplay { get; set; } = "V1";
}

public class ScoreSheetCriterionModel
{
    public Guid CriterionId { get; set; }
    public string CriterionDisplay { get; set; } = string.Empty;
    public decimal? MaxScore { get; set; }
    public bool HasJudgeAssignment { get; set; }
}

public class ScoreSheetRowModel
{
    public Guid RegistrationEntryId { get; set; }
    public Guid RegistrationEntryMemberId { get; set; }
    public string EntryDisplay { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantMetaDisplay { get; set; } = string.Empty;
    public short MemberOrder { get; set; }
    public string TeamCode { get; set; } = string.Empty;
    public string TeamDisplay { get; set; } = string.Empty;
    public bool IsTeamEntry { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TeamTotal { get; set; }
    public List<ScoreSheetScoreCellModel> Scores { get; set; } = [];
}

public class ScoreSheetScoreCellModel
{
    public int InputIndex { get; set; }
    public Guid CriterionId { get; set; }
    public string CriterionDisplay { get; set; } = string.Empty;
    public decimal? MaxScore { get; set; }
    public decimal? ScoreValue { get; set; }
    public string? Notes { get; set; }
    public bool HasJudgeAssignment { get; set; }
    public bool IsPendingVerification { get; set; }
    public decimal? PreviousScoreValue { get; set; }
}

public sealed record CompetitionCategoryOptionRow(Guid Id, string EventType, string DisplayText);
