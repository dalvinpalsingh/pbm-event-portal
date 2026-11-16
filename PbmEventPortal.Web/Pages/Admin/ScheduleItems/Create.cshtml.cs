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

public class CreateModel : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> ScheduleTypeOptionsSource =
    [
        new SelectListItem { Value = ScheduleEnums.ScheduleTypeRegistration, Text = "Registration" },
        new SelectListItem { Value = ScheduleEnums.ScheduleTypeMeal, Text = "Meal" },
        new SelectListItem { Value = ScheduleEnums.ScheduleTypeCeremony, Text = "Ceremony" },
        new SelectListItem { Value = ScheduleEnums.ScheduleTypeCompetition, Text = "Competition" },
        new SelectListItem { Value = ScheduleEnums.ScheduleTypePrizeGiving, Text = "Prize Giving" },
        new SelectListItem { Value = ScheduleEnums.ScheduleTypeGeneral, Text = "General" },
        new SelectListItem { Value = ScheduleEnums.ScheduleTypeBuffer, Text = "Buffer" }
    ];

    private static readonly IReadOnlyList<SelectListItem> StatusOptionsSource =
    [
        new SelectListItem { Value = ScheduleEnums.StatusDraft, Text = "Draft" },
        new SelectListItem { Value = ScheduleEnums.StatusPublished, Text = "Published" },
        new SelectListItem { Value = ScheduleEnums.StatusOngoing, Text = "Ongoing" },
        new SelectListItem { Value = ScheduleEnums.StatusCompleted, Text = "Completed" },
        new SelectListItem { Value = ScheduleEnums.StatusDelayed, Text = "Delayed" },
        new SelectListItem { Value = ScheduleEnums.StatusCancelled, Text = "Cancelled" },
        new SelectListItem { Value = ScheduleEnums.StatusMoved, Text = "Moved" }
    ];

    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public ScheduleItemCreateInputDTO Input { get; set; } = CreateDefaultInput();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];
    public List<SelectListItem> CompetitionCategoryOptions { get; private set; } = [];
    public List<SelectListItem> VenueOptions { get; private set; } = [];
    public List<SelectListItem> ScheduleTypeOptions { get; private set; } = [];
    public List<SelectListItem> StatusOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }
    public Guid? CreatedScheduleItemId { get; private set; }

    public async Task OnGetAsync()
    {
        Input = CreateDefaultInput();
        await LoadLookupOptionsAsync(Input.CompetitionYearId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();
        if (actorId == null)
        {
            return Challenge();
        }

        NormalizeInput();
        await ValidateInputAsync();
        await LoadLookupOptionsAsync(Input.CompetitionYearId);

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var now = DateTime.UtcNow;
        var scheduleItem = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = Input.CompetitionYearId!.Value,
            ScheduleDate = Input.ScheduleDate!.Value,
            StartTime = Input.StartTime!.Value,
            EndTime = Input.EndTime!.Value,
            DisplayTitle = Input.DisplayTitle,
            ScheduleType = Input.ScheduleType,
            CompetitionCategoryId = Input.CompetitionCategoryId,
            VenueId = Input.VenueId,
            SessionGroup = Input.SessionGroup,
            SequenceNo = Input.SequenceNo,
            ReportingTime = Input.ReportingTime,
            Status = Input.Status,
            Notes = Input.Notes,
            IsPublished = Input.IsPublished,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.ScheduleItems.Add(scheduleItem);

        await _auditLogService.LogAsync(
            entityName: "schedule_items",
            entityId: scheduleItem.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created schedule item.",
            oldValues: null,
            newValues: CreateScheduleItemSnapshot(scheduleItem));

        CreatedScheduleItemId = scheduleItem.Id;
        ResultIsSuccess = true;
        ResultTitle = "Schedule item created";
        ResultMessage = $"{scheduleItem.DisplayTitle} has been created successfully.";
        Input = CreateDefaultInput();
        await LoadLookupOptionsAsync(Input.CompetitionYearId);

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private void NormalizeInput()
    {
        Input.DisplayTitle = string.IsNullOrWhiteSpace(Input.DisplayTitle) ? string.Empty : Input.DisplayTitle.Trim();
        Input.ScheduleType = string.IsNullOrWhiteSpace(Input.ScheduleType) ? string.Empty : Input.ScheduleType.Trim().ToUpperInvariant();
        Input.Status = string.IsNullOrWhiteSpace(Input.Status) ? string.Empty : Input.Status.Trim().ToUpperInvariant();
        Input.SessionGroup = string.IsNullOrWhiteSpace(Input.SessionGroup) ? null : Input.SessionGroup.Trim();
        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();

        if (Input.Status == "PUBLISHED")
        {
            Input.IsPublished = true;
        }
    }

    private async Task ValidateInputAsync()
    {
        if (!Input.CompetitionYearId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        }
        else
        {
            var yearExists = await _dbContext.CompetitionYears.AsNoTracking()
                .AnyAsync(year => year.Id == Input.CompetitionYearId.Value && year.IsActive);
            if (!yearExists)
            {
                ModelState.AddModelError(nameof(Input.CompetitionYearId), "Selected competition year is not valid or inactive.");
            }
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

        if (!ScheduleTypeOptionsSource.Any(option => option.Value == Input.ScheduleType))
        {
            ModelState.AddModelError(nameof(Input.ScheduleType), "Selected schedule type is not valid.");
        }

        if (!StatusOptionsSource.Any(option => option.Value == Input.Status))
        {
            ModelState.AddModelError(nameof(Input.Status), "Selected status is not valid.");
        }

        ValidatePublishStatus();

        if (Input.SessionGroup?.Length > 100) ModelState.AddModelError(nameof(Input.SessionGroup), "Session group must not be more than 100 characters.");
        if (Input.SequenceNo.HasValue && Input.SequenceNo.Value < 1) ModelState.AddModelError(nameof(Input.SequenceNo), "Sequence number must be 1 or higher.");

        if (Input.CompetitionYearId.HasValue && Input.CompetitionCategoryId.HasValue)
        {
            var categoryValid = await _dbContext.CompetitionCategories.AsNoTracking()
                .AnyAsync(category => category.Id == Input.CompetitionCategoryId.Value && category.Competition.CompetitionYearId == Input.CompetitionYearId.Value);
            if (!categoryValid)
            {
                ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category does not belong to the selected competition year.");
            }
        }

        if (Input.CompetitionYearId.HasValue && Input.VenueId.HasValue)
        {
            var venueValid = await _dbContext.Venues.AsNoTracking()
                .AnyAsync(venue => venue.Id == Input.VenueId.Value && venue.CompetitionYearId == Input.CompetitionYearId.Value && venue.IsActive);
            if (!venueValid)
            {
                ModelState.AddModelError(nameof(Input.VenueId), "Selected venue does not belong to the selected competition year or is inactive.");
            }
        }
    }

    private void ValidatePublishStatus()
    {
        if (Input.Status == ScheduleEnums.StatusDraft && Input.IsPublished)
        {
            ModelState.AddModelError(nameof(Input.IsPublished), "Draft schedule items cannot be marked as published.");
            return;
        }

        if (Input.IsPublished && Input.Status != "PUBLISHED")
        {
            ModelState.AddModelError(nameof(Input.IsPublished), "Only schedule items with Published status can be marked as published.");
        }
    }

    private async Task LoadLookupOptionsAsync(Guid? selectedCompetitionYearId)
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(year => year.IsActive)
            .OrderByDescending(year => year.YearNo)
            .Select(year => new SelectListItem { Value = year.Id.ToString(), Text = year.DisplayName })
            .ToListAsync();
        CompetitionYearOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select competition year..." });

        var categoryQuery = _dbContext.CompetitionCategories.AsNoTracking().Where(category => category.IsActive);
        var venueQuery = _dbContext.Venues.AsNoTracking().Where(venue => venue.IsActive);

        if (selectedCompetitionYearId.HasValue)
        {
            categoryQuery = categoryQuery.Where(category => category.Competition.CompetitionYearId == selectedCompetitionYearId.Value);
            venueQuery = venueQuery.Where(venue => venue.CompetitionYearId == selectedCompetitionYearId.Value);
        }

        CompetitionCategoryOptions = await categoryQuery
            .OrderBy(category => category.Competition.Code)
            .ThenBy(category => category.SortOrder)
            .Select(category => new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.Competition.Code + " / " + category.CategoryCode + " - " + category.Competition.NameEnglish
            })
            .ToListAsync();
        CompetitionCategoryOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Not linked" });

        VenueOptions = await venueQuery
            .OrderBy(venue => venue.Code)
            .Select(venue => new SelectListItem { Value = venue.Id.ToString(), Text = venue.Code + " - " + venue.Name })
            .ToListAsync();
        VenueOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Not assigned" });

        ScheduleTypeOptions = ScheduleTypeOptionsSource.Select(CloneSelectListItem).ToList();
        StatusOptions = StatusOptionsSource.Select(CloneSelectListItem).ToList();
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Schedule item creation failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static ScheduleItemCreateInputDTO CreateDefaultInput()
    {
        return new ScheduleItemCreateInputDTO
        {
            ScheduleDate = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            ScheduleType = ScheduleEnums.ScheduleTypeGeneral,
            Status = ScheduleEnums.StatusDraft,
            IsPublished = false
        };
    }

    private static SelectListItem CloneSelectListItem(SelectListItem item) => new() { Value = item.Value, Text = item.Text };

    private static object CreateScheduleItemSnapshot(ScheduleItem scheduleItem)
    {
        return new
        {
            scheduleItem.Id,
            scheduleItem.CompetitionYearId,
            scheduleItem.ScheduleDate,
            scheduleItem.StartTime,
            scheduleItem.EndTime,
            scheduleItem.DisplayTitle,
            scheduleItem.ScheduleType,
            scheduleItem.CompetitionCategoryId,
            scheduleItem.VenueId,
            scheduleItem.SessionGroup,
            scheduleItem.SequenceNo,
            scheduleItem.ReportingTime,
            scheduleItem.Status,
            scheduleItem.Notes,
            scheduleItem.IsPublished,
            scheduleItem.CreatedAt,
            scheduleItem.CreatedBy,
            scheduleItem.UpdatedAt,
            scheduleItem.UpdatedBy
        };
    }
}
