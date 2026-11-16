using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.RegistrationEntries;

namespace PbmEventPortal.Web.Pages.Admin.RegistrationEntries;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public RegistrationEntryDetailsViewModel? RegistrationEntry { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var entry = await _dbContext.RegistrationEntries
            .AsNoTracking()
            .Include(x => x.CompetitionCategory).ThenInclude(x => x.Competition).ThenInclude(x => x.CompetitionYear)
            .Include(x => x.Team)
            .Include(x => x.ApprovedByNavigation)
            .Include(x => x.CreatedByNavigation)
            .Include(x => x.UpdatedByNavigation)
            .Include(x => x.RegistrationEntryMembers).ThenInclude(x => x.Participant).ThenInclude(x => x.Team)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entry == null) return NotFound();

        RegistrationEntry = new RegistrationEntryDetailsViewModel
        {
            Id = entry.Id,
            CompetitionYearDisplay = entry.CompetitionCategory.Competition.CompetitionYear.DisplayName,
            CompetitionDisplay = entry.CompetitionCategory.Competition.Code + " - " + entry.CompetitionCategory.Competition.NameEnglish,
            CategoryDisplay = entry.CompetitionCategory.CategoryCode + " / " + entry.CompetitionCategory.EventType,
            EventType = entry.EntryType,
            TeamDisplay = entry.Team.Code + " - " + entry.Team.NameEnglish,
            EntryNameDisplay = string.IsNullOrWhiteSpace(entry.EntryName) ? "-" : entry.EntryName,
            EntryStatus = entry.EntryStatus,
            ApprovedByDisplay = entry.ApprovedByNavigation?.FullNameAsPerId ?? "-",
            RejectedReasonDisplay = string.IsNullOrWhiteSpace(entry.RejectedReason) ? "-" : entry.RejectedReason,
            NotesDisplay = string.IsNullOrWhiteSpace(entry.Notes) ? "-" : entry.Notes,
            IsActive = entry.IsActive,
            CreatedAt = entry.CreatedAt,
            CreatedByDisplay = entry.CreatedByNavigation?.FullNameAsPerId ?? "System / unknown",
            UpdatedAt = entry.UpdatedAt,
            UpdatedByDisplay = entry.UpdatedByNavigation?.FullNameAsPerId ?? "Not updated yet",
            Participants = entry.RegistrationEntryMembers
                .OrderBy(participant => participant.MemberOrder)
                .Select(participant => new RegistrationEntryParticipantViewModel
                {
                    Id = participant.Id,
                    ParticipantId = participant.ParticipantId,
                    ParticipantOrder = participant.MemberOrder,
                    ParticipantName = participant.Participant.FullNameAsPerId,
                    ParticipantTahapCode = participant.Participant.TahapCode,
                    ParticipantTeamCode = participant.Participant.Team.Code,
                    IsActive = participant.IsActive
                })
                .ToList()
        };

        return Page();
    }
}
