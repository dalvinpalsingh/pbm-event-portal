using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

namespace PbmEventPortal.Web.Pages.Admin.UserProfiles;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<UserProfileListItemDTO> Users { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Users = await _dbContext.UserProfiles
            .AsNoTracking()
            .OrderBy(user => user.FullNameAsPerId)
            .Select(user => new UserProfileListItemDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi,
                IdType = user.IdType,
                NricNumber = user.NricNumber,
                PassportNumber = user.PassportNumber,
                MobileNumber = user.MobileNumber,
                Email = user.Email,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive
            })
            .ToListAsync();
    }
}