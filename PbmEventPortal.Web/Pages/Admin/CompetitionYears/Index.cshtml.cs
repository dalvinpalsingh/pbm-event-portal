using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionYears;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionYears;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<CompetitionYearListItemDTO> CompetitionYears { get; private set; } = [];

    public async Task OnGetAsync()
    {
        CompetitionYears = await _dbContext.CompetitionYears
            .AsNoTracking()
            .OrderByDescending(competitionYear => competitionYear.YearNo)
            .Select(competitionYear => new CompetitionYearListItemDTO
            {
                Id = competitionYear.Id,
                YearNo = competitionYear.YearNo,
                DisplayName = competitionYear.DisplayName,
                RegistrationOpenAt = competitionYear.RegistrationOpenAt,
                RegistrationCloseAt = competitionYear.RegistrationCloseAt,
                IsActive = competitionYear.IsActive,
                CompetitionCount = competitionYear.Competitions.Count,
                ParticipantCount = competitionYear.Participants.Count,
                VenueCount = competitionYear.Venues.Count,
                ScheduleItemCount = competitionYear.ScheduleItems.Count
            })
            .ToListAsync();
    }
}