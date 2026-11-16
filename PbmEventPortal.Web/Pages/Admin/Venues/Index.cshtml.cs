using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Venues;

namespace PbmEventPortal.Web.Pages.Admin.Venues;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<VenueListItemDTO> Venues { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Venues = await _dbContext.Venues
            .AsNoTracking()
            .OrderByDescending(venue => venue.IsActive)
            .ThenByDescending(venue => venue.CompetitionYear.YearNo)
            .ThenBy(venue => venue.Code)
            .Select(venue => new VenueListItemDTO
            {
                Id = venue.Id,
                CompetitionYearNo = venue.CompetitionYear.YearNo,
                CompetitionYearDisplayName = venue.CompetitionYear.DisplayName,
                Code = venue.Code,
                Name = venue.Name,
                LocationText = venue.LocationText,
                Capacity = venue.Capacity,
                IsActive = venue.IsActive
            })
            .ToListAsync();
    }
}
