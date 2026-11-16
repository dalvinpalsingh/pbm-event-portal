using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Application.Enums;

namespace PbmEventPortal.Application.Helpers.TeamManagement;

public class TeamManagementHelper
{
    public static bool CategoryAppliesToTahap(string categoryCode, string tahapCode)
    {
        if (string.Equals(categoryCode, tahapCode, StringComparison.OrdinalIgnoreCase)) return true;

        return categoryCode.ToUpperInvariant() switch
        {
            TahapEnums.Primary => tahapCode is TahapEnums.Tahap1 or TahapEnums.Tahap2,
            TahapEnums.Secondary => tahapCode is TahapEnums.Tahap3 or TahapEnums.Tahap4,
            _ => false
        };
    }

    public static string GetCategoryShortName(string categoryCode)
    {
        return categoryCode.ToUpperInvariant() switch
        {
            TahapEnums.Tahap1 => TahapEnums.T1,
            TahapEnums.Tahap2 => TahapEnums.T2,
            TahapEnums.Tahap3 => TahapEnums.T3,
            TahapEnums.Tahap4 => TahapEnums.T4,
            TahapEnums.Primary => TahapEnums.Pri,
            TahapEnums.Secondary => TahapEnums.Sec,
            _ => categoryCode
        };
    }

    public static int GetTargetCount(string eventType, short? individualEventMaxParticipants, short? teamEventTeamSize)
    {
        if (eventType == RegistrationEnums.EntryTypeIndividual) return individualEventMaxParticipants ?? 1;
        if (eventType == RegistrationEnums.EntryTypeTeam) return teamEventTeamSize ?? 0;
        return 0;
    }

    public static object ParticipantSnapshot(Participant participant) => new
    {
        participant.Id,
        participant.CompetitionYearId,
        participant.TeamId,
        participant.FullNameAsPerId,
        participant.FullNameGurmukhi,
        participant.IdType,
        participant.NricNumber,
        participant.PassportNumber,
        participant.PassportExpiryDate,
        participant.MobileNumber,
        participant.Email,
        participant.ParentGuardianName,
        participant.ParentGuardianMobile,
        participant.Gender,
        participant.DateOfBirth,
        participant.TahapCode,
        participant.IsActive,
        participant.CreatedAt,
        participant.CreatedBy,
        participant.UpdatedAt,
        participant.UpdatedBy
    };

    public static object RegistrationEntrySnapshot(RegistrationEntry entry, IReadOnlySet<Guid>? excludedMemberIds = null) => new
    {
        entry.Id,
        entry.CompetitionCategoryId,
        entry.TeamId,
        entry.EntryType,
        entry.EntryName,
        entry.EntryStatus,
        entry.ApprovedBy,
        entry.RejectedReason,
        entry.Notes,
        entry.IsActive,
        entry.CreatedAt,
        entry.CreatedBy,
        entry.UpdatedAt,
        entry.UpdatedBy,
        Participants = entry.RegistrationEntryMembers
            .Where(member => excludedMemberIds == null || !excludedMemberIds.Contains(member.Id))
            .OrderBy(member => member.MemberOrder)
            .Select(member => new
        {
            member.Id,
            member.RegistrationEntryId,
            member.ParticipantId,
            ParticipantOrder = member.MemberOrder,
            member.IsActive,
            member.CreatedAt,
            member.CreatedBy,
            member.UpdatedAt,
            member.UpdatedBy
        }).ToList()
    };
}
