using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.Teams;

namespace PbmEventPortal.Web.Pages.Admin.Teams;

public class DetailsModel : PageModel
{
    private const int MaximumTeamManagersPerTeam = 3;

    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public DetailsModel(
        PbmDbContext dbContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public TeamDetailsDTO? Team { get; private set; }

    public List<EligibleTeamManagerViewModel> EligibleTeamManagers { get; private set; } = [];

    [BindProperty]
    public AssignTeamManagerInputModel AssignTeamManagerInput { get; set; } = new();

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var loaded = await LoadDetailsPageAsync(id);

        if (!loaded)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAssignTeamManagerAsync(Guid id)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        var teamExists = await _dbContext.Teams
            .AsNoTracking()
            .AnyAsync(team => team.Id == id);

        if (!teamExists)
        {
            return NotFound();
        }

        if (AssignTeamManagerInput.AssignmentId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(AssignTeamManagerInput.AssignmentId),
                "Please select a Team Manager.");
        }

        var currentManagerCount = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .CountAsync(roleAssignment =>
                roleAssignment.TeamId == id &&
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified);

        if (currentManagerCount >= MaximumTeamManagersPerTeam)
        {
            ModelState.AddModelError(
                string.Empty,
                $"This Team already has the maximum of {MaximumTeamManagersPerTeam} Team Managers.");
        }

