namespace PbmEventPortal.Application.Enums;

public class ScheduleEnums
{
    public const string ScheduleTypeRegistration = "REGISTRATION";
    public const string ScheduleTypeMeal = "MEAL";
    public const string ScheduleTypeCeremony = "CEREMONY";
    public const string ScheduleTypeCompetition = "COMPETITION";
    public const string ScheduleTypePrizeGiving = "PRIZE_GIVING";
    public const string ScheduleTypeGeneral = "GENERAL";
    public const string ScheduleTypeBuffer = "BUFFER";
    public static readonly string[] ScheduleTypes = [ScheduleTypeRegistration, ScheduleTypeMeal, ScheduleTypeCeremony, ScheduleTypeCompetition, ScheduleTypePrizeGiving, ScheduleTypeGeneral, ScheduleTypeBuffer];

    public const string StatusDraft = "DRAFT";
    public const string StatusPublished = "PUBLISHED";
    public const string StatusOngoing = "ONGOING";
    public const string StatusCompleted = "COMPLETED";
    public const string StatusDelayed = "DELAYED";
    public const string StatusCancelled = "CANCELLED";
    public const string StatusMoved = "MOVED";
    public static readonly string[] Statuses = [StatusDraft, StatusPublished, StatusOngoing, StatusCompleted, StatusDelayed, StatusCancelled, StatusMoved];
}
