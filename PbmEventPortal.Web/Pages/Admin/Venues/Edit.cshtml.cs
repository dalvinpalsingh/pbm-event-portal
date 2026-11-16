using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.Venues;

namespace PbmEventPortal.Web.Pages.Admin.Venues;

public class EditModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public EditModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public VenueEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var venue = await _dbContext.Venues
            .AsNoTracking()
            .FirstOrDefaultAsync(venue => venue.Id == id);

        if (venue == null)
        {
            return NotFound();
        }

        Input = MapToInput(venue);
        await LoadLookupOptionsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        if (Input.Id != id)
        {
            ModelState.AddModelError(string.Empty, "Invalid venue request.");
        }

        NormalizeInput();
        await ValidateVenueAsync(id);
        await LoadLookupOptionsAsync();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var venue = await _dbContext.Venues
            .FirstOrDefaultAsync(venue => venue.Id == id);

        if (venue == null)
        {
            return NotFound();
        }

        var oldSnapshot = CreateVenueSnapshot(venue);
        var oldIsActive = venue.IsActive;
        var now = DateTime.UtcNow;

        venue.CompetitionYearId = Input.CompetitionYearId!.Value;
        venue.Code = Input.Code;
        venue.Name = Input.Name;
        venue.LocationText = Input.LocationText;
        venue.Capacity = Input.Capacity;
        venue.Notes = Input.Notes;
        venue.IsActive = Input.IsActive;
        venue.UpdatedAt = now;
        venue.UpdatedBy = actorId.Value;

        var actionName = oldIsActive != venue.IsActive
            ? venue.IsActive ? "ACTIVATE" : "DEACTIVATE"
            : "UPDATE";

        var changeReason = actionName switch
        {
            "ACTIVATE" => "System Admin reactivated venue.",
            "DEACTIVATE" => "System Admin deactivated venue.",
            _ => "System Admin updated venue."
        };

        await _auditLogService.LogAsync(
            entityName: "venues",
            entityId: venue.Id,
            actionName: actionName,
            changedBy: actorId.Value,
            changeReason: changeReason,
            oldValues: oldSnapshot,
            newValues: CreateVenueSnapshot(venue));

        ResultIsSuccess = true;
        ResultTitle = "Venue updated";
        ResultMessage = "The venue has been updated successfully.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static VenueEditInputDTO MapToInput(Venue venue)
    {
        return new VenueEditInputDTO
        {
            Id = venue.Id,
            CompetitionYearId = venue.CompetitionYearId,
            Code = venue.Code,
            Name = venue.Name,
            LocationText = venue.LocationText,
            Capacity = venue.Capacity,
            Notes = venue.Notes,
            IsActive = venue.IsActive
        };
    }

    private void NormalizeInput()
    {
        Input.Code = string.IsNullOrWhiteSpace(Input.Code)
            ? string.Empty
            : Input.Code.Trim().ToUpperInvariant().Replace(" ", "_");

        Input.Name = string.IsNullOrWhiteSpace(Input.Name)
            ? string.Empty
            : Input.Name.Trim();

        Input.LocationText = string.IsNullOrWhiteSpace(Input.LocationText)
            ? null
            : Input.LocationText.Trim();

        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes)
            ? null
            : Input.Notes.Trim();
    }

    private async Task ValidateVenueAsync(Guid currentVenueId)
    {
        if (!Input.CompetitionYearId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        }
        else
        {
            var competitionYearExists = await _dbContext.CompetitionYears
                .AsNoTracking()
                .AnyAsync(competitionYear => competitionYear.Id == Input.CompetitionYearId.Value && competitionYear.IsActive);

            if (!competitionYearExists)
            {
                ModelState.AddModelError(nameof(Input.CompetitionYearId), "Selected competition year is not valid or inactive.");
            }
        }

        if (string.IsNullOrWhiteSpace(Input.Code))
        {
            ModelState.AddModelError(nameof(Input.Code), "Venue code is required.");
        }
        else if (Input.Code.Length > 50)
        {
            ModelState.AddModelError(nameof(Input.Code), "Venue code must not be more than 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(nameof(Input.Name), "Venue name is required.");
        }
        else if (Input.Name.Length > 200)
        {
            ModelState.AddModelError(nameof(Input.Name), "Venue name must not be more than 200 characters.");
        }

        if (Input.LocationText?.Length > 300)
        {
            ModelState.AddModelError(nameof(Input.LocationText), "Location must not be more than 300 characters.");
        }

        if (Input.Capacity.HasValue && Input.Capacity.Value < 0)
        {
            ModelState.AddModelError(nameof(Input.Capacity), "Capacity must be 0 or higher.");
        }

        if (Input.CompetitionYearId.HasValue && !string.IsNullOrWhiteSpace(Input.Code))
        {
            var codeExists = await _dbContext.Venues
                .AsNoTracking()
                .AnyAsync(venue =>
                    venue.Id != currentVenueId &&
                    venue.CompetitionYearId == Input.CompetitionYearId.Value &&
                    venue.Code == Input.Code);

            if (codeExists)
            {
                ModelState.AddModelError(nameof(Input.Code), "A venue with this code already exists for the selected competition year.");
            }
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears
            .AsNoTracking()
            .Where(competitionYear => competitionYear.IsActive)
            .OrderByDescending(competitionYear => competitionYear.YearNo)
            .Select(competitionYear => new SelectListItem
            {
                Value = competitionYear.Id.ToString(),
                Text = competitionYear.DisplayName
            })
            .ToListAsync();

        CompetitionYearOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition year..."
        });
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Venue update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreateVenueSnapshot(Venue venue)
    {
        return new
        {
            venue.Id,
            venue.CompetitionYearId,
            venue.Code,
            venue.Name,
            venue.LocationText,
            venue.Capacity,
            venue.Notes,
            venue.IsActive,
            venue.CreatedAt,
            venue.CreatedBy,
            venue.UpdatedAt,
            venue.UpdatedBy
        };
    }
}
