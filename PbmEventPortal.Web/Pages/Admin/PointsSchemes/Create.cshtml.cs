using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.PointsSchemes;

namespace PbmEventPortal.Web.Pages.Admin.PointsSchemes;

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;

    public CreateModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _configuration = configuration;
    }

    [BindProperty]
    public PointsSchemeCreateInputDTO Input { get; set; } = new();

    public List<SelectListItem> WinnerCountOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedPointsSchemeId { get; private set; }

    public short MinWinnerCount => GetConfiguredShort("PointsSchemeRules:MinWinnerCount", 1);

    public short MaxWinnerCount => GetConfiguredShort("PointsSchemeRules:MaxWinnerCount", 100);

    public void OnGet()
    {
        Input = CreateDefaultInput(MinWinnerCount);
        LoadLookupOptions();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        NormalizeInput();
        await ValidatePointsSchemeAsync();
        LoadLookupOptions();

        if (!ModelState.IsValid)
        {
            ResultIsSuccess = false;
            ResultTitle = "Points scheme creation failed";
            ResultMessage = "Please review the following and try again.";
            return Page();
        }

        var now = DateTime.UtcNow;
        var pointsScheme = new PointsScheme
        {
            Id = Guid.NewGuid(),
            SchemeName = Input.SchemeName,
            WinnerCount = Input.WinnerCount!.Value,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        var pointsSchemeItems = Input.Items
            .OrderBy(item => item.PlacingNo!.Value)
            .Select(item => new PointsSchemeItem
            {
                Id = Guid.NewGuid(),
                PointsSchemeId = pointsScheme.Id,
                PlacingNo = item.PlacingNo!.Value,
                PointsAwarded = item.PointsAwarded!.Value,
                CreatedAt = now,
                CreatedBy = actorId.Value,
                UpdatedAt = null,
                UpdatedBy = null
            })
            .ToList();

        pointsScheme.PointsSchemeItems = pointsSchemeItems;
        _dbContext.PointsSchemes.Add(pointsScheme);

        await _auditLogService.LogAsync(
            entityName: "points_schemes",
            entityId: pointsScheme.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created points scheme.",
            oldValues: null,
            newValues: CreatePointsSchemeSnapshot(pointsScheme));

        foreach (var item in pointsSchemeItems)
        {
            await _auditLogService.LogAsync(
                entityName: "points_scheme_items",
                entityId: item.Id,
                actionName: "CREATE",
                changedBy: actorId.Value,
                changeReason: "System Admin created points scheme item.",
                oldValues: null,
                newValues: CreatePointsSchemeItemSnapshot(item));
        }

        CreatedPointsSchemeId = pointsScheme.Id;
        ResultIsSuccess = true;
        ResultTitle = "Points scheme created";
        ResultMessage = $"{pointsScheme.SchemeName} has been created successfully.";
        Input = CreateDefaultInput(MinWinnerCount);

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
        Input.SchemeName = string.IsNullOrWhiteSpace(Input.SchemeName)
            ? string.Empty
            : Input.SchemeName.Trim();

        Input.Items = Input.Items
            .Where(item => item.PlacingNo.HasValue || item.PointsAwarded.HasValue)
            .Select(item => new PointsSchemeCreateItemInputViewModel
            {
                PlacingNo = item.PlacingNo,
                PointsAwarded = item.PointsAwarded
            })
            .ToList();
    }

    private async Task ValidatePointsSchemeAsync()
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
                .AnyAsync(scheme => scheme.SchemeName == Input.SchemeName);

            if (schemeNameExists)
            {
                ModelState.AddModelError(nameof(Input.SchemeName), "A points scheme with this name already exists.");
            }
        }

        if (!Input.WinnerCount.HasValue)
        {
            ModelState.AddModelError(nameof(Input.WinnerCount), "Winner count is required.");
        }
        else if (Input.WinnerCount.Value < MinWinnerCount || Input.WinnerCount.Value > MaxWinnerCount)
        {
            ModelState.AddModelError(nameof(Input.WinnerCount), $"Winner count must be between {MinWinnerCount} and {MaxWinnerCount}.");
        }

        ValidateItems();
    }

    private void ValidateItems()
    {
        if (!Input.WinnerCount.HasValue)
        {
            return;
        }

        if (Input.Items.Count != Input.WinnerCount.Value)
        {
            ModelState.AddModelError(nameof(Input.Items), $"Exactly {Input.WinnerCount.Value} placing point rows are required.");
        }

        var submittedPlacings = new HashSet<short>();

        for (var index = 0; index < Input.Items.Count; index++)
        {
            var item = Input.Items[index];
            var rowLabel = $"Placing row {index + 1}";

            if (!item.PlacingNo.HasValue)
            {
                ModelState.AddModelError($"Input.Items[{index}].PlacingNo", $"{rowLabel}: Placing is required.");
            }
            else if (item.PlacingNo.Value < 1 || item.PlacingNo.Value > Input.WinnerCount.Value)
            {
                ModelState.AddModelError($"Input.Items[{index}].PlacingNo", $"{rowLabel}: Placing must be between 1 and {Input.WinnerCount.Value}.");
            }
            else if (!submittedPlacings.Add(item.PlacingNo.Value))
            {
                ModelState.AddModelError($"Input.Items[{index}].PlacingNo", $"{rowLabel}: Placing {item.PlacingNo.Value} is duplicated.");
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

    private void LoadLookupOptions()
    {
        WinnerCountOptions = Enumerable.Range(MinWinnerCount, MaxWinnerCount - MinWinnerCount + 1)
            .Select(winnerCount => new SelectListItem
            {
                Value = winnerCount.ToString(),
                Text = winnerCount.ToString()
            })
            .ToList();
    }

    private short GetConfiguredShort(string key, short fallbackValue)
    {
        var configuredValue = _configuration.GetValue<short?>(key);

        return configuredValue is > 0
            ? configuredValue.Value
            : fallbackValue;
    }

    private static PointsSchemeCreateInputDTO CreateDefaultInput(short winnerCount)
    {
        return new PointsSchemeCreateInputDTO
        {
            WinnerCount = winnerCount,
            IsActive = true,
            Items = Enumerable.Range(1, winnerCount)
                .Select(placingNo => new PointsSchemeCreateItemInputViewModel
                {
                    PlacingNo = (short)placingNo,
                    PointsAwarded = (short)(winnerCount - placingNo + 1)
                })
                .ToList()
        };
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
