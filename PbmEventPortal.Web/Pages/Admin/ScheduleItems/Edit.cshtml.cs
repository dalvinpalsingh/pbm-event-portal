using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.ScheduleItems;

namespace PbmEventPortal.Web.Pages.Admin.ScheduleItems;

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
    public ScheduleItemEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];
    public List<SelectListItem> CompetitionCategoryOptions { get; private set; } = [];
    public List<SelectListItem> VenueOptions { get; private set; } = [];
    public List<SelectListItem> ScheduleTypeOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var item = await _dbContext.ScheduleItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        Input = MapToInput(item);
        await LoadLookupOptionsAsync(Input.CompetitionYearId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        if (Input.Id != id) ModelState.AddModelError(string.Empty, "Invalid schedule item request.");

        NormalizeInput();
        await ValidateInputAsync();
        await LoadLookupOptionsAsync(Input.CompetitionYearId);

        if (!ModelState.IsValid)
        {
            ResultIsSuccess = false;
            ResultTitle = "Schedule item update failed";
            ResultMessage = "Please review the following and try again.";
            return Page();
        }

        var item = await _dbContext.ScheduleItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();

        var oldSnapshot = Snapshot(item);
        var now = DateTime.UtcNow;

        item.CompetitionYearId = Input.CompetitionYearId!.Value;
        item.ScheduleDate = Input.ScheduleDate!.Value;
        item.StartTime = Input.StartTime!.Value;
        item.EndTime = Input.EndTime!.Value;
        item.ReportingTime = Input.ReportingTime;
        item.DisplayTitle = Input.DisplayTitle;
        item.ScheduleType = Input.ScheduleType;
        item.CompetitionCategoryId = Input.CompetitionCategoryId;
        item.VenueId = Input.VenueId;
        item.SessionGroup = Input.SessionGroup;
        item.SequenceNo = Input.SequenceNo;
        item.Status = Input.Status;
        item.IsPublished = Input.IsPublished;
        item.Notes = Input.Notes;
        item.UpdatedAt = now;
        item.UpdatedBy = actorId.Value;

        await _auditLogService.LogAsync(
            entityName: "schedule_items",
            entityId: item.Id,
            actionName: "UPDATE",
            changedBy: actorId.Value,
            changeReason: "System Admin updated schedule item.",
            oldValues: oldSnapshot,
            newValues: Snapshot(item));

        ResultIsSuccess = true;
        ResultTitle = "Schedule item updated";
        ResultMessage = "The schedule item has been updated successfully.";
        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static ScheduleItemEditInputDTO MapToInput(ScheduleItem item) => new()
    {
        Id = item.Id,
        CompetitionYearId = item.CompetitionYearId,
        ScheduleDate = item.ScheduleDate,
        StartTime = item.StartTime,
        EndTime = item.EndTime,
        ReportingTime = item.ReportingTime,
        DisplayTitle = item.DisplayTitle,
        ScheduleType = item.ScheduleType,
        CompetitionCategoryId = item.CompetitionCategoryId,
        VenueId = item.VenueId,
        SessionGroup = item.SessionGroup,
        SequenceNo = item.SequenceNo,
        Status = item.Status,
        IsPublished = item.IsPublished,
        Notes = item.Notes
    };

    private void NormalizeInput()
    {
        Input.DisplayTitle = string.IsNullOrWhiteSpace(Input.DisplayTitle) ? string.Empty : Input.DisplayTitle.Trim();
        Input.ScheduleType = string.IsNullOrWhiteSpace(Input.ScheduleType) ? string.Empty : Input.ScheduleType.Trim().ToUpperInvariant();
        Input.Status = string.IsNullOrWhiteSpace(Input.Status) ? string.Empty : Input.Status.Trim().ToUpperInvariant();
        Input.SessionGroup = string.IsNullOrWhiteSpace(Input.SessionGroup) ? null : Input.SessionGroup.Trim();
        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();

        if (Input.Status == "PUBLISHED") Input.IsPublished = true;
    }

    private async Task ValidateInputAsync()
    {
        if (!Input.CompetitionYearId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        }
        else if (!await _dbContext.CompetitionYears.AsNoTracking().AnyAsync(x => x.Id == Input.CompetitionYearId.Value && x.IsActive))
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Selected competition year is not valid or inactive.");
        }

        if (!Input.ScheduleDate.HasValue) ModelState.AddModelError(nameof(Input.ScheduleDate), "Schedule date is required.");
        if (!Input.StartTime.HasValue) ModelState.AddModelError(nameof(Input.StartTime), "Start time is required.");
        if (!Input.EndTime.HasValue) ModelState.AddModelError(nameof(Input.EndTime), "End time is required.");
        if (Input.StartTime.HasValue && Input.EndTime.HasValue && Input.EndTime.Value <= Input.StartTime.Value)
        {
            ModelState.AddModelError(nameof(Input.EndTime), "End time must be after start time.");
        }

        if (string.IsNullOrWhiteSpace(Input.DisplayTitle)) ModelState.AddModelError(nameof(Input.DisplayTitle), "Display title is required.");
        else if (Input.DisplayTitle.Length > 200) ModelState.AddModelError(nameof(Input.DisplayTitle), "Display title must not be more than 200 characters.");

        if (!ScheduleEnums.ScheduleTypes.Contains(Input.ScheduleType)) ModelState.AddModelError(nameof(Input.ScheduleType), "Selected schedule type is not valid.");
        if (!ScheduleEnums.Statuses.Contains(Input.Status)) ModelState.AddModelError(nameof(Input.Status), "Selected status is not valid.");

        if (Input.Status == ScheduleEnums.StatusDraft && Input.IsPublished)
        {
            ModelState.AddModelError(nameof(Input.IsPublished), "Draft schedule items cannot be marked as published.");
        }
        else if (Input.IsPublished && Input.Status != "PUBLISHED")
        {
            ModelState.AddModelError(nameof(Input.IsPublished), "Only schedule items with Published status can be marked as published.");
        }

        if (Input.SessionGroup?.Length > 100) ModelState.AddModelError(nameof(Input.SessionGroup), "Session group must not be more than 100 characters.");
        if (Input.SequenceNo.HasValue && Input.SequenceNo.Value < 1) ModelState.AddModelError(nameof(Input.SequenceNo), "Sequence number must be 1 or higher.");

        if (Input.CompetitionYearId.HasValue && Input.CompetitionCategoryId.HasValue)
        {
            var categoryValid = await _dbContext.CompetitionCategories.AsNoTracking()
                .AnyAsync(x => x.Id == Input.CompetitionCategoryId.Value && x.Competition.CompetitionYearId == Input.CompetitionYearId.Value);
            if (!categoryValid) ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category does not belong to the selected competition year.");
        }

        if (Input.CompetitionYearId.HasValue && Input.VenueId.HasValue)
        {
            var venueValid = await _dbContext.Venues.AsNoTracking()
                .AnyAsync(x => x.Id == Input.VenueId.Value && x.CompetitionYearId == Input.CompetitionYearId.Value && x.IsActive);
            if (!venueValid) ModelState.AddModelError(nameof(Input.VenueId), "Selected venue does not belong to the selected competition year or is inactive.");
        }
    }

    private async Task LoadLookupOptionsAsync(Guid? yearId)
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.YearNo)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.DisplayName })
            .ToListAsync();
        CompetitionYearOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select competition year..." });

        var categories = _dbContext.CompetitionCategories.AsNoTracking().Where(x => x.IsActive);
        var venues = _dbContext.Venues.AsNoTracking().Where(x => x.IsActive);

        if (yearId.HasValue)
        {
            categories = categories.Where(x => x.Competition.CompetitionYearId == yearId.Value);
            venues = venues.Where(x => x.CompetitionYearId == yearId.Value);
        }

        CompetitionCategoryOptions = await categories
            .OrderBy(x => x.Competition.Code)
            .ThenBy(x => x.SortOrder)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Competition.Code + " / " + x.CategoryCode + " - " + x.Competition.NameEnglish })
            .ToListAsync();
        CompetitionCategoryOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Not linked" });

        VenueOptions = await venues
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Code + " - " + x.Name })
            .ToListAsync();
        VenueOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Not assigned" });

        ScheduleTypeOptions = ScheduleEnums.ScheduleTypes.Select(x => new SelectListItem { Value = x, Text = x.Replace("_", " ") }).ToList();
        StatusOptions = ScheduleEnums.Statuses.Select(x => new SelectListItem { Value = x, Text = x.Replace("_", " ") }).ToList();
    }

    private static object Snapshot(ScheduleItem item) => new
    {
        item.Id,
        item.CompetitionYearId,
        item.ScheduleDate,
        item.StartTime,
        item.EndTime,
        item.DisplayTitle,
        item.ScheduleType,
        item.CompetitionCategoryId,
        item.VenueId,
        item.SessionGroup,
        item.SequenceNo,
        item.ReportingTime,
        item.Status,
        item.Notes,
        item.IsPublished,
        item.CreatedAt,
        item.CreatedBy,
        item.UpdatedAt,
        item.UpdatedBy
    };
}
