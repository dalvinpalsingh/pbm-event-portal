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

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public TeamCreateInputDTO Input { get; set; } = new();

    public List<SelectListItem> StateOptions { get; private set; } = [];

    public List<SelectListItem> RegionOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedTeamId { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadLookupOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        NormalizeInput();

        await ValidateTeamAsync();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            await LoadLookupOptionsAsync();
            return Page();
        }

        var now = DateTime.UtcNow;

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Code = Input.Code,
            NameEnglish = Input.NameEnglish,
            NameGurmukhi = Input.NameGurmukhi,
            StateId = Input.StateId!.Value,
            RegionId = Input.RegionId!.Value,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.Teams.Add(team);

        await _auditLogService.LogAsync(
            entityName: "teams",
            entityId: team.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created team.",
            oldValues: null,
            newValues: CreateTeamSnapshot(team));

        CreatedTeamId = team.Id;
        ResultIsSuccess = true;
        ResultTitle = "Team created";
        ResultMessage = "The team has been created successfully.";

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

    private void NormalizeInput()
    {
        Input.Code = Input.Code.Trim().ToUpperInvariant().Replace(" ", "_");
        Input.NameEnglish = Input.NameEnglish.Trim();

        Input.NameGurmukhi = string.IsNullOrWhiteSpace(Input.NameGurmukhi)
            ? null
            : Input.NameGurmukhi.Trim();
    }

    private async Task ValidateTeamAsync()
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
                .AnyAsync(team => team.Code == Input.Code);

            if (codeExists)
            {
                ModelState.AddModelError(nameof(Input.Code), "Team code is already used by another team.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Input.NameEnglish))
        {
            var nameExists = await _dbContext.Teams
                .AsNoTracking()
                .AnyAsync(team => team.NameEnglish.ToLower() == Input.NameEnglish.ToLower());

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
        ResultTitle = "Team creation failed";
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