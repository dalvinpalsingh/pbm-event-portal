using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.JudgeConflicts;

namespace PbmEventPortal.Web.Pages.Admin.JudgeConflicts;

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
    public JudgeConflictEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> JudgeOptions { get; private set; } = [];
    public List<SelectListItem> ConflictTypeOptions { get; private set; } = [];
    public List<SelectListItem> TeamOptions { get; private set; } = [];
    public List<SelectListItem> ParticipantOptions { get; private set; } = [];
    public List<SelectListItem> CompetitionCategoryOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var conflict = await _dbContext.JudgeConflicts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (conflict == null) return NotFound();

        Input = new JudgeConflictEditInputDTO
        {
            Id = conflict.Id,
            JudgeUserId = conflict.JudgeUserId,
            ConflictType = conflict.ConflictType,
            TeamId = conflict.TeamId,
            ParticipantId = conflict.ParticipantId,
            CompetitionCategoryId = conflict.CompetitionCategoryId,
            Notes = conflict.Notes,
            IsActive = conflict.IsActive
        };

        await LoadLookupOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();
        if (Input.Id != id) ModelState.AddModelError(string.Empty, "Invalid judge conflict request.");

        NormalizeInput();
        await ValidateInputAsync(id);
        await LoadLookupOptionsAsync();

        if (!ModelState.IsValid)
        {
            ResultIsSuccess = false;
            ResultTitle = "Judge conflict update failed";
            ResultMessage = "Please review the following and try again.";
            return Page();
        }

        var conflict = await _dbContext.JudgeConflicts.FirstOrDefaultAsync(x => x.Id == id);
        if (conflict == null) return NotFound();

        var oldSnapshot = Snapshot(conflict);
        var oldIsActive = conflict.IsActive;

        conflict.JudgeUserId = Input.JudgeUserId!.Value;
        conflict.ConflictType = Input.ConflictType;
        conflict.TeamId = Input.TeamId;
        conflict.ParticipantId = Input.ParticipantId;
        conflict.CompetitionCategoryId = Input.CompetitionCategoryId;
        conflict.Notes = Input.Notes;
        conflict.IsActive = Input.IsActive;
        conflict.UpdatedAt = DateTime.UtcNow;
        conflict.UpdatedBy = actorId.Value;

        var actionName = oldIsActive != conflict.IsActive
            ? conflict.IsActive ? "ACTIVATE" : "DEACTIVATE"
            : "UPDATE";

        var changeReason = actionName switch
        {
            "ACTIVATE" => "System Admin reactivated judge conflict.",
            "DEACTIVATE" => "System Admin deactivated judge conflict.",
            _ => "System Admin updated judge conflict."
        };

        await _auditLogService.LogAsync("judge_conflicts", conflict.Id, actionName, actorId.Value,
            changeReason, oldSnapshot, Snapshot(conflict));

        ResultIsSuccess = true;
        ResultTitle = actionName switch
        {
            "ACTIVATE" => "Judge conflict reactivated",
            "DEACTIVATE" => "Judge conflict deactivated",
            _ => "Judge conflict updated"
        };
        ResultMessage = "The judge conflict has been updated successfully.";
        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private void NormalizeInput()
    {
        Input.ConflictType = string.IsNullOrWhiteSpace(Input.ConflictType) ? string.Empty : Input.ConflictType.Trim().ToUpperInvariant();
        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();

        if (Input.ConflictType == JudgeEnums.ConflictTypeOwnTeam)
        {
            Input.ParticipantId = null;
            Input.CompetitionCategoryId = null;
        }
        else if (Input.ConflictType == JudgeEnums.ConflictTypeRelatedParticipant)
        {
            Input.TeamId = null;
            Input.CompetitionCategoryId = null;
        }
        else if (Input.ConflictType == JudgeEnums.ConflictTypeCategoryBlock)
        {
            Input.TeamId = null;
            Input.ParticipantId = null;
        }
        else
        {
            Input.TeamId = null;
            Input.ParticipantId = null;
            Input.CompetitionCategoryId = null;
        }
    }

    private async Task ValidateInputAsync(Guid conflictId)
    {
        if (!Input.JudgeUserId.HasValue) ModelState.AddModelError(nameof(Input.JudgeUserId), "Judge is required.");
        else if (!await _dbContext.UserProfiles.AsNoTracking().AnyAsync(user => user.Id == Input.JudgeUserId.Value && user.IsActive && user.UserRoleAssignmentUsers.Any(role => role.RoleCode == RoleEnums.Judge && role.IsActive && role.IsVerified))) ModelState.AddModelError(nameof(Input.JudgeUserId), "Selected user must have an active and verified Judge role.");

        if (string.IsNullOrWhiteSpace(Input.ConflictType)) ModelState.AddModelError(nameof(Input.ConflictType), "Conflict type is required.");
        else if (!JudgeEnums.ConflictTypes.Contains(Input.ConflictType)) ModelState.AddModelError(nameof(Input.ConflictType), "Selected conflict type is not valid.");

        if (Input.ConflictType == JudgeEnums.ConflictTypeOwnTeam)
        {
            if (!Input.TeamId.HasValue) ModelState.AddModelError(nameof(Input.TeamId), "Team is required for Own team conflicts.");
            else if (!await _dbContext.Teams.AsNoTracking().AnyAsync(team => team.Id == Input.TeamId.Value && team.IsActive)) ModelState.AddModelError(nameof(Input.TeamId), "Selected team is not valid or inactive.");
        }
        else if (Input.ConflictType == JudgeEnums.ConflictTypeRelatedParticipant)
        {
            if (!Input.ParticipantId.HasValue) ModelState.AddModelError(nameof(Input.ParticipantId), "Participant is required for Related participant conflicts.");
            else if (!await _dbContext.Participants.AsNoTracking().AnyAsync(participant => participant.Id == Input.ParticipantId.Value && participant.IsActive)) ModelState.AddModelError(nameof(Input.ParticipantId), "Selected participant is not valid or inactive.");
        }
        else if (Input.ConflictType == JudgeEnums.ConflictTypeCategoryBlock)
        {
            if (!Input.CompetitionCategoryId.HasValue) ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Competition category is required for Category block conflicts.");
            else if (!await _dbContext.CompetitionCategories.AsNoTracking().AnyAsync(category => category.Id == Input.CompetitionCategoryId.Value && category.IsActive && category.Competition.IsActive && category.Competition.CompetitionYear.IsActive)) ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category is not valid or inactive.");
        }
        else if (Input.ConflictType == JudgeEnums.ConflictTypeCustom && string.IsNullOrWhiteSpace(Input.Notes))
        {
            ModelState.AddModelError(nameof(Input.Notes), "Notes are required for Custom conflicts.");
        }

        if (Input.JudgeUserId.HasValue && JudgeEnums.ConflictTypes.Contains(Input.ConflictType))
        {
            var duplicateExists = await _dbContext.JudgeConflicts.AsNoTracking().AnyAsync(conflict =>
                conflict.Id != conflictId &&
                conflict.JudgeUserId == Input.JudgeUserId.Value &&
                conflict.ConflictType == Input.ConflictType &&
                conflict.TeamId == Input.TeamId &&
                conflict.ParticipantId == Input.ParticipantId &&
                conflict.CompetitionCategoryId == Input.CompetitionCategoryId);

            if (duplicateExists) ModelState.AddModelError(string.Empty, "A matching judge conflict already exists. Please edit the existing conflict instead.");
        }
    }

    private async Task LoadLookupOptionsAsync()
    {
        JudgeOptions = await _dbContext.UserProfiles.AsNoTracking().Where(user => user.IsActive && user.UserRoleAssignmentUsers.Any(role => role.RoleCode == RoleEnums.Judge && role.IsActive && role.IsVerified)).OrderBy(user => user.FullNameAsPerId).Select(user => new SelectListItem { Value = user.Id.ToString(), Text = user.FullNameAsPerId + " (" + user.Email + ")" }).ToListAsync();
        JudgeOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = JudgeOptions.Count == 0 ? "No active verified judges found" : "Select judge..." });

        ConflictTypeOptions =
        [
            new SelectListItem { Value = string.Empty, Text = "Select conflict type..." },
            new SelectListItem { Value = JudgeEnums.ConflictTypeOwnTeam, Text = "Own team" },
            new SelectListItem { Value = JudgeEnums.ConflictTypeRelatedParticipant, Text = "Related participant" },
            new SelectListItem { Value = JudgeEnums.ConflictTypeCategoryBlock, Text = "Category block" },
            new SelectListItem { Value = JudgeEnums.ConflictTypeCustom, Text = "Custom" }
        ];

        TeamOptions = await _dbContext.Teams.AsNoTracking().Where(team => team.IsActive).OrderBy(team => team.Code).Select(team => new SelectListItem { Value = team.Id.ToString(), Text = team.Code + " - " + team.NameEnglish }).ToListAsync();
        TeamOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select team..." });

        ParticipantOptions = await _dbContext.Participants.AsNoTracking().Where(participant => participant.IsActive).OrderBy(participant => participant.FullNameAsPerId).Select(participant => new SelectListItem { Value = participant.Id.ToString(), Text = participant.FullNameAsPerId + " / " + participant.Team.Code }).ToListAsync();
        ParticipantOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = ParticipantOptions.Count == 0 ? "No active participants found" : "Select participant..." });

        CompetitionCategoryOptions = await _dbContext.CompetitionCategories.AsNoTracking().Where(category => category.IsActive && category.Competition.IsActive && category.Competition.CompetitionYear.IsActive).OrderByDescending(category => category.Competition.CompetitionYear.YearNo).ThenBy(category => category.Competition.Code).ThenBy(category => category.SortOrder).Select(category => new SelectListItem { Value = category.Id.ToString(), Text = category.Competition.Code + " / " + category.CategoryCode + " - " + category.Competition.NameEnglish }).ToListAsync();
        CompetitionCategoryOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select competition category..." });
    }

    private static object Snapshot(JudgeConflict conflict) => new
    {
        conflict.Id,
        conflict.JudgeUserId,
        conflict.ConflictType,
        conflict.TeamId,
        conflict.ParticipantId,
        conflict.CompetitionCategoryId,
        conflict.Notes,
        conflict.IsActive,
        conflict.CreatedAt,
        conflict.CreatedBy,
        conflict.UpdatedAt,
        conflict.UpdatedBy
    };
}
