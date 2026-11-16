using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

namespace PbmEventPortal.Web.Pages.Admin.PointsSchemes;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<PointsSchemeListItemDTO> PointsSchemes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PointsSchemes = await _dbContext.PointsSchemes
            .AsNoTracking()
            .OrderByDescending(scheme => scheme.IsActive)
            .ThenBy(scheme => scheme.WinnerCount)
            .ThenBy(scheme => scheme.SchemeName)
            .Select(scheme => new PointsSchemeListItemDTO
            {
                Id = scheme.Id,
                SchemeName = scheme.SchemeName,
                WinnerCount = scheme.WinnerCount,
                IsActive = scheme.IsActive,
                Items = scheme.PointsSchemeItems
                    .OrderBy(item => item.PlacingNo)
                    .Select(item => new PointsSchemeItemListItemViewModel
                    {
                        PlacingNo = item.PlacingNo,
                        PointsAwarded = item.PointsAwarded
                    })
                    .ToList()
            })
            .ToListAsync();
    }
}
