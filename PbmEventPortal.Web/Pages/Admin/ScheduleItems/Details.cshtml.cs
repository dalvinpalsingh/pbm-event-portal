using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.ScheduleItems;

namespace PbmEventPortal.Web.Pages.Admin.ScheduleItems;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ScheduleItemDetailsDTO? ScheduleItem { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var scheduleItem = await _dbContext.ScheduleItems
            .AsNoTracking()
            .Include(scheduleItem => scheduleItem.CompetitionYear)
            .Include(scheduleItem => scheduleItem.CompetitionCategory)
                .ThenInclude(competitionCategory => competitionCategory!.Competition)
            .Include(scheduleItem => scheduleItem.Venue)
            .FirstOrDefaultAsync(scheduleItem => scheduleItem.Id == id);

        if (scheduleItem == null)
        {
            return NotFound();
        }

        ScheduleItem = new ScheduleItemDetailsDTO
        {
            Id = scheduleItem.Id,
            CompetitionYearId = scheduleItem.CompetitionYearId,
            CompetitionYearNo = scheduleItem.CompetitionYear.YearNo,
            CompetitionYearDisplayName = scheduleItem.CompetitionYear.DisplayName,
            ScheduleDate = scheduleItem.ScheduleDate,
            StartTime = scheduleItem.StartTime,
            EndTime = scheduleItem.EndTime,
            ReportingTime = scheduleItem.ReportingTime,
            DisplayTitle = scheduleItem.DisplayTitle,
            ScheduleType = scheduleItem.ScheduleType,
            CompetitionCategoryId = scheduleItem.CompetitionCategoryId,
            CompetitionCode = scheduleItem.CompetitionCategory?.Competition.Code,
            CompetitionNameEnglish = scheduleItem.CompetitionCategory?.Competition.NameEnglish,
            CategoryCode = scheduleItem.CompetitionCategory?.CategoryCode,
            VenueId = scheduleItem.VenueId,
            VenueCode = scheduleItem.Venue?.Code,
            VenueName = scheduleItem.Venue?.Name,
            VenueLocationText = scheduleItem.Venue?.LocationText,
            SessionGroup = scheduleItem.SessionGroup,
            SequenceNo = scheduleItem.SequenceNo,
            Status = scheduleItem.Status,
            IsPublished = scheduleItem.IsPublished,
            Notes = scheduleItem.Notes
        };

        return Page();
    }
}
