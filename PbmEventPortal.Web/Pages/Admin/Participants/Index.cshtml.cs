using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Participants;

namespace PbmEventPortal.Web.Pages.Admin.Participants;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<ParticipantListItemDTO> Participants { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Participants = await _dbContext.Participants
            .AsNoTracking()
            .OrderByDescending(participant => participant.IsActive)
            .ThenByDescending(participant => participant.CompetitionYear.YearNo)
            .ThenBy(participant => participant.Team.Code)
            .ThenBy(participant => participant.FullNameAsPerId)
            .Select(participant => new ParticipantListItemDTO
            {
                Id = participant.Id,
                CompetitionYearDisplayName = participant.CompetitionYear.DisplayName,
                TeamCode = participant.Team.Code,
                TeamName = participant.Team.NameEnglish,
                FullNameAsPerId = participant.FullNameAsPerId,
                FullNameGurmukhi = participant.FullNameGurmukhi,
                IdType = participant.IdType,
                NricNumber = participant.NricNumber,
                PassportNumber = participant.PassportNumber,
                Gender = participant.Gender.ToString(),
                DateOfBirth = participant.DateOfBirth,
                TahapCode = participant.TahapCode,
                IsActive = participant.IsActive
            })
            .ToListAsync();
    }
}
