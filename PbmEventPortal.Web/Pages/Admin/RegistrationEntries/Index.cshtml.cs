using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.RegistrationEntries;

namespace PbmEventPortal.Web.Pages.Admin.RegistrationEntries;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<RegistrationEntryListItemViewModel> RegistrationEntries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        RegistrationEntries = await _dbContext.RegistrationEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.IsActive)
            .ThenByDescending(entry => entry.CompetitionCategory.Competition.CompetitionYear.YearNo)
            .ThenBy(entry => entry.CompetitionCategory.Competition.Code)
            .ThenBy(entry => entry.CompetitionCategory.SortOrder)
            .ThenBy(entry => entry.Team.Code)
            .Select(entry => new RegistrationEntryListItemViewModel
            {
                Id = entry.Id,
                CompetitionYearDisplay = entry.CompetitionCategory.Competition.CompetitionYear.DisplayName,
                CompetitionDisplay = entry.CompetitionCategory.Competition.Code + " - " + entry.CompetitionCategory.Competition.NameEnglish,
                CategoryDisplay = entry.CompetitionCategory.CategoryCode + " / " + entry.CompetitionCategory.EventType,
                EventType = entry.EntryType,
                TeamDisplay = entry.Team.Code + " - " + entry.Team.NameEnglish,
                EntryNameDisplay = string.IsNullOrWhiteSpace(entry.EntryName) ? "-" : entry.EntryName,
                EntryStatus = entry.EntryStatus,
                IsActive = entry.IsActive,
                ActiveMemberCount = entry.RegistrationEntryMembers.Count(member => member.IsActive),
                MemberSummary = entry.RegistrationEntryMembers
                    .Where(member => member.IsActive)
                    .OrderBy(member => member.MemberOrder)
                    .Select(member => member.Participant.FullNameAsPerId)
                    .FirstOrDefault() ?? "No active members"
            })
            .ToListAsync();
    }
}
