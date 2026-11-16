namespace PbmEventPortal.Application.Enums;

public class RegistrationEnums
{
    public const string DraftStatusDraft = "DRAFT";

    public const string EntryStatusDraft = "DRAFT";
    public const string EntryStatusSubmitted = "SUBMITTED";
    public const string EntryStatusApproved = "APPROVED";
    public const string EntryStatusRejected = "REJECTED";
    public const string EntryStatusCancelled = "CANCELLED";
    public static readonly string[] EntryStatuses = [EntryStatusDraft, EntryStatusSubmitted, EntryStatusApproved, EntryStatusRejected, EntryStatusCancelled];

    public const string EntryTypeIndividual = "INDIVIDUAL";
    public const string EntryTypeTeam = "TEAM";
}
