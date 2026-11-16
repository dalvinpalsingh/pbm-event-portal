using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.TeamCompositionPatterns;

namespace PbmEventPortal.Web.Pages.Admin.TeamCompositionPatterns;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<TeamCompositionPatternListItemViewModel> TeamCompositionPatterns { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var patterns = await _dbContext.TeamCompositionPatterns
            .AsNoTracking()
            .Include(pattern => pattern.CompetitionCategory)
                .ThenInclude(category => category.Competition)
                .ThenInclude(competition => competition.CompetitionYear)
            .Include(pattern => pattern.TeamCompositionPatternSlots)
                .ThenInclude(slot => slot.TahapCodeNavigation)
            .OrderByDescending(pattern => pattern.IsActive)
            .ThenByDescending(pattern => pattern.CompetitionCategory.Competition.CompetitionYear.YearNo)
            .ThenBy(pattern => pattern.CompetitionCategory.Competition.Code)
            .ThenBy(pattern => pattern.CompetitionCategory.SortOrder)
            .ThenBy(pattern => pattern.PatternCode)
            .ToListAsync();

        TeamCompositionPatterns = patterns
            .Select(pattern => new TeamCompositionPatternListItemViewModel
            {
                Id = pattern.Id,
                CompetitionYearDisplay = pattern.CompetitionCategory.Competition.CompetitionYear.DisplayName,
                CompetitionDisplay = pattern.CompetitionCategory.Competition.Code + " - " + pattern.CompetitionCategory.Competition.NameEnglish,
                CategoryDisplay = pattern.CompetitionCategory.CategoryCode + " / " + pattern.CompetitionCategory.EventType,
                PatternCode = pattern.PatternCode,
                TeamSize = pattern.CompetitionCategory.TeamEventTeamSize ?? 0,
                SlotSummary = string.Join(" + ", pattern.TeamCompositionPatternSlots
                    .Where(slot => slot.IsActive)
                    .OrderBy(slot => slot.SlotNo)
                    .Select(slot => slot.TahapCodeNavigation.Name)),
                AllowYoungerSubstitution = pattern.AllowYoungerSubstitution,
                IsActive = pattern.IsActive
            })
            .ToList();
    }
}
