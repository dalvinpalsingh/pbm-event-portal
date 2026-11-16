using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.JudgeConflicts;

namespace PbmEventPortal.Web.Pages.Admin.JudgeConflicts;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public JudgeConflictDetailsDTO? Conflict { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Conflict = await _dbContext.JudgeConflicts
            .AsNoTracking()
            .Where(conflict => conflict.Id == id)
            .Select(conflict => new JudgeConflictDetailsDTO
            {
                Id = conflict.Id,
                JudgeName = conflict.JudgeUser.FullNameAsPerId,
                JudgeEmail = conflict.JudgeUser.Email,
                JudgeMobileNumber = conflict.JudgeUser.MobileNumber,
                ConflictType = conflict.ConflictType,
                TargetDisplay = conflict.Team != null
                    ? conflict.Team.Code + " - " + conflict.Team.NameEnglish
                    : conflict.Participant != null
                        ? conflict.Participant.FullNameAsPerId + " / " + conflict.Participant.Team.Code
                        : conflict.CompetitionCategory != null
                            ? conflict.CompetitionCategory.Competition.Code + " / " + conflict.CompetitionCategory.CategoryCode + " - " + conflict.CompetitionCategory.Competition.NameEnglish
                            : "Custom / not linked",
                Notes = conflict.Notes,
                IsActive = conflict.IsActive,
                CreatedAt = conflict.CreatedAt,
                CreatedByName = conflict.CreatedByNavigation == null ? null : conflict.CreatedByNavigation.FullNameAsPerId,
                UpdatedAt = conflict.UpdatedAt,
                UpdatedByName = conflict.UpdatedByNavigation == null ? null : conflict.UpdatedByNavigation.FullNameAsPerId
            })
            .FirstOrDefaultAsync();

        return Conflict == null ? NotFound() : Page();
    }
}
