using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

namespace PbmEventPortal.Web.Pages.Admin.PointsSchemes;

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
    public PointsSchemeEditInputDTO Input { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var pointsScheme = await LoadPointsSchemeAsync(id, asNoTracking: true);

        if (pointsScheme == null)
        {
            return NotFound();
        }

        Input = MapToInput(pointsScheme);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        var pointsScheme = await LoadPointsSchemeAsync(id, asNoTracking: false);

        if (pointsScheme == null)
        {
            return NotFound();
        }

        PopulateReadOnlyInputFields(pointsScheme);
        NormalizeInput(pointsScheme);
        await ValidateInputAsync(pointsScheme);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var oldSchemeSnapshot = CreatePointsSchemeSnapshot(pointsScheme);
        var oldSchemeIsActive = pointsScheme.IsActive;
        var oldItemSnapshots = pointsScheme.PointsSchemeItems.ToDictionary(
            item => item.Id,
            item => CreatePointsSchemeItemSnapshot(item));

        var inputItemsById = Input.Items.ToDictionary(item => item.Id);
        var changedItems = pointsScheme.PointsSchemeItems
            .Where(item => inputItemsById.ContainsKey(item.Id) && item.PointsAwarded != inputItemsById[item.Id].PointsAwarded)
            .ToList();

        var schemeChanged = pointsScheme.SchemeName != Input.SchemeName ||
                            pointsScheme.IsActive != Input.IsActive;

        if (schemeChanged || changedItems.Count > 0)
        {
            var now = DateTime.UtcNow;

            if (schemeChanged)
            {
                pointsScheme.SchemeName = Input.SchemeName;
                pointsScheme.IsActive = Input.IsActive;
                pointsScheme.UpdatedAt = now;
                pointsScheme.UpdatedBy = actorId.Value;
            }

            foreach (var item in changedItems)
            {
                item.PointsAwarded = inputItemsById[item.Id].PointsAwarded!.Value;
                item.UpdatedAt = now;
                item.UpdatedBy = actorId.Value;
            }

            await _dbContext.SaveChangesAsync();

            if (schemeChanged)
            {
                var actionName = oldSchemeIsActive != pointsScheme.IsActive
                    ? pointsScheme.IsActive ? "ACTIVATE" : "DEACTIVATE"
                    : "UPDATE";

                var changeReason = actionName switch
                {
                    "ACTIVATE" => "System Admin reactivated points scheme.",
                    "DEACTIVATE" => "System Admin deactivated points scheme.",
                    _ => "System Admin updated points scheme."
                };

                await _auditLogService.LogAsync(
                    entityName: "points_schemes",
                    entityId: pointsScheme.Id,
                    actionName: actionName,
                    changedBy: actorId.Value,
                    changeReason: changeReason,
                    oldValues: oldSchemeSnapshot,
                    newValues: CreatePointsSchemeSnapshot(pointsScheme));
            }

            foreach (var item in changedItems)
            {
                await _auditLogService.LogAsync(
                    entityName: "points_scheme_items",
                    entityId: item.Id,
                    actionName: "UPDATE",
                    changedBy: actorId.Value,
                    changeReason: "System Admin updated points scheme item.",
                    oldValues: oldItemSnapshots[item.Id],
                    newValues: CreatePointsSchemeItemSnapshot(item));
            }
        }

        var refreshedPointsScheme = await LoadPointsSchemeAsync(pointsScheme.Id, asNoTracking: true);
        Input = MapToInput(refreshedPointsScheme!);

        ResultIsSuccess = true;
        ResultTitle = schemeChanged || changedItems.Count > 0
            ? "Points scheme updated"
            : "No changes detected";
        ResultMessage = schemeChanged || changedItems.Count > 0
            ? "The points scheme has been updated successfully."
            : "No points scheme changes were detected.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private async Task<PointsScheme?> LoadPointsSchemeAsync(Guid id, bool asNoTracking)
    {
        var query = _dbContext.PointsSchemes
            .Include(pointsScheme => pointsScheme.PointsSchemeItems)
            .Where(pointsScheme => pointsScheme.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private static PointsSchemeEditInputDTO MapToInput(PointsScheme pointsScheme)
    {
        return new PointsSchemeEditInputDTO
        {
            Id = pointsScheme.Id,
            SchemeName = pointsScheme.SchemeName,
            WinnerCount = pointsScheme.WinnerCount,
            IsActive = pointsScheme.IsActive,
            Items = pointsScheme.PointsSchemeItems
                .OrderBy(item => item.PlacingNo)
                .Select(item => new PointsSchemeEditItemInputViewModel
                {
                    Id = item.Id,
                    PlacingNo = item.PlacingNo,
                    PointsAwarded = item.PointsAwarded
                })
                .ToList()
        };
    }

    private void PopulateReadOnlyInputFields(PointsScheme pointsScheme)
    {
        Input.Id = pointsScheme.Id;
        Input.WinnerCount = pointsScheme.WinnerCount;
    }

    private void NormalizeInput(PointsScheme pointsScheme)
    {
        var savedItemsById = pointsScheme.PointsSchemeItems.ToDictionary(item => item.Id);

        Input.SchemeName = string.IsNullOrWhiteSpace(Input.SchemeName)
            ? string.Empty
            : Input.SchemeName.Trim();

        Input.Items = Input.Items
            .Where(item => savedItemsById.ContainsKey(item.Id))
            .Select(item => new PointsSchemeEditItemInputViewModel
            {
                Id = item.Id,
                PlacingNo = savedItemsById[item.Id].PlacingNo,
                PointsAwarded = item.PointsAwarded
            })
            .OrderBy(item => item.PlacingNo)
            .ToList();
    }

    private async Task ValidateInputAsync(PointsScheme pointsScheme)
    {
        if (string.IsNullOrWhiteSpace(Input.SchemeName))
        {
            ModelState.AddModelError(nameof(Input.SchemeName), "Scheme name is required.");
        }
        else if (Input.SchemeName.Length > 100)
        {
            ModelState.AddModelError(nameof(Input.SchemeName), "Scheme name must not be more than 100 characters.");
        }
        else
        {
            var schemeNameExists = await _dbContext.PointsSchemes
                .AsNoTracking()
                .AnyAsync(scheme => scheme.Id != pointsScheme.Id && scheme.SchemeName == Input.SchemeName);

            if (schemeNameExists)
            {
                ModelState.AddModelError(nameof(Input.SchemeName), "A points scheme with this name already exists.");
            }
        }

        if (Input.Items.Count != pointsScheme.PointsSchemeItems.Count)
        {
            ModelState.AddModelError(nameof(Input.Items), "The submitted points scheme item set does not match the saved item set.");
            return;
        }

        var savedItemIds = pointsScheme.PointsSchemeItems.Select(item => item.Id).ToHashSet();
        var submittedItemIds = new HashSet<Guid>();

        for (var index = 0; index < Input.Items.Count; index++)
        {
            var item = Input.Items[index];
            var rowLabel = $"Placing row {index + 1}";

            if (!savedItemIds.Contains(item.Id))
            {
                ModelState.AddModelError($"Input.Items[{index}].Id", $"{rowLabel}: Invalid points scheme item row.");
            }
            else if (!submittedItemIds.Add(item.Id))
            {
                ModelState.AddModelError($"Input.Items[{index}].Id", $"{rowLabel}: This points scheme item row was submitted more than once.");
            }

            if (!item.PointsAwarded.HasValue)
            {
                ModelState.AddModelError($"Input.Items[{index}].PointsAwarded", $"{rowLabel}: Points awarded is required.");
            }
            else if (item.PointsAwarded.Value < 0)
            {
                ModelState.AddModelError($"Input.Items[{index}].PointsAwarded", $"{rowLabel}: Points awarded must be 0 or higher.");
            }
        }
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Points scheme update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static object CreatePointsSchemeSnapshot(PointsScheme pointsScheme)
    {
        return new
        {
            pointsScheme.Id,
            pointsScheme.SchemeName,
            pointsScheme.WinnerCount,
            pointsScheme.IsActive,
            pointsScheme.CreatedAt,
            pointsScheme.CreatedBy,
            pointsScheme.UpdatedAt,
            pointsScheme.UpdatedBy
        };
    }

    private static object CreatePointsSchemeItemSnapshot(PointsSchemeItem item)
    {
        return new
        {
            item.Id,
            item.PointsSchemeId,
            item.PlacingNo,
            item.PointsAwarded,
            item.CreatedAt,
            item.CreatedBy,
            item.UpdatedAt,
            item.UpdatedBy
        };
    }
}
