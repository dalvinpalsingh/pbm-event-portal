using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.JudgeAssignments;

namespace PbmEventPortal.Web.Pages.Admin.JudgeAssignments;

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public JudgeAssignmentCreateInputDTO Input { get; set; } = new();

    public List<SelectListItem> CompetitionCategoryOptions { get; private set; } = [];
    public List<SelectListItem> JudgeOptions { get; private set; } = [];
    public List<SelectListItem> CriterionOptions { get; private set; } = [];
    public List<SelectListItem> ScheduleItemOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }
    public Guid? CreatedJudgeAssignmentId { get; private set; }

    public async Task OnGetAsync()
    {
        Input = new JudgeAssignmentCreateInputDTO { IsActive = true };
        await LoadLookupOptionsAsync(Input.CompetitionCategoryId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();
        await ValidateInputAsync();
        await LoadLookupOptionsAsync(Input.CompetitionCategoryId);

        if (!ModelState.IsValid)
        {
            ResultIsSuccess = false;
            ResultTitle = "Judge assignment creation failed";
            ResultMessage = "Please review the following and try again.";
            return Page();
        }

        var assignment = new JudgeAssignment
        {
            Id = Guid.NewGuid(),
            CompetitionCategoryId = Input.CompetitionCategoryId!.Value,
            ScheduleItemId = Input.ScheduleItemId,
            JudgeUserId = Input.JudgeUserId!.Value,
            CriterionId = Input.CriterionId,
            IsActive = Input.IsActive,
            Notes = Input.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorId.Value
        };

        _dbContext.JudgeAssignments.Add(assignment);
        await _auditLogService.LogAsync("judge_assignments", assignment.Id, "CREATE", actorId.Value,
            "System Admin created judge assignment.", null, Snapshot(assignment));

        CreatedJudgeAssignmentId = assignment.Id;
        ResultIsSuccess = true;
        ResultTitle = "Judge assignment created";
        ResultMessage = "The judge assignment has been created successfully.";
        Input = new JudgeAssignmentCreateInputDTO { IsActive = true };
        await LoadLookupOptionsAsync(Input.CompetitionCategoryId);
        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task ValidateInputAsync()
    {
        if (!Input.CompetitionCategoryId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Competition category is required.");
        }
        else if (!await _dbContext.CompetitionCategories.AsNoTracking().AnyAsync(x => x.Id == Input.CompetitionCategoryId.Value && x.IsActive && x.Competition.IsActive && x.Competition.CompetitionYear.IsActive))
        {
            ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category is not valid or inactive.");
        }

        if (!Input.JudgeUserId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.JudgeUserId), "Judge is required.");
        }
        else if (!await _dbContext.UserProfiles.AsNoTracking().AnyAsync(x => x.Id == Input.JudgeUserId.Value && x.IsActive && x.UserRoleAssignmentUsers.Any(r => r.RoleCode == RoleEnums.Judge && r.IsActive && r.IsVerified)))
        {
            ModelState.AddModelError(nameof(Input.JudgeUserId), "Selected user must have an active and verified Judge role.");
        }

        if (Input.CompetitionCategoryId.HasValue && Input.CriterionId.HasValue && !await _dbContext.CompetitionScoringCriteria.AsNoTracking().AnyAsync(x => x.Id == Input.CriterionId.Value && x.IsActive && x.Competition.CompetitionCategories.Any(c => c.Id == Input.CompetitionCategoryId.Value)))
        {
            ModelState.AddModelError(nameof(Input.CriterionId), "Selected scoring criterion does not belong to the selected competition category.");
        }

        if (Input.CompetitionCategoryId.HasValue && Input.ScheduleItemId.HasValue && !await _dbContext.ScheduleItems.AsNoTracking().AnyAsync(x => x.Id == Input.ScheduleItemId.Value && x.ScheduleType == "COMPETITION" && x.CompetitionCategoryId == Input.CompetitionCategoryId.Value))
        {
            ModelState.AddModelError(nameof(Input.ScheduleItemId), "Selected schedule item must be a competition session for the selected category.");
        }

        if (Input.CompetitionCategoryId.HasValue && Input.JudgeUserId.HasValue)
        {
            var duplicateExists = await _dbContext.JudgeAssignments.AsNoTracking().AnyAsync(x =>
                x.CompetitionCategoryId == Input.CompetitionCategoryId.Value &&
                x.JudgeUserId == Input.JudgeUserId.Value &&
                x.ScheduleItemId == Input.ScheduleItemId &&
                x.CriterionId == Input.CriterionId);

            if (duplicateExists)
            {
                ModelState.AddModelError(string.Empty, "A matching judge assignment already exists. Please edit the existing assignment instead.");
            }
        }
    }

    private async Task LoadLookupOptionsAsync(Guid? selectedCategoryId)
    {
        CompetitionCategoryOptions = await _dbContext.CompetitionCategories.AsNoTracking()
            .Where(x => x.IsActive && x.Competition.IsActive && x.Competition.CompetitionYear.IsActive)
            .OrderByDescending(x => x.Competition.CompetitionYear.YearNo).ThenBy(x => x.Competition.Code).ThenBy(x => x.SortOrder)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Competition.Code + " / " + x.CategoryCode + " - " + x.Competition.NameEnglish })
            .ToListAsync();
        CompetitionCategoryOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select competition category..." });

        JudgeOptions = await _dbContext.UserProfiles.AsNoTracking()
            .Where(x => x.IsActive && x.UserRoleAssignmentUsers.Any(r => r.RoleCode == RoleEnums.Judge && r.IsActive && r.IsVerified))
            .OrderBy(x => x.FullNameAsPerId)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.FullNameAsPerId + " (" + x.Email + ")" })
            .ToListAsync();
        JudgeOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = JudgeOptions.Count == 0 ? "No active verified judges found" : "Select judge..." });

        var criteria = _dbContext.CompetitionScoringCriteria.AsNoTracking().Where(x => x.IsActive);
        var schedules = _dbContext.ScheduleItems.AsNoTracking().Where(x => x.ScheduleType == "COMPETITION" && x.CompetitionCategoryId != null);

        if (selectedCategoryId.HasValue)
        {
            criteria = criteria.Where(x => x.Competition.CompetitionCategories.Any(c => c.Id == selectedCategoryId.Value));
            schedules = schedules.Where(x => x.CompetitionCategoryId == selectedCategoryId.Value);
        }

        CriterionOptions = await criteria.OrderBy(x => x.Competition.Code).ThenBy(x => x.SortOrder)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Competition.Code + " / " + x.CriterionNo + ". " + x.CriterionName })
            .ToListAsync();
        CriterionOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "All criteria" });

        var scheduleItems = await schedules.OrderBy(x => x.ScheduleDate).ThenBy(x => x.StartTime)
            .Select(x => new { x.Id, x.ScheduleDate, x.StartTime, x.EndTime, x.DisplayTitle })
            .ToListAsync();

        ScheduleItemOptions = scheduleItems
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.ScheduleDate:yyyy-MM-dd} {x.StartTime:HH:mm}-{x.EndTime:HH:mm} / {x.DisplayTitle}"
            })
            .ToList();
        ScheduleItemOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Not linked" });
    }

    private static object Snapshot(JudgeAssignment assignment) => new
    {
        assignment.Id,
        assignment.CompetitionCategoryId,
        assignment.ScheduleItemId,
        assignment.JudgeUserId,
        assignment.CriterionId,
        assignment.IsActive,
        assignment.Notes,
        assignment.CreatedAt,
        assignment.CreatedBy,
        assignment.UpdatedAt,
        assignment.UpdatedBy
    };
}
