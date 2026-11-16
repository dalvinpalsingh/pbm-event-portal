using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.ScheduleItems;

namespace PbmEventPortal.Web.Pages.Admin.ScheduleItems;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<ScheduleItemListItemDTO> ScheduleItems { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ScheduleItems = await _dbContext.ScheduleItems
            .AsNoTracking()
            .OrderByDescending(scheduleItem => scheduleItem.CompetitionYear.YearNo)
            .ThenBy(scheduleItem => scheduleItem.ScheduleDate)
            .ThenBy(scheduleItem => scheduleItem.StartTime)
            .ThenBy(scheduleItem => scheduleItem.SequenceNo ?? short.MaxValue)
            .ThenBy(scheduleItem => scheduleItem.DisplayTitle)
            .Select(scheduleItem => new ScheduleItemListItemDTO
            {
                Id = scheduleItem.Id,
                CompetitionYearNo = scheduleItem.CompetitionYear.YearNo,
                CompetitionYearDisplayName = scheduleItem.CompetitionYear.DisplayName,
                ScheduleDate = scheduleItem.ScheduleDate,
                StartTime = scheduleItem.StartTime,
                EndTime = scheduleItem.EndTime,
                DisplayTitle = scheduleItem.DisplayTitle,
                ScheduleType = scheduleItem.ScheduleType,
                CompetitionCode = scheduleItem.CompetitionCategory == null ? null : scheduleItem.CompetitionCategory.Competition.Code,
                CompetitionNameEnglish = scheduleItem.CompetitionCategory == null ? null : scheduleItem.CompetitionCategory.Competition.NameEnglish,
                CategoryCode = scheduleItem.CompetitionCategory == null ? null : scheduleItem.CompetitionCategory.CategoryCode,
                VenueCode = scheduleItem.Venue == null ? null : scheduleItem.Venue.Code,
                VenueName = scheduleItem.Venue == null ? null : scheduleItem.Venue.Name,
                Status = scheduleItem.Status,
                IsPublished = scheduleItem.IsPublished
            })
            .ToListAsync();
    }
}
