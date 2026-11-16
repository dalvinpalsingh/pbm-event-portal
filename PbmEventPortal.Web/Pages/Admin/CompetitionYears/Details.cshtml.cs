using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.CompetitionYears;

namespace PbmEventPortal.Web.Pages.Admin.CompetitionYears;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public CompetitionYearDetailsDTO? CompetitionYear { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var loaded = await LoadDetailsPageAsync(id);

        return loaded
            ? Page()
            : NotFound();
    }

    private async Task<bool> LoadDetailsPageAsync(Guid id)
    {
        CompetitionYear = await _dbContext.CompetitionYears
            .AsNoTracking()
            .Where(competitionYear => competitionYear.Id == id)
            .Select(competitionYear => new CompetitionYearDetailsDTO
            {
                Id = competitionYear.Id,
                YearNo = competitionYear.YearNo,
                DisplayName = competitionYear.DisplayName,
                RegistrationOpenAt = competitionYear.RegistrationOpenAt,
                RegistrationCloseAt = competitionYear.RegistrationCloseAt,
                IsActive = competitionYear.IsActive,
                CreatedAt = competitionYear.CreatedAt,
                CreatedBy = competitionYear.CreatedBy,
                UpdatedAt = competitionYear.UpdatedAt,
                UpdatedBy = competitionYear.UpdatedBy,
                CompetitionCount = competitionYear.Competitions.Count,
                ParticipantCount = competitionYear.Participants.Count,
                VenueCount = competitionYear.Venues.Count,
                ScheduleItemCount = competitionYear.ScheduleItems.Count
            })
            .FirstOrDefaultAsync();

        if (CompetitionYear == null)
        {
            return false;
        }

        await LoadAuditActorsAsync(CompetitionYear);

        return true;
    }

    private async Task LoadAuditActorsAsync(CompetitionYearDetailsDTO competitionYear)
    {
        var auditActorIds = new[]
            {
                competitionYear.CreatedBy,
                competitionYear.UpdatedBy
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
            .Select(user => new CompetitionYearAuditActorDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi
            })
            .ToDictionaryAsync(user => user.Id);

        if (competitionYear.CreatedBy.HasValue &&
            auditActors.TryGetValue(competitionYear.CreatedBy.Value, out var createdByUser))
        {
            competitionYear.CreatedByUser = createdByUser;
        }

        if (competitionYear.UpdatedBy.HasValue &&
            auditActors.TryGetValue(competitionYear.UpdatedBy.Value, out var updatedByUser))
        {
            competitionYear.UpdatedByUser = updatedByUser;
        }
    }
}
