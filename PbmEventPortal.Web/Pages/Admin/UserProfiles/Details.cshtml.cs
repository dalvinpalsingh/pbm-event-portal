using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.UserProfiles;

namespace PbmEventPortal.Web.Pages.Admin.UserProfiles;

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

    public UserProfileDetailsDTO? UserProfile { get; private set; }

    public List<SelectListItem> RoleOptions { get; private set; } = [];

    public List<SelectListItem> TeamOptions { get; private set; } = [];

    public UserProfileRoleAssignmentCreateInputDTO RoleInput { get; private set; } = new();

    public UserProfileTeamManagerTeamAssignmentInputDTO TeamManagerTeamInput { get; private set; } = new();

    [TempData]
    public string? RoleAssignmentResultStatus { get; set; }

    [TempData]
    public string? RoleAssignmentResultTitle { get; set; }

    [TempData]
    public string? RoleAssignmentResultMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var pageLoaded = await LoadPageDataAsync(id);

        return pageLoaded
            ? Page()
            : NotFound();
    }

    public async Task<IActionResult> OnPostAssignRoleAsync(
        Guid id,
        [Bind(Prefix = "RoleInput")] UserProfileRoleAssignmentCreateInputDTO roleInput)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        RoleInput = roleInput ?? new UserProfileRoleAssignmentCreateInputDTO();

        RoleInput.RoleCode = string.IsNullOrWhiteSpace(RoleInput.RoleCode)
            ? string.Empty
            : RoleInput.RoleCode.Trim().ToUpperInvariant();

        await ValidateRoleAssignmentAsync(id, RoleInput);

        if (!ModelState.IsValid)
        {
            RoleAssignmentResultStatus = "danger";
            RoleAssignmentResultTitle = "Role assignment failed";
            RoleAssignmentResultMessage = "Please review the following and try again.";

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var now = DateTime.UtcNow;

        var roleAssignment = new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = id,
            RoleCode = RoleInput.RoleCode,
            TeamId = null,
            IsVerified = RoleInput.IsVerified,
            IsPrimary = RoleInput.IsPrimary,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.UserRoleAssignments.Add(roleAssignment);

        await _auditLogService.LogAsync(
            entityName: "user_role_assignments",
            entityId: roleAssignment.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin assigned role to user.",
            oldValues: null,
            newValues: CreateRoleAssignmentSnapshot(roleAssignment));

        RoleAssignmentResultStatus = "success";
        RoleAssignmentResultTitle = "Role assigned";
        RoleAssignmentResultMessage = "The role has been assigned to this user successfully.";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignTeamToTeamManagerAsync(
        Guid id,
        [Bind(Prefix = "TeamManagerTeamInput")] UserProfileTeamManagerTeamAssignmentInputDTO teamManagerTeamInput)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        TeamManagerTeamInput = teamManagerTeamInput ?? new UserProfileTeamManagerTeamAssignmentInputDTO();

        if (TeamManagerTeamInput.AssignmentId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(TeamManagerTeamInput.AssignmentId),
                "Invalid Team Manager assignment request.");
        }

        if (TeamManagerTeamInput.TeamId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(TeamManagerTeamInput.TeamId),
                "Please select a Team.");
        }

        if (!ModelState.IsValid)
        {
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var userIsActive = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == id &&
                user.IsActive);

        if (!userIsActive)
        {
            ModelState.AddModelError(string.Empty, "User profile could not be found or is inactive.");
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var selectedTeamExists = await _dbContext.Teams
            .AsNoTracking()
            .AnyAsync(team =>
                team.Id == TeamManagerTeamInput.TeamId &&
                team.IsActive);

        if (!selectedTeamExists)
        {
            ModelState.AddModelError(
                nameof(TeamManagerTeamInput.TeamId),
                "Selected Team is not valid or inactive.");

            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var selectedTeamManagerCount = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .CountAsync(roleAssignment =>
                roleAssignment.TeamId == TeamManagerTeamInput.TeamId &&
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified);

        if (selectedTeamManagerCount >= MaximumTeamManagersPerTeam)
        {
            ModelState.AddModelError(
                nameof(TeamManagerTeamInput.TeamId),
                $"Selected Team already has the maximum of {MaximumTeamManagersPerTeam} Team Managers.");

            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var assignment = await _dbContext.UserRoleAssignments
            .FirstOrDefaultAsync(roleAssignment =>
                roleAssignment.Id == TeamManagerTeamInput.AssignmentId &&
                roleAssignment.UserId == id &&
                roleAssignment.RoleCode == RoleEnums.TeamManager);

        if (assignment == null)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment was not found.");
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        if (!assignment.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment is inactive.");
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        if (!assignment.IsVerified)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment is not verified.");
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        if (assignment.TeamId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "This Team Manager is already assigned to a Team.");
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var userAlreadyAssignedToAnotherTeam = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(roleAssignment =>
                roleAssignment.Id != assignment.Id &&
                roleAssignment.UserId == id &&
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified &&
                roleAssignment.TeamId.HasValue);

        if (userAlreadyAssignedToAnotherTeam)
        {
            ModelState.AddModelError(string.Empty, "This Team Manager is already assigned to another Team.");
            ShowTeamAssignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var oldSnapshot = CreateRoleAssignmentSnapshot(assignment);

        var now = DateTime.UtcNow;

        assignment.TeamId = TeamManagerTeamInput.TeamId;
        assignment.UpdatedAt = now;
        assignment.UpdatedBy = actorId.Value;

        var newSnapshot = CreateRoleAssignmentSnapshot(assignment);

        await _auditLogService.LogAsync(
            entityName: "user_role_assignments",
            entityId: assignment.Id,
            actionName: "ASSIGN_TEAM_TO_TEAM_MANAGER",
            changedBy: actorId.Value,
            changeReason: "System Admin assigned team to Team Manager from user profile details.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        RoleAssignmentResultStatus = "success";
        RoleAssignmentResultTitle = "Team assigned";
        RoleAssignmentResultMessage = "The Team has been assigned to this Team Manager successfully.";

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUnassignTeamFromTeamManagerAsync(
        Guid id,
        Guid assignmentId)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        if (assignmentId == Guid.Empty)
        {
            ModelState.AddModelError(string.Empty, "Invalid Team Manager assignment request.");
            ShowTeamUnassignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var userExists = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(user => user.Id == id);

        if (!userExists)
        {
            return NotFound();
        }

        var assignment = await _dbContext.UserRoleAssignments
            .FirstOrDefaultAsync(roleAssignment =>
                roleAssignment.Id == assignmentId &&
                roleAssignment.UserId == id &&
                roleAssignment.RoleCode == RoleEnums.TeamManager);

        if (assignment == null)
        {
            ModelState.AddModelError(string.Empty, "Selected Team Manager assignment was not found.");
            ShowTeamUnassignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        if (!assignment.TeamId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "This Team Manager is not currently assigned to a Team.");
            ShowTeamUnassignmentFailure();

            var pageLoaded = await LoadPageDataAsync(id);

            return pageLoaded
                ? Page()
                : NotFound();
        }

        var oldSnapshot = CreateRoleAssignmentSnapshot(assignment);

        var now = DateTime.UtcNow;

        assignment.TeamId = null;
        assignment.UpdatedAt = now;
        assignment.UpdatedBy = actorId.Value;

        var newSnapshot = CreateRoleAssignmentSnapshot(assignment);

        await _auditLogService.LogAsync(
            entityName: "user_role_assignments",
            entityId: assignment.Id,
            actionName: "UNASSIGN_TEAM_FROM_TEAM_MANAGER",
            changedBy: actorId.Value,
            changeReason: "System Admin removed team from Team Manager from user profile details.",
            oldValues: oldSnapshot,
            newValues: newSnapshot);

        RoleAssignmentResultStatus = "success";
        RoleAssignmentResultTitle = "Team removed";
        RoleAssignmentResultMessage = "The Team has been removed from this Team Manager successfully.";

        return RedirectToPage(new { id });
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private async Task<bool> LoadPageDataAsync(Guid id)
    {
        UserProfile = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserProfileDetailsDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi,
                IdType = user.IdType,
                NricNumber = user.NricNumber,
                PassportNumber = user.PassportNumber,
                PassportExpiryDate = user.PassportExpiryDate,
                MobileNumber = user.MobileNumber,
                Email = user.Email,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                CreatedBy = user.CreatedBy,
                UpdatedAt = user.UpdatedAt,
                UpdatedBy = user.UpdatedBy
            })
            .FirstOrDefaultAsync();

        if (UserProfile == null)
        {
            return false;
        }

        await LoadAuditActorsAsync(UserProfile);
        await LoadRoleAssignmentsAsync(UserProfile);
        await LoadRoleOptionsAsync(UserProfile);
        await LoadTeamOptionsAsync();

        return true;
    }

    private async Task LoadAuditActorsAsync(UserProfileDetailsDTO userProfile)
    {
        var auditActorIds = new[]
            {
                userProfile.CreatedBy,
                userProfile.UpdatedBy
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
            .Select(user => new UserProfileAuditActorDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi
            })
            .ToDictionaryAsync(user => user.Id);

        if (userProfile.CreatedBy.HasValue &&
            auditActors.TryGetValue(userProfile.CreatedBy.Value, out var createdByUser))
        {
            userProfile.CreatedByUser = createdByUser;
        }

        if (userProfile.UpdatedBy.HasValue &&
            auditActors.TryGetValue(userProfile.UpdatedBy.Value, out var updatedByUser))
        {
            userProfile.UpdatedByUser = updatedByUser;
        }
    }

    private async Task LoadRoleAssignmentsAsync(UserProfileDetailsDTO userProfile)
    {
        userProfile.RoleAssignments = await (
            from userRoleAssignment in _dbContext.UserRoleAssignments.AsNoTracking()
            join roleType in _dbContext.RoleTypes.AsNoTracking()
                on userRoleAssignment.RoleCode equals roleType.Code
            join team in _dbContext.Teams.AsNoTracking()
                on userRoleAssignment.TeamId equals team.Id into teamJoin
            from team in teamJoin.DefaultIfEmpty()
            where userRoleAssignment.UserId == userProfile.Id
            orderby userRoleAssignment.IsPrimary descending,
                    roleType.RoleScope,
                    roleType.Name
            select new UserProfileRoleAssignmentDTO
            {
                Id = userRoleAssignment.Id,
                RoleCode = userRoleAssignment.RoleCode,
                RoleName = roleType.Name,
                RoleScope = roleType.RoleScope,
                RoleDescription = roleType.Description,
                TeamId = userRoleAssignment.TeamId,
                TeamName = team == null ? null : team.NameEnglish,
                IsVerified = userRoleAssignment.IsVerified,
                IsPrimary = userRoleAssignment.IsPrimary,
                IsActive = userRoleAssignment.IsActive
            })
            .ToListAsync();
    }

    private async Task LoadRoleOptionsAsync(UserProfileDetailsDTO userProfile)
    {
        var existingNullTeamRoleCodes = userProfile.RoleAssignments
            .Where(roleAssignment => roleAssignment.TeamId == null)
            .Select(roleAssignment => roleAssignment.RoleCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        RoleOptions = await _dbContext.RoleTypes
            .AsNoTracking()
            .Where(roleType => roleType.IsActive)
            .OrderBy(roleType => roleType.RoleScope)
            .ThenBy(roleType => roleType.Name)
            .Select(roleType => new SelectListItem
            {
                Value = roleType.Code,
                Text = roleType.Name,
                Disabled = existingNullTeamRoleCodes.Contains(roleType.Code)
            })
            .ToListAsync();

        RoleOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select role..."
        });
    }

    private async Task LoadTeamOptionsAsync()
    {
        var teamManagerCounts = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(roleAssignment =>
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified &&
                roleAssignment.TeamId.HasValue)
            .GroupBy(roleAssignment => roleAssignment.TeamId!.Value)
            .Select(group => new
            {
                TeamId = group.Key,
                ManagerCount = group.Count()
            })
            .ToDictionaryAsync(
                item => item.TeamId,
                item => item.ManagerCount);

        var activeTeams = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.IsActive)
            .OrderBy(team => team.NameEnglish)
            .Select(team => new
            {
                team.Id,
                team.Code,
                team.NameEnglish
            })
            .ToListAsync();

        TeamOptions = activeTeams
            .Select(team =>
            {
                var managerCount = teamManagerCounts.TryGetValue(team.Id, out var count)
                    ? count
                    : 0;

                return new SelectListItem
                {
                    Value = team.Id.ToString(),
                    Text = $"{team.NameEnglish} ({team.Code}) - {managerCount}/{MaximumTeamManagersPerTeam} managers",
                    Disabled = managerCount >= MaximumTeamManagersPerTeam
                };
            })
            .ToList();

        TeamOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select team..."
        });
    }

    private async Task ValidateRoleAssignmentAsync(
        Guid userId,
        UserProfileRoleAssignmentCreateInputDTO roleInput)
    {
        var userExists = await _dbContext.UserProfiles
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId);

        if (!userExists)
        {
            ModelState.AddModelError(string.Empty, "User profile could not be found.");
            return;
        }

        if (string.IsNullOrWhiteSpace(roleInput.RoleCode))
        {
            ModelState.AddModelError(nameof(RoleInput.RoleCode), "Role is required.");
            return;
        }

        var roleExists = await _dbContext.RoleTypes
            .AsNoTracking()
            .AnyAsync(roleType =>
                roleType.Code == roleInput.RoleCode &&
                roleType.IsActive);

        if (!roleExists)
        {
            ModelState.AddModelError(nameof(RoleInput.RoleCode), "Selected role is not valid or inactive.");
            return;
        }

        var roleAlreadyAssigned = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(roleAssignment =>
                roleAssignment.UserId == userId &&
                roleAssignment.RoleCode == roleInput.RoleCode &&
                roleAssignment.TeamId == null);

        if (roleAlreadyAssigned)
        {
            ModelState.AddModelError(nameof(RoleInput.RoleCode), "This role is already assigned to the user.");
        }

        if (roleInput.IsPrimary)
        {
            var alreadyHasPrimaryRole = await _dbContext.UserRoleAssignments
                .AsNoTracking()
                .AnyAsync(roleAssignment =>
                    roleAssignment.UserId == userId &&
                    roleAssignment.IsActive &&
                    roleAssignment.IsPrimary);

            if (alreadyHasPrimaryRole)
            {
                ModelState.AddModelError(nameof(RoleInput.IsPrimary), "This user already has a primary role assignment.");
            }
        }
    }

    private void ShowTeamAssignmentFailure()
    {
        RoleAssignmentResultStatus = "danger";
        RoleAssignmentResultTitle = "Team assignment failed";
        RoleAssignmentResultMessage = "Please review the following and try again.";
    }

    private void ShowTeamUnassignmentFailure()
    {
        RoleAssignmentResultStatus = "danger";
        RoleAssignmentResultTitle = "Team removal failed";
        RoleAssignmentResultMessage = "Please review the following and try again.";
    }

    private static object CreateRoleAssignmentSnapshot(UserRoleAssignment roleAssignment)
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
}
