using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionScoringCriteria;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionScoringCriteria;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public CompetitionScoringCriterionDetailsViewModel? ScoringCriteria { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        ScoringCriteria = await _dbContext.Competitions
            .AsNoTracking()
            .Where(competition =>
                competition.Id == id &&
                competition.CompetitionScoringCriteria.Any())
            .Select(competition => new CompetitionScoringCriterionDetailsViewModel
            {
                CompetitionId = competition.Id,
                CompetitionYearNo = competition.CompetitionYear.YearNo,
                CompetitionCode = competition.Code,
                CompetitionNameEnglish = competition.NameEnglish,
                CompetitionNameGurmukhi = competition.NameGurmukhi,
                CompetitionType = competition.CompetitionType,
                CompetitionCriteriaCount = competition.CriteriaCount,
                Criteria = competition.CompetitionScoringCriteria
                    .OrderBy(criterion => criterion.SortOrder)
                    .ThenBy(criterion => criterion.CriterionNo)
                    .ThenBy(criterion => criterion.CreatedAt)
                    .Select(criterion => new CompetitionScoringCriterionDetailsRowViewModel
                    {
                        Id = criterion.Id,
                        CriterionNo = criterion.CriterionNo,
                        CriterionName = criterion.CriterionName,
                        MaxScore = criterion.MaxScore,
                        SortOrder = criterion.SortOrder,
                        IsActive = criterion.IsActive
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (ScoringCriteria == null)
        {
            return NotFound();
        }

        return Page();
    }
}