        if (!ModelState.IsValid)
        {
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        var assignment = await _dbContext.UserRoleAssignments
            .FirstOrDefaultAsync(roleAssignment =>
                roleAssignment.Id == AssignTeamManagerInput.AssignmentId &&
                roleAssignment.RoleCode == RoleEnums.TeamManager);

        if (assignment == null)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment was not found.");
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        if (!assignment.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment is inactive.");
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        if (!assignment.IsVerified)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment is not verified.");
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        if (assignment.TeamId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager is already assigned to a Team.");
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        var userIsActive = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == assignment.UserId &&
                user.IsActive);

        if (!userIsActive)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager user profile is inactive.");
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        var userAlreadyAssignedToAnotherTeam = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(roleAssignment =>
                roleAssignment.Id != assignment.Id &&
                roleAssignment.UserId == assignment.UserId &&
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified &&
                roleAssignment.TeamId.HasValue);

        if (userAlreadyAssignedToAnotherTeam)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager is already assigned to another Team.");
            ShowFailureResult("Team Manager assignment failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        var oldSnapshot = CreateUserRoleAssignmentSnapshot(assignment);

        var now = DateTime.UtcNow;

        assignment.TeamId = id;
        assignment.UpdatedAt = now;
        assignment.UpdatedBy = actorId.Value;

        var newSnapshot = CreateUserRoleAssignmentSnapshot(assignment);

        await _auditLogService.LogAsync(
            entityName: "user_role_assignments",
            entityId: assignment.Id,
            actionName: "ASSIGN_TEAM_MANAGER",
            changedBy: actorId.Value,
            changeReason: "System Admin assigned Team Manager to team.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = "Team Manager assigned";
        ResultMessage = "The Team Manager has been assigned successfully.";

        await LoadDetailsPageAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostUnassignTeamManagerAsync(Guid id, Guid assignmentId)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        var teamExists = await _dbContext.Teams
            .AsNoTracking()
            .AnyAsync(team => team.Id == id);

        if (!teamExists)
        {
            return NotFound();
        }

        if (assignmentId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Invalid Team Manager assignment request.");
            ShowFailureResult("Team Manager removal failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        var assignment = await _dbContext.UserRoleAssignments
            .FirstOrDefaultAsync(roleAssignment =>
                roleAssignment.Id == assignmentId &&
                roleAssignment.RoleCode == RoleEnums.TeamManager);

        if (assignment == null)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment was not found.");
            ShowFailureResult("Team Manager removal failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        if (assignment.TeamId != id)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager is not assigned to this Team.");
            ShowFailureResult("Team Manager removal failed");
            await LoadDetailsPageAsync(id);
            return Page();
        }

        var oldSnapshot = CreateUserRoleAssignmentSnapshot(assignment);

        var now = DateTime.UtcNow;

        assignment.TeamId = null;
        assignment.UpdatedAt = now;
        assignment.UpdatedBy = actorId.Value;

        var newSnapshot = CreateUserRoleAssignmentSnapshot(assignment);

        await _auditLogService.LogAsync(
            entityName: "user_role_assignments",
            entityId: assignment.Id,
            actionName: "UNASSIGN_TEAM_MANAGER",
            changedBy: actorId.Value,
            changeReason: "System Admin removed Team Manager from team.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        ResultIsSuccess = true;
        ResultTitle = "Team Manager removed";
        ResultMessage = "The Team Manager has been removed from this Team successfully.";

        await LoadDetailsPageAsync(id);

        return Page();
    }

    private async Task<bool> LoadDetailsPageAsync(Guid id)
    {
        Team = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Id == id)
            .Select(team => new TeamDetailsDTO
            {
                Id = team.Id,
                Code = team.Code,
                NameEnglish = team.NameEnglish,
                NameGurmukhi = team.NameGurmukhi,
                StateId = team.StateId,
                StateCode = team.State.Code,
                StateName = team.State.Name,
                RegionId = team.RegionId,
                RegionCode = team.Region.Code,
                RegionName = team.Region.Name,
                IsActive = team.IsActive,
                CreatedAt = team.CreatedAt,
                CreatedBy = team.CreatedBy,
                UpdatedAt = team.UpdatedAt,
                UpdatedBy = team.UpdatedBy
            })
            .FirstOrDefaultAsync();

        if (Team == null)
        {
            return false;
        }

        await LoadAuditActorsAsync(Team);
        await LoadTeamManagersAsync(Team);
        await LoadEligibleTeamManagersAsync(Team);

        return true;
    }

    private async Task LoadAuditActorsAsync(TeamDetailsDTO team)
    {
        var auditActorIds = new[]
            {
                team.CreatedBy,
                team.UpdatedBy
            }
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToList();

        if (auditActorIds.Count == 0)
        {
            return;
        }

        var auditActors = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => auditActorIds.Contains(user.Id))
            .Select(user => new TeamAuditActorDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi
            })
            .ToDictionaryAsync(user => user.Id);

        if (team.CreatedBy.HasValue &&
            auditActors.TryGetValue(team.CreatedBy.Value, out var createdByUser))
        {
            team.CreatedByUser = createdByUser;
        }

        if (team.UpdatedBy.HasValue &&
            auditActors.TryGetValue(team.UpdatedBy.Value, out var updatedByUser))
        {
            team.UpdatedByUser = updatedByUser;
        }
    }

    private async Task LoadTeamManagersAsync(TeamDetailsDTO team)
    {
        team.TeamManagers = await (
            from roleAssignment in _dbContext.UserRoleAssignments.AsNoTracking()
            join userProfile in _dbContext.UserProfiles.AsNoTracking()
                on roleAssignment.UserId equals userProfile.Id
            where roleAssignment.TeamId == team.Id &&
                  roleAssignment.RoleCode == RoleEnums.TeamManager
            orderby roleAssignment.IsPrimary descending,
                    userProfile.FullNameAsPerId
            select new TeamManagerAssignmentDTO
            {
                AssignmentId = roleAssignment.Id,
                UserId = userProfile.Id,
                FullNameAsPerId = userProfile.FullNameAsPerId,
                FullNameGurmukhi = userProfile.FullNameGurmukhi,
                Email = userProfile.Email,
                MobileNumber = userProfile.MobileNumber,
                IsVerified = roleAssignment.IsVerified,
                IsPrimary = roleAssignment.IsPrimary,
                IsActive = roleAssignment.IsActive
            })
            .ToListAsync();
    }

    private async Task LoadEligibleTeamManagersAsync(TeamDetailsDTO team)
    {
        EligibleTeamManagers = [];

        if (team.TeamManagers.Count >= MaximumTeamManagersPerTeam)
        {
            return;
        }

        var alreadyAssignedTeamManagerUserIds = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(roleAssignment =>
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified &&
                roleAssignment.TeamId.HasValue)
            .Select(roleAssignment => roleAssignment.UserId)
            .Distinct()
            .ToListAsync();

        EligibleTeamManagers = await (
            from roleAssignment in _dbContext.UserRoleAssignments.AsNoTracking()
            join userProfile in _dbContext.UserProfiles.AsNoTracking()
                on roleAssignment.UserId equals userProfile.Id
            where roleAssignment.RoleCode == RoleEnums.TeamManager &&
                  roleAssignment.TeamId == null &&
                  roleAssignment.IsActive &&
                  roleAssignment.IsVerified &&
                  userProfile.IsActive &&
                  !alreadyAssignedTeamManagerUserIds.Contains(userProfile.Id)
            orderby userProfile.FullNameAsPerId
            select new EligibleTeamManagerViewModel
            {
                AssignmentId = roleAssignment.Id,
                UserId = userProfile.Id,
                FullNameAsPerId = userProfile.FullNameAsPerId,
                FullNameGurmukhi = userProfile.FullNameGurmukhi,
                Email = userProfile.Email,
                MobileNumber = userProfile.MobileNumber
            })
            .ToListAsync();
    }

    private void ShowFailureResult(string title)
    {
        ResultIsSuccess = false;
        ResultTitle = title;
        ResultMessage = "Please review the following and try again.";
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private static object CreateUserRoleAssignmentSnapshot(UserRoleAssignment roleAssignment)
    {
        return new
        {
            roleAssignment.Id,
            roleAssignment.UserId,
            roleAssignment.RoleCode,
            roleAssignment.TeamId,
            roleAssignment.IsVerified,
            roleAssignment.IsPrimary,
            roleAssignment.IsActive,
            roleAssignment.CreatedAt,
            roleAssignment.CreatedBy,
            roleAssignment.UpdatedAt,
            roleAssignment.UpdatedBy
        };
    }

    public class AssignTeamManagerInputModel
    {
        public Guid AssignmentId { get; set; }
    }

    public class EligibleTeamManagerViewModel
    {
        public Guid AssignmentId { get; set; }

        public Guid UserId { get; set; }

        public string FullNameAsPerId { get; set; } = string.Empty;

        public string? FullNameGurmukhi { get; set; }

        public string Email { get; set; } = string.Empty;

        public string MobileNumber { get; set; } = string.Empty;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(FullNameGurmukhi)
                ? FullNameAsPerId
                : $"{FullNameAsPerId} / {FullNameGurmukhi}";
    }
}
