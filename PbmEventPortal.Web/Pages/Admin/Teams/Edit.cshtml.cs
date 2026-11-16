using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.Teams;

namespace PbmEventPortal.Web.Pages.Admin.Teams;

public class EditModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public EditModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public TeamEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> StateOptions { get; private set; } = [];

    public List<SelectListItem> RegionOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(team => team.Id == id);

        if (team == null)
        {
            return NotFound();
        }

        Input = MapToInput(team);

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
            ModelState.AddModelError(string.Empty, "Invalid team request.");
        }

        NormalizeInput();

        await ValidateTeamAsync(id);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var team = await _dbContext.Teams
            .FirstOrDefaultAsync(team => team.Id == id);

        if (team == null)
        {
            return NotFound();
        }

        var oldSnapshot = CreateTeamSnapshot(team);

        var now = DateTime.UtcNow;

        team.Code = Input.Code;
        team.NameEnglish = Input.NameEnglish;
        team.NameGurmukhi = Input.NameGurmukhi;
        team.StateId = Input.StateId!.Value;
        team.RegionId = Input.RegionId!.Value;
        team.IsActive = Input.IsActive;
        team.UpdatedAt = now;
        team.UpdatedBy = actorId.Value;

        var newSnapshot = CreateTeamSnapshot(team);

        await _auditLogService.LogAsync(
            entityName: "teams",
            entityId: team.Id,
            actionName: "UPDATE",
            changedBy: actorId.Value,
            changeReason: "System Admin updated team.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = "Team updated";
        ResultMessage = "The team has been updated successfully.";

        await LoadLookupOptionsAsync();

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static TeamEditInputDTO MapToInput(Team team)
    {
        return new TeamEditInputDTO
        {
            Id = team.Id,
            Code = team.Code,
            NameEnglish = team.NameEnglish,
            NameGurmukhi = team.NameGurmukhi,
            StateId = team.StateId,
            RegionId = team.RegionId,
            IsActive = team.IsActive
        };
    }

    private void NormalizeInput()
    {
        Input.Code = Input.Code.Trim().ToUpperInvariant().Replace(" ", "_");
        Input.NameEnglish = Input.NameEnglish.Trim();

        Input.NameGurmukhi = string.IsNullOrWhiteSpace(Input.NameGurmukhi)
            ? null
            : Input.NameGurmukhi.Trim();
    }

    private async Task ValidateTeamAsync(Guid currentTeamId)
    {
        if (string.IsNullOrWhiteSpace(Input.Code))
        {
            ModelState.AddModelError(nameof(Input.Code), "Team code is required.");
        }

        if (!string.IsNullOrWhiteSpace(Input.Code) &&
            Input.Code.Length > 50)
        {
            ModelState.AddModelError(nameof(Input.Code), "Team code must not be more than 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(Input.NameEnglish))
        {
            ModelState.AddModelError(nameof(Input.NameEnglish), "English name is required.");
        }

        if (!Input.StateId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.StateId), "State is required.");
        }

        if (!Input.RegionId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.RegionId), "Region is required.");
        }

        if (Input.StateId.HasValue)
        {
            var stateExists = await _dbContext.States
                .AsNoTracking()
                .AnyAsync(state =>
                    state.Id == Input.StateId.Value &&
                    state.IsActive);

            if (!stateExists)
            {
                ModelState.AddModelError(nameof(Input.StateId), "Selected state is not valid or inactive.");
            }
        }

        if (Input.RegionId.HasValue)
        {
            var regionExists = await _dbContext.Regions
                .AsNoTracking()
                .AnyAsync(region =>
                    region.Id == Input.RegionId.Value &&
                    region.IsActive);

            if (!regionExists)
            {
                ModelState.AddModelError(nameof(Input.RegionId), "Selected region is not valid or inactive.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Input.Code))
        {
            var codeExists = await _dbContext.Teams
                .AsNoTracking()
                .AnyAsync(team =>
                    team.Id != currentTeamId &&
                    team.Code == Input.Code);

            if (codeExists)
            {
                ModelState.AddModelError(nameof(Input.Code), "Team code is already used by another team.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Input.NameEnglish))
        {
            var nameExists = await _dbContext.Teams
                .AsNoTracking()
                .AnyAsync(team =>
                    team.Id != currentTeamId &&
                    team.NameEnglish.ToLower() == Input.NameEnglish.ToLower());

            if (nameExists)
            {
                ModelState.AddModelError(nameof(Input.NameEnglish), "English name is already used by another team.");
            }
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        StateOptions = await _dbContext.States
            .AsNoTracking()
            .Where(state => state.IsActive)
            .OrderBy(state => state.Name)
            .Select(state => new SelectListItem
            {
                Value = state.Id.ToString(),
                Text = state.Name
            })
            .ToListAsync();

        StateOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select state..."
        });

        RegionOptions = await _dbContext.Regions
            .AsNoTracking()
            .Where(region => region.IsActive)
            .OrderBy(region => region.Name)
            .Select(region => new SelectListItem
            {
                Value = region.Id.ToString(),
                Text = region.Name
            })
            .ToListAsync();

        RegionOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select region..."
        });
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Team update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreateTeamSnapshot(Team team)
    {
        return new
        {
            team.Id,
            team.Code,
            team.NameEnglish,
            team.NameGurmukhi,
            team.StateId,
            team.RegionId,
            team.IsActive,
            team.CreatedAt,
            team.CreatedBy,
            team.UpdatedAt,
            team.UpdatedBy
        };
    }
}