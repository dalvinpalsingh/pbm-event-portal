namespace PbmEventPortal.Application.Enums;

public class JudgeEnums
{
    public const string ConflictTypeOwnTeam = "OWN_TEAM";
    public const string ConflictTypeRelatedParticipant = "RELATED_PARTICIPANT";
    public const string ConflictTypeCategoryBlock = "CATEGORY_BLOCK";
    public const string ConflictTypeCustom = "CUSTOM";
    public static readonly string[] ConflictTypes = [ConflictTypeOwnTeam, ConflictTypeRelatedParticipant, ConflictTypeCategoryBlock, ConflictTypeCustom];
}
