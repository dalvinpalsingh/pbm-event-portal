using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.Venues;

namespace PbmEventPortal.Web.Pages.Admin.Venues;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public VenueDetailsDTO? Venue { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var venue = await _dbContext.Venues
            .AsNoTracking()
            .Include(venue => venue.CompetitionYear)
            .FirstOrDefaultAsync(venue => venue.Id == id);

        if (venue == null)
        {
            return NotFound();
        }

        Venue = new VenueDetailsDTO
        {
            Id = venue.Id,
            CompetitionYearId = venue.CompetitionYearId,
            CompetitionYearNo = venue.CompetitionYear.YearNo,
            CompetitionYearDisplayName = venue.CompetitionYear.DisplayName,
            Code = venue.Code,
            Name = venue.Name,
            LocationText = venue.LocationText,
            Capacity = venue.Capacity,
            Notes = venue.Notes,
            IsActive = venue.IsActive
        };

        return Page();
    }
}
