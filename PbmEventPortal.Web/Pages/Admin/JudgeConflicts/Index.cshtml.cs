using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.JudgeConflicts;

namespace PbmEventPortal.Web.Pages.Admin.JudgeConflicts;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<JudgeConflictListItemDTO> JudgeConflicts { get; private set; } = [];

    public async Task OnGetAsync()
    {
        JudgeConflicts = await _dbContext.JudgeConflicts
            .AsNoTracking()
            .OrderByDescending(conflict => conflict.IsActive)
            .ThenBy(conflict => conflict.JudgeUser.FullNameAsPerId)
            .ThenBy(conflict => conflict.ConflictType)
            .Select(conflict => new JudgeConflictListItemDTO
            {
                Id = conflict.Id,
                JudgeName = conflict.JudgeUser.FullNameAsPerId,
                JudgeEmail = conflict.JudgeUser.Email,
                ConflictType = conflict.ConflictType,
                TeamCode = conflict.Team == null ? null : conflict.Team.Code,
                TeamName = conflict.Team == null ? null : conflict.Team.NameEnglish,
                ParticipantName = conflict.Participant == null ? null : conflict.Participant.FullNameAsPerId,
                ParticipantTeamCode = conflict.Participant == null ? null : conflict.Participant.Team.Code,
                CompetitionCode = conflict.CompetitionCategory == null ? null : conflict.CompetitionCategory.Competition.Code,
                CompetitionName = conflict.CompetitionCategory == null ? null : conflict.CompetitionCategory.Competition.NameEnglish,
                CategoryCode = conflict.CompetitionCategory == null ? null : conflict.CompetitionCategory.CategoryCode,
                IsActive = conflict.IsActive
            })
            .ToListAsync();
    }
}
