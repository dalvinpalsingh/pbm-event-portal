using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Competitions;

namespace PbmEventPortal.Web.Pages.Admin.Competitions;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<CompetitionListItemDTO> Competitions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Competitions = await _dbContext.Competitions
            .AsNoTracking()
            .OrderByDescending(competition => competition.CompetitionYear.YearNo)
            .ThenBy(competition => competition.Code)
            .Select(competition => new CompetitionListItemDTO
            {
                Id = competition.Id,
                CompetitionYearId = competition.CompetitionYearId,
                CompetitionYearNo = competition.CompetitionYear.YearNo,
                CompetitionYearDisplayName = competition.CompetitionYear.DisplayName,
                Code = competition.Code,
                NameEnglish = competition.NameEnglish,
                NameGurmukhi = competition.NameGurmukhi,
                CompetitionType = competition.CompetitionType,
                CriteriaCount = competition.CriteriaCount,
                MinJudges = competition.MinJudges,
                MaxJudges = competition.MaxJudges,
                IsActive = competition.IsActive,
                CategoryCount = competition.CompetitionCategories.Count,
                ScoringCriteriaCount = competition.CompetitionScoringCriteria.Count
            })
            .ToListAsync();
    }
}
