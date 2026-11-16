using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Participants;

namespace PbmEventPortal.Web.Pages.Admin.Participants;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ParticipantDetailsDTO? Participant { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var item = await _dbContext.Participants
            .AsNoTracking()
            .Include(x => x.CompetitionYear)
            .Include(x => x.Team)
            .Include(x => x.CreatedByNavigation)
            .Include(x => x.UpdatedByNavigation)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item == null) return NotFound();

        var registrationEntries = await _dbContext.RegistrationEntryMembers
            .AsNoTracking()
            .Where(member => member.ParticipantId == id)
            .Include(member => member.RegistrationEntry).ThenInclude(entry => entry.CompetitionCategory).ThenInclude(category => category.Competition)
            .Include(member => member.RegistrationEntry).ThenInclude(entry => entry.RegistrationEntryMembers).ThenInclude(entryMember => entryMember.Participant)
            .OrderByDescending(member => member.RegistrationEntry.IsActive)
            .ThenBy(member => member.RegistrationEntry.CompetitionCategory.Competition.Code)
            .ThenBy(member => member.RegistrationEntry.CompetitionCategory.SortOrder)
            .ThenBy(member => member.MemberOrder)
            .Select(member => new ParticipantRegistrationEntryViewModel
            {
                Id = member.RegistrationEntry.Id,
                CompetitionDisplay = member.RegistrationEntry.CompetitionCategory.Competition.Code + " - " + member.RegistrationEntry.CompetitionCategory.Competition.NameEnglish,
                CategoryDisplay = member.RegistrationEntry.CompetitionCategory.CategoryCode,
                EntryType = member.RegistrationEntry.EntryType,
                EntryStatus = member.RegistrationEntry.EntryStatus,
                IsActive = member.RegistrationEntry.IsActive && member.IsActive,
                ParticipantOrder = member.MemberOrder,
                OtherParticipantsDisplay = string.Join(", ", member.RegistrationEntry.RegistrationEntryMembers
                    .Where(otherMember => otherMember.IsActive && otherMember.ParticipantId != id)
                    .OrderBy(otherMember => otherMember.MemberOrder)
                    .Select(otherMember => otherMember.Participant.FullNameAsPerId))
            })
            .ToListAsync();

        Participant = new ParticipantDetailsDTO
        {
            Id = item.Id,
            CompetitionYearDisplayName = item.CompetitionYear.DisplayName,
            TeamDisplay = item.Team.Code + " - " + item.Team.NameEnglish,
            FullNameAsPerId = item.FullNameAsPerId,
            FullNameGurmukhi = item.FullNameGurmukhi,
            IdType = item.IdType,
            NricNumber = item.NricNumber,
            PassportNumber = item.PassportNumber,
            PassportExpiryDate = item.PassportExpiryDate,
            MobileNumber = item.MobileNumber,
            Email = item.Email,
            ParentGuardianName = item.ParentGuardianName,
            ParentGuardianMobile = item.ParentGuardianMobile,
            Gender = item.Gender.ToString(),
            DateOfBirth = item.DateOfBirth,
            TahapCode = item.TahapCode,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            CreatedByName = item.CreatedByNavigation?.FullNameAsPerId,
            UpdatedAt = item.UpdatedAt,
            UpdatedByName = item.UpdatedByNavigation?.FullNameAsPerId,
            RegistrationEntries = registrationEntries
        };

        return Page();
    }
}
