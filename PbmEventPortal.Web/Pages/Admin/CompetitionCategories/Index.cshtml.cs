using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionCategories;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionCategories;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<CompetitionCategoryListItemDTO> CompetitionCategories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        CompetitionCategories = await _dbContext.CompetitionCategories
            .AsNoTracking()
            .OrderByDescending(competitionCategory => competitionCategory.Competition.CompetitionYear.YearNo)
            .ThenBy(competitionCategory => competitionCategory.Competition.NameEnglish)
            .ThenBy(competitionCategory => competitionCategory.SortOrder)
            .ThenBy(competitionCategory => competitionCategory.CategoryCode)
            .Select(competitionCategory => new CompetitionCategoryListItemDTO
            {
                Id = competitionCategory.Id,
                CompetitionId = competitionCategory.CompetitionId,
                CompetitionNameEnglish = competitionCategory.Competition.NameEnglish,
                CompetitionNameGurmukhi = competitionCategory.Competition.NameGurmukhi,
                CompetitionYearNo = competitionCategory.Competition.CompetitionYear.YearNo,
                CategoryCode = competitionCategory.CategoryCode,
                CategoryName = competitionCategory.CategoryCodeNavigation.Name,
                EventType = competitionCategory.EventType,
                GroupingType = competitionCategory.CategoryCodeNavigation.GroupingType,
                SortOrder = competitionCategory.SortOrder,
                IndividualEventMaxParticipants = competitionCategory.IndividualEventMaxParticipants,
                TeamEventMaxEntriesPerTeam = competitionCategory.TeamEventMaxEntriesPerTeam,
                TeamEventTeamSize = competitionCategory.TeamEventTeamSize,
                TeamEventGenderRestrictions = competitionCategory.TeamEventGenderRestrictions,
                TeamEventYoungerSubstitutionAllowed = competitionCategory.TeamEventYoungerSubstitutionAllowed,
                WinnerCount = competitionCategory.WinnerCount,
                Notes = competitionCategory.Notes,
                IsActive = competitionCategory.IsActive
            })
            .ToListAsync();
    }
}
