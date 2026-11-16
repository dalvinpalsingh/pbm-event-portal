using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Competitions;

namespace PbmEventPortal.Web.Pages.Admin.Competitions;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public CompetitionDetailsDTO? Competition { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var loaded = await LoadDetailsPageAsync(id);

        return loaded
            ? Page()
            : NotFound();
    }

    private async Task<bool> LoadDetailsPageAsync(Guid id)
    {
        Competition = await _dbContext.Competitions
            .AsNoTracking()
            .Where(competition => competition.Id == id)
            .Select(competition => new CompetitionDetailsDTO
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
                Notes = competition.Notes,
                IsActive = competition.IsActive,
                CreatedAt = competition.CreatedAt,
                CreatedBy = competition.CreatedBy,
                UpdatedAt = competition.UpdatedAt,
                UpdatedBy = competition.UpdatedBy,
                CategoryCount = competition.CompetitionCategories.Count,
                ScoringCriteriaCount = competition.CompetitionScoringCriteria.Count
            })
            .FirstOrDefaultAsync();

        if (Competition == null)
        {
            return false;
        }

        await LoadAuditActorsAsync(Competition);

        return true;
    }

    private async Task LoadAuditActorsAsync(CompetitionDetailsDTO competition)
    {
        var auditActorIds = new[]
            {
                competition.CreatedBy,
                competition.UpdatedBy
            }
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToList();

        if (auditActorIds.Count == 0)
        {
            return;
        }

        var auditActors = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => auditActorIds.Contains(user.Id))
            .Select(user => new CompetitionAuditActorDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi
            })
            .ToDictionaryAsync(user => user.Id);

        if (competition.CreatedBy.HasValue &&
            auditActors.TryGetValue(competition.CreatedBy.Value, out var createdByUser))
        {
            competition.CreatedByUser = createdByUser;
        }

        if (competition.UpdatedBy.HasValue &&
            auditActors.TryGetValue(competition.UpdatedBy.Value, out var updatedByUser))
        {
            competition.UpdatedByUser = updatedByUser;
        }
    }
}
