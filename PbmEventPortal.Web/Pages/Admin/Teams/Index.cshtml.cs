using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Teams;

namespace PbmEventPortal.Web.Pages.Admin.Teams;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<TeamListItemDTO> Teams { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Teams = await _dbContext.Teams
            .AsNoTracking()
            .OrderBy(team => team.NameEnglish)
            .Select(team => new TeamListItemDTO
            {
                Id = team.Id,
                Code = team.Code,
                NameEnglish = team.NameEnglish,
                NameGurmukhi = team.NameGurmukhi,
                StateName = team.State.Name,
                RegionName = team.Region.Name,
                IsActive = team.IsActive,
                TeamManagerCount = team.UserRoleAssignments.Count(roleAssignment =>
                    roleAssignment.RoleCode == RoleEnums.TeamManager &&
                    roleAssignment.IsActive &&
                    roleAssignment.IsVerified)
            })
            .ToListAsync();
    }
}
