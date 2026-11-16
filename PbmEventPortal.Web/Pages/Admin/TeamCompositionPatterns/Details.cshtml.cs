using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.TeamCompositionPatterns;

namespace PbmEventPortal.Web.Pages.Admin.TeamCompositionPatterns;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public TeamCompositionPatternDetailsViewModel? TeamCompositionPattern { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var pattern = await _dbContext.TeamCompositionPatterns
            .AsNoTracking()
            .Include(x => x.CompetitionCategory).ThenInclude(x => x.Competition).ThenInclude(x => x.CompetitionYear)
            .Include(x => x.TeamCompositionPatternSlots).ThenInclude(x => x.TahapCodeNavigation)
            .Include(x => x.CreatedByNavigation)
            .Include(x => x.UpdatedByNavigation)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (pattern == null)
        {
            return NotFound();
        }

        TeamCompositionPattern = new TeamCompositionPatternDetailsViewModel
        {
            Id = pattern.Id,
            CompetitionYearDisplay = pattern.CompetitionCategory.Competition.CompetitionYear.DisplayName,
            CompetitionDisplay = pattern.CompetitionCategory.Competition.Code + " - " + pattern.CompetitionCategory.Competition.NameEnglish,
            CategoryDisplay = pattern.CompetitionCategory.CategoryCode + " / " + pattern.CompetitionCategory.EventType,
            PatternCode = pattern.PatternCode,
            TeamSize = pattern.CompetitionCategory.TeamEventTeamSize ?? 0,
            Slots = pattern.TeamCompositionPatternSlots
                .OrderBy(slot => slot.SlotNo)
                .Select(slot => new TeamCompositionPatternSlotViewModel
                {
                    Id = slot.Id,
                    SlotNo = slot.SlotNo,
                    TahapCode = slot.TahapCode,
                    TahapDisplay = slot.TahapCodeNavigation.Name,
                    IsActive = slot.IsActive
                })
                .ToList(),
            AllowYoungerSubstitution = pattern.AllowYoungerSubstitution,
            NotesDisplay = string.IsNullOrWhiteSpace(pattern.Notes) ? "-" : pattern.Notes,
            IsActive = pattern.IsActive,
            CreatedAt = pattern.CreatedAt,
            CreatedByDisplay = pattern.CreatedByNavigation?.FullNameAsPerId ?? "System / unknown",
            UpdatedAt = pattern.UpdatedAt,
            UpdatedByDisplay = pattern.UpdatedByNavigation?.FullNameAsPerId ?? "Not updated yet"
        };

        return Page();
    }
}
