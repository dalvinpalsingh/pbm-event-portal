using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

namespace PbmEventPortal.Web.Pages.Admin.PointsSchemes;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public PointsSchemeDetailsDTO? PointsScheme { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var scheme = await _dbContext.PointsSchemes
            .AsNoTracking()
            .Include(scheme => scheme.PointsSchemeItems)
            .FirstOrDefaultAsync(scheme => scheme.Id == id);

        if (scheme == null)
        {
            return NotFound();
        }

        PointsScheme = new PointsSchemeDetailsDTO
        {
            Id = scheme.Id,
            SchemeName = scheme.SchemeName,
            WinnerCount = scheme.WinnerCount,
            IsActive = scheme.IsActive,
            Items = scheme.PointsSchemeItems
                .OrderBy(item => item.PlacingNo)
                .Select(item => new PointsSchemeDetailsItemViewModel
                {
                    Id = item.Id,
                    PlacingNo = item.PlacingNo,
                    PointsAwarded = item.PointsAwarded
                })
                .ToList()
        };

        return Page();
    }
}
