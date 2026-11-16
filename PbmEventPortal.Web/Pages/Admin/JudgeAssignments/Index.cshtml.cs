using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.JudgeAssignments;

namespace PbmEventPortal.Web.Pages.Admin.JudgeAssignments;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<JudgeAssignmentListItemDTO> JudgeAssignments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        JudgeAssignments = await _dbContext.JudgeAssignments
            .AsNoTracking()
            .OrderByDescending(assignment => assignment.IsActive)
            .ThenByDescending(assignment => assignment.CompetitionCategory.Competition.CompetitionYear.YearNo)
            .ThenBy(assignment => assignment.CompetitionCategory.Competition.Code)
            .ThenBy(assignment => assignment.CompetitionCategory.SortOrder)
            .ThenBy(assignment => assignment.JudgeUser.FullNameAsPerId)
            .Select(assignment => new JudgeAssignmentListItemDTO
            {
                Id = assignment.Id,
                CompetitionYearNo = assignment.CompetitionCategory.Competition.CompetitionYear.YearNo,
                CompetitionYearDisplayName = assignment.CompetitionCategory.Competition.CompetitionYear.DisplayName,
                CompetitionCode = assignment.CompetitionCategory.Competition.Code,
                CompetitionNameEnglish = assignment.CompetitionCategory.Competition.NameEnglish,
                CategoryCode = assignment.CompetitionCategory.CategoryCode,
                EventType = assignment.CompetitionCategory.EventType,
                JudgeName = assignment.JudgeUser.FullNameAsPerId,
                JudgeEmail = assignment.JudgeUser.Email,
                CriterionName = assignment.Criterion == null ? null : assignment.Criterion.CriterionName,
                CriterionNo = assignment.Criterion == null ? null : assignment.Criterion.CriterionNo,
                ScheduleDate = assignment.ScheduleItem == null ? null : assignment.ScheduleItem.ScheduleDate,
                ScheduleStartTime = assignment.ScheduleItem == null ? null : assignment.ScheduleItem.StartTime,
                ScheduleEndTime = assignment.ScheduleItem == null ? null : assignment.ScheduleItem.EndTime,
                ScheduleTitle = assignment.ScheduleItem == null ? null : assignment.ScheduleItem.DisplayTitle,
                IsActive = assignment.IsActive
            })
            .ToListAsync();
    }
}
