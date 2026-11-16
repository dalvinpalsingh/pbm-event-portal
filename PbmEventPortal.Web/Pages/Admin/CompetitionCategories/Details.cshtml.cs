using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionCategories;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionCategories;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public CompetitionCategoryDetailsDTO? CompetitionCategory { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        CompetitionCategory = await _dbContext.CompetitionCategories
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CompetitionCategoryDetailsDTO
            {
                Id = x.Id,
                CompetitionId = x.CompetitionId,
                CompetitionYearNo = x.Competition.CompetitionYear.YearNo,
                CompetitionNameEnglish = x.Competition.NameEnglish,
                CompetitionNameGurmukhi = x.Competition.NameGurmukhi,
                CompetitionType = x.Competition.CompetitionType,
                CategoryCode = x.CategoryCode,
                CategoryName = x.CategoryCodeNavigation.Name,
                GroupingType = x.CategoryCodeNavigation.GroupingType,
                MinAge = x.CategoryCodeNavigation.MinAge,
                MaxAge = x.CategoryCodeNavigation.MaxAge,
                EventType = x.EventType,
                SortOrder = x.SortOrder,
                IndividualEventMaxParticipants = x.IndividualEventMaxParticipants,
                TeamEventMaxEntriesPerTeam = x.TeamEventMaxEntriesPerTeam,
                TeamEventTeamSize = x.TeamEventTeamSize,
                TeamEventGenderRestrictions = x.TeamEventGenderRestrictions,
                TeamEventYoungerSubstitutionAllowed = x.TeamEventYoungerSubstitutionAllowed,
                WinnerCount = x.WinnerCount,
                Notes = x.Notes,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .FirstOrDefaultAsync();

        if (CompetitionCategory == null)
        {
            return NotFound();
        }

        await LoadAuditActorsAsync();
        return Page();
    }

    private async Task LoadAuditActorsAsync()
    {
        if (CompetitionCategory == null)
        {
            return;
        }

        var auditActorIds = new[] { CompetitionCategory.CreatedBy, CompetitionCategory.UpdatedBy }
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
            .Select(user => new CompetitionCategoryAuditActorDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi
            })
            .ToDictionaryAsync(user => user.Id);

        if (CompetitionCategory.CreatedBy.HasValue && auditActors.TryGetValue(CompetitionCategory.CreatedBy.Value, out var createdByUser))
        {
            CompetitionCategory.CreatedByUser = createdByUser;
        }

        if (CompetitionCategory.UpdatedBy.HasValue && auditActors.TryGetValue(CompetitionCategory.UpdatedBy.Value, out var updatedByUser))
        {
            CompetitionCategory.UpdatedByUser = updatedByUser;
        }
    }
}
