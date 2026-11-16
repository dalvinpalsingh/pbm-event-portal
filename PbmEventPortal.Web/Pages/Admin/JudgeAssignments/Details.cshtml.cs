using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.JudgeAssignments;

namespace PbmEventPortal.Web.Pages.Admin.JudgeAssignments;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public JudgeAssignmentDetailsDTO? Assignment { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Assignment = await _dbContext.JudgeAssignments
            .AsNoTracking()
            .Where(assignment => assignment.Id == id)
            .Select(assignment => new JudgeAssignmentDetailsDTO
            {
                Id = assignment.Id,
                CompetitionYearDisplay = assignment.CompetitionCategory.Competition.CompetitionYear.DisplayName,
                CompetitionDisplay = assignment.CompetitionCategory.Competition.Code + " - " + assignment.CompetitionCategory.Competition.NameEnglish,
                CategoryDisplay = assignment.CompetitionCategory.CategoryCode,
                EventType = assignment.CompetitionCategory.EventType,
                JudgeName = assignment.JudgeUser.FullNameAsPerId,
                JudgeEmail = assignment.JudgeUser.Email,
                JudgeMobileNumber = assignment.JudgeUser.MobileNumber,
                CriterionDisplay = assignment.Criterion == null
                    ? "All criteria"
                    : assignment.Criterion.CriterionNo + ". " + assignment.Criterion.CriterionName,
                ScheduleDisplay = assignment.ScheduleItem == null
                    ? "Not linked"
                    : assignment.ScheduleItem.ScheduleDate.ToString("yyyy-MM-dd") + " " + assignment.ScheduleItem.StartTime.ToString("HH:mm") + "-" + assignment.ScheduleItem.EndTime.ToString("HH:mm") + " / " + assignment.ScheduleItem.DisplayTitle,
                IsActive = assignment.IsActive,
                Notes = assignment.Notes,
                CreatedAt = assignment.CreatedAt,
                CreatedByName = assignment.CreatedByNavigation == null ? null : assignment.CreatedByNavigation.FullNameAsPerId,
                UpdatedAt = assignment.UpdatedAt,
                UpdatedByName = assignment.UpdatedByNavigation == null ? null : assignment.UpdatedByNavigation.FullNameAsPerId
            })
            .FirstOrDefaultAsync();

        return Assignment == null ? NotFound() : Page();
    }
}
