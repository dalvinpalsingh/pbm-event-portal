using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionScoringCriteria;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<CompetitionScoringCriterionListItemDTO> ScoringCriteria { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ScoringCriteria = await _dbContext.Competitions
            .AsNoTracking()
            .Where(competition => competition.CompetitionScoringCriteria.Any())
            .OrderByDescending(competition => competition.CompetitionYear.YearNo)
            .ThenBy(competition => competition.NameEnglish)
            .Select(competition => new CompetitionScoringCriterionListItemDTO
            {
                CompetitionId = competition.Id,
                CompetitionCode = competition.Code,
                CompetitionNameEnglish = competition.NameEnglish,
                CompetitionNameGurmukhi = competition.NameGurmukhi,
                CompetitionYearNo = competition.CompetitionYear.YearNo,
                CompetitionType = competition.CompetitionType,
                Criteria = competition.CompetitionScoringCriteria
                    .OrderBy(criterion => criterion.SortOrder)
                    .ThenBy(criterion => criterion.CriterionNo)
                    .ThenBy(criterion => criterion.CreatedAt)
                    .Select(criterion => new CompetitionScoringCriterionListCriterionViewModel
                    {
                        Id = criterion.Id,
                        CriterionNo = criterion.CriterionNo,
                        CriterionName = criterion.CriterionName,
                        IsActive = criterion.IsActive
                    })
                    .ToList()
            })
            .ToListAsync();
    }
}
