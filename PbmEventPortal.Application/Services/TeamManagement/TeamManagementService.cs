using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PbmEventPortal.Application.DTOs.Common;
using PbmEventPortal.Application.DTOs.TeamManagement;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Helpers.Common;
using PbmEventPortal.Application.Helpers.TeamManagement;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Application.Services.Security;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using System.Globalization;
using System.Net.Mail;
using System.Text.Json;

namespace PbmEventPortal.Application.Services.TeamManagement;

public class TeamManagementService : ITeamManagementService
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly UserAccessDTO _currentUser;
    private readonly ILogger<TeamManagementService> _logger;

    public TeamManagementService(
        PbmDbContext dbContext,
        IAuditLogService auditLogService,
        IUserContext userContext,
        ILogger<TeamManagementService> logger)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _userContext = userContext;
        _currentUser = _userContext.GetCurrentUser();
        _logger = logger;
    }

    #region Participant Services
    public async Task<ResultDTO> GetCompetitionAssignmentsAsync(Guid? teamId, string? tahapCode, Guid? participantId)
    {
        try
        {
            var assignedTeamId = await GetAccessibleTeamIdAsync(teamId);
            if (assignedTeamId == null)
            {
                return ResultDTO.Fail(NoticeEnums.ForbiddenTeamAccessTitle, NoticeEnums.ForbiddenTeamAccessDetails);
            }

            var normalizedTahapCode = string.IsNullOrWhiteSpace(tahapCode) ? string.Empty : tahapCode.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedTahapCode))
            {
                return ResultDTO.Fail(NoticeEnums.TahapIsRequiredTitle, NoticeEnums.TahapIsRequiredDetails);
            }

            var participantIdToExclude = GuidHelper.IsValidGuid(participantId)
                ? participantId
                : (Guid?)null;

            if (participantIdToExclude != null)
            {
                var participantExists = await _dbContext.Participants
                    .AsNoTracking()
                    .AnyAsync(p =>
                        p.Id == participantIdToExclude &&
                        p.TeamId == teamId &&
                        p.IsActive);

                if (!participantExists)
                {
                    return ResultDTO.Fail(NoticeEnums.ParticipantNotFoundTitle, NoticeEnums.ParticipantNotFoundDetails);
                }
            }

            var categoryRows = await _dbContext.CompetitionCategories
                .AsNoTracking()
                .Where(c =>
                    c.IsActive &&
                    c.Competition.IsActive &&
                    c.Competition.CompetitionYear.IsActive)
                .OrderBy(c => c.Competition.Code)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.CategoryCode)
                .Select(c => new CompetitionAssignmentCategoryDTO
                {
                    Id = c.Id,
                    CompetitionCode = c.Competition.Code,
                    CompetitionName = c.Competition.NameEnglish,
                    CategoryCode = c.CategoryCode,
                    EventType = c.EventType,
                    SortOrder = c.SortOrder,
                    IndividualEventMaxParticipants = c.IndividualEventMaxParticipants,
                    TeamEventTeamSize = c.TeamEventTeamSize
                })
                .ToListAsync();

            var eligibleCategories = categoryRows
                .Where(c => TeamManagementHelper.CategoryAppliesToTahap(c.CategoryCode, normalizedTahapCode))
                .ToList();

            var eligibleCategoryIds = eligibleCategories.Select(c => c.Id).ToList();

            var existingCounts = eligibleCategoryIds.Count == 0
                ? new Dictionary<Guid, int>()
                : await _dbContext.RegistrationEntryMembers
                    .AsNoTracking()
                    .Where(m =>
                        m.IsActive &&
                        m.RegistrationEntry.IsActive &&
                        m.RegistrationEntry.TeamId == assignedTeamId.Value &&
                        (!participantIdToExclude.HasValue || m.ParticipantId != participantIdToExclude.Value) &&
                        eligibleCategoryIds.Contains(m.RegistrationEntry.CompetitionCategoryId))
                    .GroupBy(m => m.RegistrationEntry.CompetitionCategoryId)
                    .Select(g => new
                    {
                        CompetitionCategoryId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(r => r.CompetitionCategoryId, r => r.Count);

            var optionRows = eligibleCategories
                .Select(c => new TeamCompetitionAssignmentOptionDTO
                {
                    CompetitionCategoryId = c.Id,
                    CompetitionCode = c.CompetitionCode,
                    CompetitionName = c.CompetitionName,
                    CategoryCode = c.CategoryCode,
                    CategoryShortName = TeamManagementHelper.GetCategoryShortName(c.CategoryCode),
                    EventType = c.EventType,
                    TargetCount = TeamManagementHelper.GetTargetCount(c.EventType, c.IndividualEventMaxParticipants, c.TeamEventTeamSize),
                    CurrentCount = existingCounts.TryGetValue(c.Id, out var count) ? count : 0,
                    DisplayText = $"{c.CompetitionCode} / {TeamManagementHelper.GetCategoryShortName(c.CategoryCode)} - {c.CompetitionName}"
                })
                .ToList();
            _logger.LogInformation(NoticeEnums.CompetitionAssignmentsFoundTitle);
            return ResultDTO.Success(NoticeEnums.CompetitionAssignmentsFoundTitle, NoticeEnums.CompetitionAssignmentsFoundDetails, null, optionRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, NoticeEnums.CompetitionAssignmentsFailedToLoadDetails);
            var errors = new List<string> { ex.Message, ex.StackTrace ?? string.Empty };
            return ResultDTO.Fail(NoticeEnums.UnexpectedErrorTitle, NoticeEnums.UnexpectedErrorDetails, errors);
        }
    }

    public async Task<TeamManagerAccessDTO> GetPageAccessAsync(Guid? teamId)
    {
        var access = new TeamManagerAccessDTO { UserAccess = _currentUser };
        var userId = _currentUser.UserId;
        var isValidUserId = GuidHelper.IsValidGuid(userId);
        var isValidTeamId = GuidHelper.IsValidGuid(teamId);

        if (!isValidUserId)
        {
            access.ValidationResult = ResultDTO.Fail(NoticeEnums.UserNotFoundTitle, NoticeEnums.UserNotFoundDetails);
            return access;
        }

        if (!_currentUser.IsUserAuthenticated)
        {
            access.ValidationResult = ResultDTO.Fail(NoticeEnums.UserNotAuthenticated, NoticeEnums.UserNotAuthenticatedDetails);
            return access;
        }

        if (_currentUser.UserHasAnyAdminRole)
        {
            if (!isValidTeamId)
            {
                access.ValidationResult = ResultDTO.Success(NoticeEnums.TeamIdNotProvidedTitle, NoticeEnums.TeamIdNotProvidedDetails);
                return access;
            }

            var teamExists = await _dbContext.Teams
                .AsNoTracking()
                .AnyAsync(team => team.Id == teamId && team.IsActive);

            if (!teamExists)
            {
                access.ValidationResult = ResultDTO.Fail(NoticeEnums.TeamNotFoundTitle, NoticeEnums.TeamNotFoundDetails);
                return access;
            }

            access.SelectedTeamId = teamId;
            access.ValidationResult = ResultDTO.Success(NoticeEnums.TeamFoundTitle, NoticeEnums.TeamFoundDetails);
            return access;
        }

        if (!isValidTeamId)
        {
            access.ValidationResult = ResultDTO.Success(NoticeEnums.TeamIdNotProvidedTitle, NoticeEnums.TeamIdNotProvidedDetails);
            return access;
        }

        var assignedTeamId = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(roleAssignment =>
                roleAssignment.UserId == userId &&
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified &&
                roleAssignment.TeamId != null &&
                roleAssignment.Team != null &&
                roleAssignment.Team.IsActive)
            .Select(roleAssignment => roleAssignment.TeamId)
            .FirstOrDefaultAsync();

        if (!GuidHelper.IsValidGuid(assignedTeamId))
        {
            access.ValidationResult = ResultDTO.Fail(NoticeEnums.NoTeamAssignedTitle, NoticeEnums.NoTeamAssignedDetails);
            return access;
        }

        access.SelectedTeamId = teamId;
        access.HasAssignedTeam = true;
        access.ValidationResult = ResultDTO.Success(NoticeEnums.TeamFoundTitle, NoticeEnums.TeamFoundDetails);
        return access;
    }

    public async Task<List<TeamParticipantSummaryDTO>> GetParticipantsAsync(Guid? teamId)
    {
        if (!GuidHelper.IsValidGuid(teamId))
        {
            return [];
        }

        try
        {
            var teamParticipants = await _dbContext.Participants
                .AsNoTracking()
                .Where(p =>
                    p.TeamId == teamId &&
                    p.IsActive)
                .OrderBy(p => p.TahapCodeNavigation.SortOrder)
                .ThenBy(p => p.FullNameAsPerId)
                .Select(p => new TeamParticipantSummaryDTO
                {
                    ParticipantId = p.Id,
                    FullName = p.FullNameAsPerId,
                    FullNameGurmukhi = p.FullNameGurmukhi,
                    TahapCode = p.TahapCode,
                    TahapName = p.TahapCodeNavigation.Name,
                    Gender = p.Gender.ToString(),
                    DateOfBirth = p.DateOfBirth,
                    IdType = p.IdType,
                    NricNumber = p.NricNumber,
                    PassportNumber = p.PassportNumber,
                    PassportExpiryDate = p.PassportExpiryDate,
                    MobileNumber = p.MobileNumber,
                    Email = p.Email,
                    ParentGuardianName = p.ParentGuardianName,
                    ParentGuardianMobile = p.ParentGuardianMobile
                })
                .ToListAsync();

            if (teamParticipants.Count == 0)
            {
                return [];
            }

            var participantIds = teamParticipants
                .Select(p => p.ParticipantId)
                .ToList();

            var participantCompetitions = await _dbContext.RegistrationEntryMembers
                .AsNoTracking()
                .Where(m =>
                    participantIds.Contains(m.ParticipantId) &&
                    m.IsActive &&
                    m.RegistrationEntry.IsActive &&
                    m.RegistrationEntry.TeamId == teamId)
                .OrderBy(m => m.RegistrationEntry.CompetitionCategory.EventType)
                .ThenBy(m => m.RegistrationEntry.CompetitionCategory.SortOrder)
                .ThenBy(m => m.RegistrationEntry.CompetitionCategory.Competition.NameEnglish)
                .Select(m => new ParticipantCompetitionSummaryDTO
                {
                    ParticipantId = m.ParticipantId,
                    EntryType = m.RegistrationEntry.CompetitionCategory.EventType,
                    CompetitionName = m.RegistrationEntry.CompetitionCategory.Competition.NameEnglish,
                    EntryStatus = m.RegistrationEntry.EntryStatus
                })
                .ToListAsync();

            foreach (var participant in teamParticipants)
            {
                participant.RegisteredCompetitions = participantCompetitions
                    .Where(pc => pc.ParticipantId == participant.ParticipantId)
                    .ToList();
            }
            _logger.LogInformation(NoticeEnums.ParticipantsFoundTitle);
            return teamParticipants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, NoticeEnums.ParticipantNotFoundDetails);
            var errors = new List<string> { ex.Message, ex.StackTrace ?? string.Empty };
            return [];
        }
    }

    #endregion






    #region Draft Services

    #endregion


    #region Common Services
    public async Task<Guid?> GetAccessibleTeamIdAsync(Guid? requestedTeamId)
    {
        // To-do: make private after refactor
        var userId = _currentUser.UserId;
        if (!GuidHelper.IsValidGuid(userId))
        {
            return null;
        }

        if (!_currentUser.IsUserAuthenticated)
        {
            return null;
        }

        if (_currentUser.UserHasAnyAdminRole)
        {
            if (!GuidHelper.IsValidGuid(requestedTeamId))
            {
                return null;
            }

            var teamExists = await _dbContext.Teams
                .AsNoTracking()
                .AnyAsync(team => team.Id == requestedTeamId && team.IsActive);

            return teamExists ? requestedTeamId : null;
        }

        var assignedTeamId = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(roleAssignment =>
                roleAssignment.UserId == userId &&
                roleAssignment.RoleCode == RoleEnums.TeamManager &&
                roleAssignment.IsActive &&
                roleAssignment.IsVerified &&
                roleAssignment.TeamId != null &&
                roleAssignment.Team != null &&
                roleAssignment.Team.IsActive)
            .Select(roleAssignment => roleAssignment.TeamId)
            .FirstOrDefaultAsync();

        return assignedTeamId == requestedTeamId ? requestedTeamId : null;
    }

    public async Task<ResultDTO> GetTeamManagementWorkspaceAsync(Guid? teamId)
    {
        var workspace = new TeamManagementWorkspaceDTO();
        var currentUserId = _currentUser.UserId;

        if (!GuidHelper.IsValidGuid(currentUserId))
        {
            return ResultDTO.Fail(NoticeEnums.UserNotFoundTitle, NoticeEnums.UserNotFoundDetails);
        }

        if (!GuidHelper.IsValidGuid(teamId))
        {
            return ResultDTO.Fail(NoticeEnums.TeamNotFoundTitle, NoticeEnums.TeamNotFoundDetails);
        }

        try
        {
            workspace.TeamSummary = await _dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new TeamSummaryDTO
            {
                TeamId = t.Id,
                TeamCode = t.Code,
                TeamName = t.NameEnglish,
                TeamNameGurmukhi = t.NameGurmukhi,
                StateName = t.State.Name,
                RegionName = t.Region.Name,
                ActiveParticipantCount = t.Participants.Count(participant => participant.IsActive),
                ActiveRegistrationEntryCount = t.RegistrationEntries.Count(entry => entry.IsActive),
                ApprovedRegistrationEntryCount = t.RegistrationEntries.Count(entry =>
                    entry.IsActive && entry.EntryStatus == RegistrationEnums.EntryStatusApproved),
                PendingRegistrationEntryCount = t.RegistrationEntries.Count(entry =>
                    entry.IsActive &&
                    entry.EntryStatus != RegistrationEnums.EntryStatusApproved &&
                    entry.EntryStatus != RegistrationEnums.EntryStatusRejected),
                RejectedRegistrationEntryCount = t.RegistrationEntries.Count(entry =>
                    entry.IsActive && entry.EntryStatus == RegistrationEnums.EntryStatusRejected)
            })
            .FirstOrDefaultAsync();

            workspace.TeamManagers = await _dbContext.UserRoleAssignments
                .AsNoTracking()
                .Where(ra =>
                    ra.RoleCode == RoleEnums.TeamManager &&
                    ra.TeamId == teamId &&
                    ra.IsActive &&
                    ra.IsVerified &&
                    ra.User.IsActive)
                .OrderBy(ra => ra.User.FullNameAsPerId)
                .Select(ra => new TeamManagerSummaryDTO
                {
                    UserId = ra.UserId,
                    FullName = ra.User.FullNameAsPerId,
                    Email = ra.User.Email,
                    MobileNumber = ra.User.MobileNumber,
                    IsPrimary = ra.IsPrimary,
                    IsCurrentUser = ra.UserId == currentUserId
                })
                .ToListAsync();

            workspace.RegistrationStatuses = await _dbContext.RegistrationEntries
                .AsNoTracking()
                .Where(e =>
                    e.TeamId == teamId &&
                    e.IsActive)
                .GroupBy(e => e.EntryStatus)
                .Select(g => new RegistrationStatusDTO
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderBy(r => r.Status)
                .ToListAsync();

            workspace.Participants = await GetParticipantsAsync(teamId);
            workspace.Drafts = await GetDraftApplicationsAsync(teamId); //to-do, fix GetDraftApplicationsAsync method.

            _logger.LogInformation(NoticeEnums.TeamManagementWorkspaceLoadedTitle);
            return ResultDTO.Success(NoticeEnums.TeamManagementWorkspaceLoadedTitle, NoticeEnums.TeamManagementWorkspaceLoadedDetails, null, workspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, NoticeEnums.TeamManagementWorkspaceFailedToLoadDetails);
            var errors = new List<string> { ex.Message, ex.StackTrace ?? string.Empty };
            return ResultDTO.Fail(NoticeEnums.UnexpectedErrorTitle, NoticeEnums.UnexpectedErrorDetails, errors);
        }
    }

    public async Task<List<TeamDTO>> GetTeamsAsync()
    {
        return await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.IsActive)
            .OrderBy(team => team.NameEnglish)
            .Select(team => new TeamDTO
            {
                TeamId = team.Id,
                TeamCode = team.Code,
                TeamName = team.NameEnglish
            })
            .ToListAsync();
    }

    #endregion



    #region Private methods

    #endregion

    public async Task<TeamParticipantSubmitResultDTO> CreateTeamParticipantAsync(Guid teamId, TeamParticipantSubmitRequest request)
    {
        var actorId = _currentUser.UserId;
        if (!actorId.HasValue)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "You must be signed in before adding a participant." };
        }

        NormalizeSubmitRequest(request);

        if (request.TeamId != teamId)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "You do not have access to add participants for this team." };
        }

        var validation = await ValidateSubmitRequestAsync(request, teamId);
        if (!validation.IsValid)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be added.", Errors = validation.Errors };
        }

        TeamParticipantRegistrationDraft? submittedDraft = null;

        if (request.DraftId.HasValue)
        {
            submittedDraft = await _dbContext.TeamParticipantRegistrationDrafts
                .FirstOrDefaultAsync(draft =>
                    draft.Id == request.DraftId.Value &&
                    draft.TeamId == teamId &&
                    draft.DraftStatus == RegistrationEnums.DraftStatusDraft &&
                    draft.IsActive);

            if (submittedDraft == null)
            {
                return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Draft could not be found or is no longer editable." };
            }
        }

        var now = DateTime.UtcNow;
        var participant = new Participant
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = validation.CompetitionYearId!.Value,
            TeamId = teamId,
            FullNameAsPerId = request.FullNameAsPerId,
            FullNameGurmukhi = request.FullNameGurmukhi,
            IdType = request.IdType,
            NricNumber = request.NricNumber,
            PassportNumber = request.PassportNumber,
            PassportExpiryDate = request.PassportExpiryDate,
            MobileNumber = request.MobileNumber,
            Email = request.Email,
            ParentGuardianName = request.ParentGuardianName,
            ParentGuardianMobile = request.ParentGuardianMobile,
            Gender = request.Gender[0],
            DateOfBirth = request.DateOfBirth!.Value,
            TahapCode = request.TahapCode,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actorId.Value
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {

            _dbContext.Participants.Add(participant);
            await _auditLogService.LogAsync("participants", participant.Id, "CREATE", actorId.Value,
                "Team Manager created participant from TeamManagement workspace.", null, TeamManagementHelper.ParticipantSnapshot(participant));

            foreach (var category in validation.SelectedIndividualCategories)
            {
                var entry = CreateRegistrationEntry(category, teamId, participant.Id, actorId.Value, now, RegistrationEnums.EntryStatusSubmitted);
                _dbContext.RegistrationEntries.Add(entry);
                await _auditLogService.LogAsync("registration_entries", entry.Id, "CREATE", actorId.Value,
                    "Team Manager submitted individual registration entry from TeamManagement workspace.", null, TeamManagementHelper.RegistrationEntrySnapshot(entry));
            }

            foreach (var category in validation.SelectedTeamCategories)
            {
                var existingEntry = await LoadBestExistingTeamEntryAsync(category, teamId);
                if (existingEntry == null)
                {
                    var entry = CreateRegistrationEntry(category, teamId, participant.Id, actorId.Value, now, RegistrationEnums.EntryStatusSubmitted);
                    _dbContext.RegistrationEntries.Add(entry);
                    await _auditLogService.LogAsync("registration_entries", entry.Id, "CREATE", actorId.Value,
                        "Team Manager submitted team registration entry from TeamManagement workspace.", null, TeamManagementHelper.RegistrationEntrySnapshot(entry));
                }
                else
                {
                    var oldSnapshot = TeamManagementHelper.RegistrationEntrySnapshot(existingEntry);
                    existingEntry.RegistrationEntryMembers.Add(new RegistrationEntryMember
                    {
                        Id = Guid.NewGuid(),
                        RegistrationEntryId = existingEntry.Id,
                        ParticipantId = participant.Id,
                        MemberOrder = (short)(existingEntry.RegistrationEntryMembers.Where(member => member.IsActive && member.Participant.IsActive).Select(member => (int)member.MemberOrder).DefaultIfEmpty(0).Max() + 1),
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = actorId.Value
                    });

                    if (existingEntry.EntryStatus == RegistrationEnums.EntryStatusDraft)
                    {
                        existingEntry.EntryStatus = RegistrationEnums.EntryStatusSubmitted;
                    }
                    existingEntry.UpdatedAt = now;
                    existingEntry.UpdatedBy = actorId.Value;

                    await _auditLogService.LogAsync("registration_entries", existingEntry.Id, "UPDATE", actorId.Value,
                        "Team Manager submitted participant into team registration entry from TeamManagement workspace.", oldSnapshot, TeamManagementHelper.RegistrationEntrySnapshot(existingEntry));
                }
            }

            if (submittedDraft != null)
            {
                var oldDraftSnapshot = DraftSnapshot(submittedDraft);

                _dbContext.TeamParticipantRegistrationDrafts.Remove(submittedDraft);

                await _auditLogService.LogAsync(
                    "team_participant_registration_drafts",
                    submittedDraft.Id,
                    "DELETE",
                    actorId.Value,
                    "Team Manager submitted participant registration draft for approval from TeamManagement workspace.",
                    oldDraftSnapshot,
                    null);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            if (TryBuildParticipantUniqueConstraintResult(exception, "Participant could not be added.", out var duplicateResult))
            {
                return duplicateResult;
            }

            _logger.LogError(exception, "Participant could not be added for team {TeamId}.", teamId);
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be added." };
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError(exception, "Participant could not be added for team {TeamId}.", teamId);
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be added." };
        }

        return new TeamParticipantSubmitResultDTO
        {
            IsSuccess = true,
            NoticeTitle = "Participant added successfully.",
            ParticipantId = participant.Id
        };
    }

    public async Task<TeamParticipantSubmitResultDTO> EditTeamParticipantAsync(Guid teamId, TeamParticipantSubmitRequest request)
    {
        var actorId = _currentUser.UserId;
        if (!actorId.HasValue)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "You must be signed in before updating a participant." };
        }

        if (!request.ParticipantId.HasValue || request.ParticipantId.Value == Guid.Empty)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant is required before updating." };
        }

        NormalizeSubmitRequest(request);

        if (request.TeamId != teamId)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "You do not have access to update participants for this team." };
        }

        var participant = await _dbContext.Participants
            .Include(row => row.RegistrationEntryMembers)
                .ThenInclude(member => member.RegistrationEntry)
                    .ThenInclude(entry => entry.RegistrationEntryMembers)
                        .ThenInclude(entryMember => entryMember.Participant)
            .Include(row => row.RegistrationEntryMembers)
                .ThenInclude(member => member.RegistrationEntry)
                    .ThenInclude(entry => entry.CompetitionCategory)
                        .ThenInclude(category => category.Competition)
            .FirstOrDefaultAsync(row =>
                row.Id == request.ParticipantId.Value &&
                row.TeamId == teamId &&
                row.IsActive);

        if (participant == null)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be found or is no longer active." };
        }

        var activeMemberships = participant.RegistrationEntryMembers
            .Where(member =>
                member.IsActive &&
                member.RegistrationEntry.IsActive &&
                member.RegistrationEntry.TeamId == teamId)
            .ToList();

        var currentIndividualCategoryIds = activeMemberships
            .Where(member => member.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeIndividual)
            .Select(member => member.RegistrationEntry.CompetitionCategoryId)
            .Distinct()
            .ToList();

        var currentTeamCategoryIds = activeMemberships
            .Where(member => member.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeTeam)
            .Select(member => member.RegistrationEntry.CompetitionCategoryId)
            .Distinct()
            .ToList();

        var validation = await ValidateSubmitRequestAsync(
            request,
            teamId,
            participant.Id,
            currentIndividualCategoryIds,
            currentTeamCategoryIds);

        if (!validation.IsValid)
        {
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be updated.", Errors = validation.Errors };
        }

        var selectedCategoryIds = request.IndividualCompetitionCategoryIds
            .Concat(request.TeamCompetitionCategoryIds)
            .ToHashSet();
        var entryOldSnapshots = new Dictionary<Guid, object>();
        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {

            var oldParticipantSnapshot = TeamManagementHelper.ParticipantSnapshot(participant);

            participant.CompetitionYearId = validation.CompetitionYearId!.Value;
            participant.FullNameAsPerId = request.FullNameAsPerId;
            participant.FullNameGurmukhi = request.FullNameGurmukhi;
            participant.IdType = request.IdType;
            participant.NricNumber = request.NricNumber;
            participant.PassportNumber = request.PassportNumber;
            participant.PassportExpiryDate = request.PassportExpiryDate;
            participant.MobileNumber = request.MobileNumber;
            participant.Email = request.Email;
            participant.ParentGuardianName = request.ParentGuardianName;
            participant.ParentGuardianMobile = request.ParentGuardianMobile;
            participant.Gender = request.Gender[0];
            participant.DateOfBirth = request.DateOfBirth!.Value;
            participant.TahapCode = request.TahapCode;
            participant.UpdatedAt = now;
            participant.UpdatedBy = actorId.Value;

            foreach (var member in activeMemberships.Where(member => !selectedCategoryIds.Contains(member.RegistrationEntry.CompetitionCategoryId)))
            {
                var entry = member.RegistrationEntry;
                CaptureRegistrationEntrySnapshot(entryOldSnapshots, entry);

                member.IsActive = false;
                member.UpdatedAt = now;
                member.UpdatedBy = actorId.Value;

                if (entry.RegistrationEntryMembers.All(entryMember => !entryMember.IsActive || !entryMember.Participant.IsActive))
                {
                    entry.IsActive = false;
                }
                else
                {
                    entry.EntryStatus = RegistrationEnums.EntryStatusSubmitted;
                }

                entry.UpdatedAt = now;
                entry.UpdatedBy = actorId.Value;
            }

            foreach (var category in validation.SelectedIndividualCategories.Where(category => !currentIndividualCategoryIds.Contains(category.Id)))
            {
                var entry = CreateRegistrationEntry(category, teamId, participant.Id, actorId.Value, now, RegistrationEnums.EntryStatusSubmitted);
                _dbContext.RegistrationEntries.Add(entry);
                await _auditLogService.LogAsync("registration_entries", entry.Id, "CREATE", actorId.Value,
                    "Team Manager submitted individual registration entry update from TeamManagement workspace.", null, TeamManagementHelper.RegistrationEntrySnapshot(entry));
            }

            foreach (var category in validation.SelectedTeamCategories.Where(category => !currentTeamCategoryIds.Contains(category.Id)))
            {
                var existingEntry = await LoadBestExistingTeamEntryAsync(category, teamId);
                if (existingEntry == null)
                {
                    var entry = CreateRegistrationEntry(category, teamId, participant.Id, actorId.Value, now, RegistrationEnums.EntryStatusSubmitted);
                    _dbContext.RegistrationEntries.Add(entry);
                    await _auditLogService.LogAsync("registration_entries", entry.Id, "CREATE", actorId.Value,
                        "Team Manager submitted team registration entry update from TeamManagement workspace.", null, TeamManagementHelper.RegistrationEntrySnapshot(entry));
                }
                else
                {
                    CaptureRegistrationEntrySnapshot(entryOldSnapshots, existingEntry);
                    existingEntry.RegistrationEntryMembers.Add(new RegistrationEntryMember
                    {
                        Id = Guid.NewGuid(),
                        RegistrationEntryId = existingEntry.Id,
                        ParticipantId = participant.Id,
                        MemberOrder = (short)(existingEntry.RegistrationEntryMembers.Where(member => member.IsActive && member.Participant.IsActive).Select(member => (int)member.MemberOrder).DefaultIfEmpty(0).Max() + 1),
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = actorId.Value
                    });

                    existingEntry.EntryStatus = RegistrationEnums.EntryStatusSubmitted;
                    existingEntry.UpdatedAt = now;
                    existingEntry.UpdatedBy = actorId.Value;
                }
            }

            await _auditLogService.LogAsync("participants", participant.Id, "UPDATE", actorId.Value,
                "Team Manager updated participant from TeamManagement workspace.", oldParticipantSnapshot, TeamManagementHelper.ParticipantSnapshot(participant));

            foreach (var (entryId, oldSnapshot) in entryOldSnapshots)
            {
                var entry = await _dbContext.RegistrationEntries
                    .Include(row => row.RegistrationEntryMembers)
                    .FirstAsync(row => row.Id == entryId);

                await _auditLogService.LogAsync("registration_entries", entry.Id, "UPDATE", actorId.Value,
                    "Team Manager updated participant registration assignments from TeamManagement workspace.", oldSnapshot, TeamManagementHelper.RegistrationEntrySnapshot(entry));
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            if (TryBuildParticipantUniqueConstraintResult(exception, "Participant could not be updated.", out var duplicateResult))
            {
                return duplicateResult;
            }

            _logger.LogError(exception, "Participant could not be updated for team {TeamId}, participant {ParticipantId}.", teamId, request.ParticipantId);
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be updated." };
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError(exception, "Participant could not be updated for team {TeamId}, participant {ParticipantId}.", teamId, request.ParticipantId);
            return new TeamParticipantSubmitResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be updated." };
        }

        return new TeamParticipantSubmitResultDTO
        {
            IsSuccess = true,
            NoticeTitle = "Participant updated successfully.",
            ParticipantId = participant.Id
        };
    }

    public async Task<ResultDTO> DeleteTeamParticipantAsync(Guid teamId, Guid participantId)
    {
        var actorId = _currentUser.UserId;
        if (!actorId.HasValue)
        {
            return new ResultDTO { IsSuccess = false, NoticeTitle = "You must be signed in before deleting a participant." };
        }

        if (EventHelper.IsLockingPeriodActive())
        {
            return new ResultDTO { IsSuccess = false, NoticeTitle = "Participant changes are currently locked." };
        }

        var participant = await _dbContext.Participants
            .Include(row => row.RegistrationEntryMembers)
                .ThenInclude(member => member.RegistrationEntry)
                    .ThenInclude(entry => entry.RegistrationEntryMembers)
            .FirstOrDefaultAsync(row =>
                row.Id == participantId &&
                row.TeamId == teamId &&
                row.IsActive);

        if (participant == null)
        {
            return new ResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be deleted or is already deleted." };
        }

        var participantMemberRows = participant.RegistrationEntryMembers.ToList();
        var participantMemberIds = participantMemberRows
            .Select(member => member.Id)
            .ToHashSet();
        var affectedEntries = participantMemberRows
            .Select(member => member.RegistrationEntry)
            .DistinctBy(entry => entry.Id)
            .ToList();
        var affectedEntryIds = affectedEntries
            .Select(entry => entry.Id)
            .ToHashSet();
        var entriesToDelete = affectedEntries
            .Where(entry => entry.RegistrationEntryMembers.All(member => participantMemberIds.Contains(member.Id)))
            .ToList();
        var entryIdsToDelete = entriesToDelete
            .Select(entry => entry.Id)
            .ToHashSet();
        var entriesToKeep = affectedEntries
            .Where(entry => !entryIdsToDelete.Contains(entry.Id))
            .ToList();

        var hasScoreOrResultRows =
            (participantMemberIds.Count > 0 && await _dbContext.ScoreEntryScores.AsNoTracking().AnyAsync(score => participantMemberIds.Contains(score.RegistrationEntryMemberId))) ||
            await _dbContext.CompetitionResultParticipants.AsNoTracking().AnyAsync(row =>
                row.ParticipantId == participant.Id ||
                participantMemberIds.Contains(row.RegistrationEntryMemberId)) ||
            await _dbContext.OverallTahapResults.AsNoTracking().AnyAsync(row => row.ParticipantId == participant.Id) ||
            (affectedEntryIds.Count > 0 && await _dbContext.ScoreEntries.AsNoTracking().AnyAsync(row => affectedEntryIds.Contains(row.RegistrationEntryId))) ||
            (affectedEntryIds.Count > 0 && await _dbContext.CompetitionResultEntries.AsNoTracking().AnyAsync(row => affectedEntryIds.Contains(row.RegistrationEntryId)));

        if (hasScoreOrResultRows)
        {
            return new ResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Participant cannot be deleted because score or result records already exist for this participant or their registration entries."
            };
        }

        var now = DateTime.UtcNow;
        var oldParticipantSnapshot = TeamManagementHelper.ParticipantSnapshot(participant);
        var oldEntrySnapshots = affectedEntries.ToDictionary(entry => entry.Id, entry => TeamManagementHelper.RegistrationEntrySnapshot(entry));

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {

            var participantJudgeConflicts = await _dbContext.JudgeConflicts
                .Where(conflict => conflict.ParticipantId == participant.Id)
                .ToListAsync();
            foreach (var conflict in participantJudgeConflicts)
            {
                conflict.ParticipantId = null;
                conflict.IsActive = false;
                conflict.UpdatedAt = now;
                conflict.UpdatedBy = actorId.Value;
            }

            var submittedDrafts = await _dbContext.TeamParticipantRegistrationDrafts
                .Where(draft => draft.SubmittedParticipantId == participant.Id)
                .ToListAsync();
            foreach (var draft in submittedDrafts)
            {
                draft.SubmittedParticipantId = null;
                draft.UpdatedAt = now;
                draft.UpdatedBy = actorId.Value;
            }

            foreach (var entry in entriesToKeep)
            {
                var remainingMembers = entry.RegistrationEntryMembers
                    .Where(member => !participantMemberIds.Contains(member.Id))
                    .OrderByDescending(member => member.IsActive)
                    .ThenBy(member => member.MemberOrder)
                    .ThenBy(member => member.CreatedAt)
                    .ToList();

                for (var index = 0; index < remainingMembers.Count; index++)
                {
                    remainingMembers[index].MemberOrder = (short)(index + 1);
                    remainingMembers[index].UpdatedAt = now;
                    remainingMembers[index].UpdatedBy = actorId.Value;
                }

                entry.EntryStatus = RegistrationEnums.EntryStatusSubmitted;
                entry.UpdatedAt = now;
                entry.UpdatedBy = actorId.Value;
            }

            var newEntrySnapshots = entriesToKeep.ToDictionary(
                entry => entry.Id,
                entry => TeamManagementHelper.RegistrationEntrySnapshot(entry, participantMemberIds));

            _dbContext.RegistrationEntryMembers.RemoveRange(participantMemberRows);
            _dbContext.RegistrationEntries.RemoveRange(entriesToDelete);
            _dbContext.Participants.Remove(participant);

            await _auditLogService.LogAsync("participants", participant.Id, "DELETE", actorId.Value,
                "Team Manager permanently deleted participant from TeamManagement workspace.",
                oldParticipantSnapshot,
                null);

            foreach (var entry in entriesToKeep)
            {
                await _auditLogService.LogAsync("registration_entries", entry.Id, "UPDATE", actorId.Value,
                    "Team Manager removed participant from registration entry and resubmitted remaining members from TeamManagement workspace.",
                    oldEntrySnapshots[entry.Id],
                    newEntrySnapshots[entry.Id]);
            }

            foreach (var entry in entriesToDelete)
            {
                await _auditLogService.LogAsync("registration_entries", entry.Id, "DELETE", actorId.Value,
                    "Team Manager permanently deleted empty registration entry after participant delete from TeamManagement workspace.",
                    oldEntrySnapshots[entry.Id],
                    null);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new ResultDTO
            {
                IsSuccess = true,
                NoticeTitle = "Participant deleted successfully."
            };
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError(exception, "Participant could not be deleted for team {TeamId}, participant {ParticipantId}.", teamId, participantId);
            return new ResultDTO { IsSuccess = false, NoticeTitle = "Participant could not be deleted." };
        }
    }

    

    public async Task<TeamParticipantDetailsDTO> GetTeamParticipantDetailsAsync(Guid teamId, Guid participantId)
    {
        try
        {
            var participant = await _dbContext.Participants
                .AsNoTracking()
                .Where(row =>
                    row.Id == participantId &&
                    row.TeamId == teamId &&
                    row.IsActive)
                .Select(row => new TeamParticipantDetailsDTO
                {
                    IsSuccess = true,
                    ParticipantId = row.Id,
                    TeamId = row.TeamId,
                    FullName = row.FullNameAsPerId,
                    FullNameGurmukhi = row.FullNameGurmukhi,
                    TahapCode = row.TahapCode,
                    TahapName = row.TahapCodeNavigation.Name,
                    Gender = row.Gender.ToString(),
                    DateOfBirth = row.DateOfBirth,
                    IdType = row.IdType,
                    NricNumber = row.NricNumber,
                    PassportNumber = row.PassportNumber,
                    PassportExpiryDate = row.PassportExpiryDate,
                    MobileNumber = row.MobileNumber,
                    MobileCountryCode = ProfileHelper.ExtractCountryCode(row.MobileNumber),
                    MobileLocalNumber = ProfileHelper.ExtractLocalNumber(row.MobileNumber),
                    Email = row.Email,
                    ParentGuardianName = row.ParentGuardianName,
                    ParentGuardianMobile = row.ParentGuardianMobile,
                    ParentGuardianMobileCountryCode = ProfileHelper.ExtractCountryCode(row.ParentGuardianMobile),
                    ParentGuardianMobileLocalNumber = ProfileHelper.ExtractLocalNumber(row.ParentGuardianMobile)
                })
                .FirstOrDefaultAsync();

            if (participant == null)
            {
                return new TeamParticipantDetailsDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "Participant could not be found or is no longer active."
                };
            }

            var selectedCategoryRows = await _dbContext.RegistrationEntryMembers
                .AsNoTracking()
                .Where(member =>
                    member.ParticipantId == participantId &&
                    member.IsActive &&
                    member.RegistrationEntry.IsActive &&
                    member.RegistrationEntry.TeamId == teamId)
                .Select(member => new
                {
                    member.RegistrationEntry.CompetitionCategoryId,
                    member.RegistrationEntry.CompetitionCategory.EventType
                })
                .ToListAsync();

            participant.IndividualCompetitionCategoryIds = selectedCategoryRows
                .Where(row => string.Equals(row.EventType, RegistrationEnums.EntryTypeIndividual, StringComparison.OrdinalIgnoreCase))
                .Select(row => row.CompetitionCategoryId)
                .Distinct()
                .ToList();

            participant.TeamCompetitionCategoryIds = selectedCategoryRows
                .Where(row => string.Equals(row.EventType, RegistrationEnums.EntryTypeTeam, StringComparison.OrdinalIgnoreCase))
                .Select(row => row.CompetitionCategoryId)
                .Distinct()
                .ToList();

            return participant;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Participant details could not be loaded for team {TeamId}, participant {ParticipantId}.", teamId, participantId);
            return new TeamParticipantDetailsDTO
            {
                IsSuccess = false,
                NoticeTitle = "Participant details could not be loaded."
            };
        }
    }

    public async Task<List<TeamDraftSummaryDTO>> GetDraftApplicationsAsync(Guid? teamId)
    {
        var drafts = await _dbContext.TeamParticipantRegistrationDrafts
            .AsNoTracking()
            .Where(draft =>
                draft.TeamId == teamId &&
                    draft.DraftStatus == RegistrationEnums.DraftStatusDraft &&
                draft.IsActive)
            .OrderBy(draft => draft.TahapCode)
            .ThenByDescending(draft => draft.UpdatedAt ?? draft.CreatedAt)
            .ToListAsync();

        var draftApplications = drafts.Select(draft =>
        {
            var idType = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "IdType");
            var nricNumber = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "NricNumber");
            var passportNumber = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "PassportNumber");
            var mobileCountryCode = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "MobileCountryCode");
            var mobileLocalNumber = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "MobileLocalNumber");
            var parentGuardianMobileCountryCode = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "ParentGuardianMobileCountryCode");
            var parentGuardianMobileLocalNumber = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "ParentGuardianMobileLocalNumber");
            var gender = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "Gender");

            return new TeamDraftSummaryDTO
            {
                DraftId = draft.Id,
                TeamId = draft.TeamId,
                FullName = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "FullNameAsPerId"),
                FullNameGurmukhi = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "FullNameGurmukhi"),
                TahapCode = draft.TahapCode,
                TahapName = ProfileHelper.GetTahapDisplayName(draft.TahapCode),
                Gender = ProfileHelper.GetGenderDisplayName(gender),
                DateOfBirth = JSONHelper.GetDateValueFromJSON(draft.DraftPayloadJson, "DateOfBirth"),
                IdType = string.IsNullOrWhiteSpace(idType) ? ProfileEnums.IdTypeNric : idType,
                IdNumber = string.Equals(idType, ProfileEnums.IdTypePassport, StringComparison.OrdinalIgnoreCase) ? passportNumber : nricNumber,
                MobileNumber = ProfileHelper.BuildPhoneNumber(mobileCountryCode, mobileLocalNumber),
                Email = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "Email"),
                ParentGuardianName = JSONHelper.GetStringValueFromJSON(draft.DraftPayloadJson, "ParentGuardianName"),
                ParentGuardianMobile = ProfileHelper.BuildPhoneNumber(parentGuardianMobileCountryCode, parentGuardianMobileLocalNumber),
                LastSavedAt = draft.UpdatedAt ?? draft.CreatedAt,
                IndividualCompetitionCategoryIds = JSONHelper.GetGuidsFromJSON(draft.SelectedIndividualCompetitionCategoryIdsJson),
                TeamCompetitionCategoryIds = JSONHelper.GetGuidsFromJSON(draft.SelectedTeamCompetitionCategoryIdsJson)
            };
        }).ToList();

        var selectedCategoryIds = draftApplications
            .SelectMany(draft => draft.IndividualCompetitionCategoryIds.Concat(draft.TeamCompetitionCategoryIds))
            .Distinct()
            .ToList();

        if (selectedCategoryIds.Count > 0)
        {
            var categoryLookup = await _dbContext.CompetitionCategories.AsNoTracking()
                .Where(category => selectedCategoryIds.Contains(category.Id))
                .Select(category => new
                {
                    CompetitionCategoryId = category.Id,
                    category.EventType,
                    CompetitionName = category.Competition.NameEnglish,
                    category.SortOrder
                })
                .ToDictionaryAsync(category => category.CompetitionCategoryId);

            foreach (var draft in draftApplications)
            {
                var individualCompetitions = draft.IndividualCompetitionCategoryIds
                    .Select(categoryId => categoryLookup.TryGetValue(categoryId, out var category) ? category : null)
                    .Where(category => category != null)
                    .OrderBy(category => category!.SortOrder)
                    .ThenBy(category => category!.CompetitionName)
                    .Select(category => new DraftApplicationCompetitionDTO
                    {
                        DraftId = draft.DraftId,
                        CompetitionCategoryId = category!.CompetitionCategoryId,
                        EntryType = category.EventType,
                        CompetitionName = category.CompetitionName,
                        EntryStatus = RegistrationEnums.EntryStatusDraft
                    });

                var teamCompetitions = draft.TeamCompetitionCategoryIds
                    .Select(categoryId => categoryLookup.TryGetValue(categoryId, out var category) ? category : null)
                    .Where(category => category != null)
                    .OrderBy(category => category!.SortOrder)
                    .ThenBy(category => category!.CompetitionName)
                    .Select(category => new DraftApplicationCompetitionDTO
                    {
                        DraftId = draft.DraftId,
                        CompetitionCategoryId = category!.CompetitionCategoryId,
                        EntryType = category.EventType,
                        CompetitionName = category.CompetitionName,
                        EntryStatus = RegistrationEnums.EntryStatusDraft
                    });

                draft.RegisteredCompetitions = individualCompetitions
                    .Concat(teamCompetitions)
                    .ToList();
            }
        }

        return draftApplications
            .OrderBy(draft => Array.IndexOf(TahapEnums.TahapCodes, draft.TahapCode))
            .ThenBy(draft => string.IsNullOrWhiteSpace(draft.FullName) ? draft.FullNameGurmukhi : draft.FullName)
            .ToList();
    }

    public async Task<ParticipantDraftDetailsResultDTO> GetParticipantDraftDetailsAsync(Guid teamId, Guid draftId)
    {
        var draft = await _dbContext.TeamParticipantRegistrationDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(existingDraft =>
                existingDraft.Id == draftId &&
                existingDraft.TeamId == teamId &&
                existingDraft.DraftStatus == RegistrationEnums.DraftStatusDraft &&
                existingDraft.IsActive);

        if (draft == null)
        {
            return new ParticipantDraftDetailsResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Draft could not be found or is no longer editable."
            };
        }

        return new ParticipantDraftDetailsResultDTO
        {
            IsSuccess = true,
            Draft = new ParticipantDraftDetailsDTO
            {
                Id = draft.Id,
                TeamId = draft.TeamId,
                TahapCode = draft.TahapCode,
                TahapName = ProfileHelper.GetTahapDisplayName(draft.TahapCode),
                DraftStatus = draft.DraftStatus,
                DraftPayloadJson = draft.DraftPayloadJson,
                IndividualCompetitionCategoryIds = JSONHelper.GetGuidsFromJSON(draft.SelectedIndividualCompetitionCategoryIdsJson),
                TeamCompetitionCategoryIds = JSONHelper.GetGuidsFromJSON(draft.SelectedTeamCompetitionCategoryIdsJson),
                LastSavedAt = draft.UpdatedAt ?? draft.CreatedAt,
                CreatedAt = draft.CreatedAt,
                UpdatedAt = draft.UpdatedAt
            }
        };
    }

    public async Task<ParticipantDraftSaveResultDTO> SaveParticipantDraftAsync(Guid teamId, ParticipantDraftSaveRequest request)
    {
        var actorId = _currentUser.UserId;
        if (!actorId.HasValue)
        {
            return new ParticipantDraftSaveResultDTO { IsSuccess = false, NoticeTitle = "You must be signed in before saving a draft." };
        }

        NormalizeDraftRequest(request);

        if (request.TeamId != teamId)
        {
            return new ParticipantDraftSaveResultDTO { IsSuccess = false, NoticeTitle = "You do not have access to save drafts for this team." };
        }

        var validation = await ValidateDraftRequestAsync(request);
        if (validation.Errors.Count > 0 || !validation.CompetitionYearId.HasValue)
        {
            return new ParticipantDraftSaveResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Draft could not be saved.",
                Errors = validation.Errors
            };
        }

        var now = DateTime.UtcNow;
        var draftPayloadJson = BuildDraftPayloadJson(request);
        var selectedIndividualJson = JsonSerializer.Serialize(request.IndividualCompetitionCategoryIds);
        var selectedTeamJson = JsonSerializer.Serialize(request.TeamCompetitionCategoryIds);

        TeamParticipantRegistrationDraft? draft = null;

        if (request.DraftId.HasValue)
        {
            draft = await _dbContext.TeamParticipantRegistrationDrafts
                .FirstOrDefaultAsync(existingDraft =>
                    existingDraft.Id == request.DraftId.Value &&
                    existingDraft.TeamId == teamId &&
                    existingDraft.DraftStatus == RegistrationEnums.DraftStatusDraft &&
                    existingDraft.IsActive);

            if (draft == null)
            {
                return new ParticipantDraftSaveResultDTO { IsSuccess = false, NoticeTitle = "Draft could not be found or is no longer editable." };
            }
        }

        if (draft == null)
        {
            draft = new TeamParticipantRegistrationDraft
            {
                Id = Guid.NewGuid(),
                CompetitionYearId = validation.CompetitionYearId.Value,
                TeamId = teamId,
                TahapCode = request.TahapCode,
                DraftStatus = RegistrationEnums.DraftStatusDraft,
                DraftPayloadJson = draftPayloadJson,
                SelectedIndividualCompetitionCategoryIdsJson = selectedIndividualJson,
                SelectedTeamCompetitionCategoryIdsJson = selectedTeamJson,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = actorId.Value
            };

            _dbContext.TeamParticipantRegistrationDrafts.Add(draft);

            await _auditLogService.LogAsync(
                "team_participant_registration_drafts",
                draft.Id,
                "CREATE",
                actorId.Value,
                "Team Manager saved participant registration draft from TeamManagement workspace.",
                null,
                DraftSnapshot(draft));
        }
        else
        {
            var oldSnapshot = DraftSnapshot(draft);

            draft.CompetitionYearId = validation.CompetitionYearId.Value;
            draft.TahapCode = request.TahapCode;
            draft.DraftPayloadJson = draftPayloadJson;
            draft.SelectedIndividualCompetitionCategoryIdsJson = selectedIndividualJson;
            draft.SelectedTeamCompetitionCategoryIdsJson = selectedTeamJson;
            draft.UpdatedAt = now;
            draft.UpdatedBy = actorId.Value;

            await _auditLogService.LogAsync(
                "team_participant_registration_drafts",
                draft.Id,
                "UPDATE",
                actorId.Value,
                "Team Manager updated participant registration draft from TeamManagement workspace.",
                oldSnapshot,
                DraftSnapshot(draft));
        }

        await _dbContext.SaveChangesAsync();

        return new ParticipantDraftSaveResultDTO
        {
            IsSuccess = true,
            NoticeTitle = "Draft saved successfully.",
            DraftId = draft.Id
        };
    }

    public async Task<ResultDTO> DeleteParticipantDraftAsync(Guid teamId, Guid draftId)
    {
        var actorId = _currentUser.UserId;
        if (!actorId.HasValue)
        {
            return new ResultDTO { IsSuccess = false, NoticeTitle = "You must be signed in before deleting a draft." };
        }

        var draft = await _dbContext.TeamParticipantRegistrationDrafts
            .FirstOrDefaultAsync(existingDraft =>
                existingDraft.Id == draftId &&
                existingDraft.TeamId == teamId &&
                existingDraft.DraftStatus == RegistrationEnums.DraftStatusDraft &&
                existingDraft.IsActive);

        if (draft == null)
        {
            return new ResultDTO { IsSuccess = false, NoticeTitle = "Draft could not be found or is no longer editable." };
        }

        var oldSnapshot = DraftSnapshot(draft);

        _dbContext.TeamParticipantRegistrationDrafts.Remove(draft);

        await _auditLogService.LogAsync(
            entityName: "team_participant_registration_drafts",
            entityId: draft.Id,
            actionName: "DELETE",
            changedBy: actorId.Value,
            changeReason: "Team Manager deleted participant registration draft from TeamManagement workspace.",
            oldValues: oldSnapshot,
            newValues: null);

        await _dbContext.SaveChangesAsync();

        return new ResultDTO
        {
            IsSuccess = true,
            NoticeTitle = "Draft deleted successfully."
        };
    }

    private static void NormalizeSubmitRequest(TeamParticipantSubmitRequest request)
    {
        request.FullNameAsPerId = string.IsNullOrWhiteSpace(request.FullNameAsPerId) ? string.Empty : request.FullNameAsPerId.Trim();
        request.FullNameGurmukhi = string.IsNullOrWhiteSpace(request.FullNameGurmukhi) ? string.Empty : request.FullNameGurmukhi.Trim();
        request.IdType = string.IsNullOrWhiteSpace(request.IdType) ? string.Empty : request.IdType.Trim().ToUpperInvariant();
        request.NricNumber = string.IsNullOrWhiteSpace(request.NricNumber) ? null : new string(request.NricNumber.Where(char.IsDigit).ToArray());
        request.PassportNumber = string.IsNullOrWhiteSpace(request.PassportNumber) ? null : request.PassportNumber.Trim().ToUpperInvariant();
        request.MobileCountryCode = string.IsNullOrWhiteSpace(request.MobileCountryCode) ? "+60" : request.MobileCountryCode.Trim();
        request.MobileLocalNumber = string.IsNullOrWhiteSpace(request.MobileLocalNumber) ? null : new string(request.MobileLocalNumber.Where(char.IsDigit).ToArray());
        request.MobileNumber = string.IsNullOrWhiteSpace(request.MobileLocalNumber) ? null : $"{request.MobileCountryCode}{request.MobileLocalNumber}".Replace(" ", "");
        request.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        request.ParentGuardianName = string.IsNullOrWhiteSpace(request.ParentGuardianName) ? string.Empty : request.ParentGuardianName.Trim();
        request.ParentGuardianMobileCountryCode = string.IsNullOrWhiteSpace(request.ParentGuardianMobileCountryCode) ? "+60" : request.ParentGuardianMobileCountryCode.Trim();
        request.ParentGuardianMobileLocalNumber = string.IsNullOrWhiteSpace(request.ParentGuardianMobileLocalNumber) ? null : new string(request.ParentGuardianMobileLocalNumber.Where(char.IsDigit).ToArray());
        request.ParentGuardianMobile = $"{request.ParentGuardianMobileCountryCode}{request.ParentGuardianMobileLocalNumber}".Replace(" ", "");
        request.Gender = string.IsNullOrWhiteSpace(request.Gender) ? string.Empty : request.Gender.Trim().ToUpperInvariant();
        request.TahapCode = string.IsNullOrWhiteSpace(request.TahapCode) ? string.Empty : request.TahapCode.Trim().ToUpperInvariant();
        request.IndividualCompetitionCategoryIds = request.IndividualCompetitionCategoryIds.Where(id => id != Guid.Empty).Distinct().ToList();
        request.TeamCompetitionCategoryIds = request.TeamCompetitionCategoryIds.Where(id => id != Guid.Empty).Distinct().ToList();
    }

    private static void NormalizeDraftRequest(ParticipantDraftSaveRequest request)
    {
        request.FullNameAsPerId = string.IsNullOrWhiteSpace(request.FullNameAsPerId) ? string.Empty : request.FullNameAsPerId.Trim();
        request.FullNameGurmukhi = string.IsNullOrWhiteSpace(request.FullNameGurmukhi) ? string.Empty : request.FullNameGurmukhi.Trim();
        request.IdType = string.IsNullOrWhiteSpace(request.IdType) ? string.Empty : request.IdType.Trim().ToUpperInvariant();
        request.NricNumber = string.IsNullOrWhiteSpace(request.NricNumber) ? null : new string(request.NricNumber.Where(char.IsDigit).ToArray());
        request.PassportNumber = string.IsNullOrWhiteSpace(request.PassportNumber) ? null : request.PassportNumber.Trim().ToUpperInvariant();
        request.MobileCountryCode = string.IsNullOrWhiteSpace(request.MobileCountryCode) ? "+60" : request.MobileCountryCode.Trim();
        request.MobileLocalNumber = string.IsNullOrWhiteSpace(request.MobileLocalNumber) ? null : new string(request.MobileLocalNumber.Where(char.IsDigit).ToArray());
        request.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        request.ParentGuardianName = string.IsNullOrWhiteSpace(request.ParentGuardianName) ? string.Empty : request.ParentGuardianName.Trim();
        request.ParentGuardianMobileCountryCode = string.IsNullOrWhiteSpace(request.ParentGuardianMobileCountryCode) ? "+60" : request.ParentGuardianMobileCountryCode.Trim();
        request.ParentGuardianMobileLocalNumber = string.IsNullOrWhiteSpace(request.ParentGuardianMobileLocalNumber) ? null : new string(request.ParentGuardianMobileLocalNumber.Where(char.IsDigit).ToArray());
        request.Gender = string.IsNullOrWhiteSpace(request.Gender) ? string.Empty : request.Gender.Trim().ToUpperInvariant();
        request.TahapCode = string.IsNullOrWhiteSpace(request.TahapCode) ? string.Empty : request.TahapCode.Trim().ToUpperInvariant();
        request.IndividualCompetitionCategoryIds = request.IndividualCompetitionCategoryIds.Where(id => id != Guid.Empty).Distinct().ToList();
        request.TeamCompetitionCategoryIds = request.TeamCompetitionCategoryIds.Where(id => id != Guid.Empty).Distinct().ToList();
    }

    private async Task<ParticipantDraftValidationResult> ValidateDraftRequestAsync(ParticipantDraftSaveRequest request)
    {
        var result = new ParticipantDraftValidationResult();
        var selectedCategoryIds = request.IndividualCompetitionCategoryIds
            .Concat(request.TeamCompetitionCategoryIds)
            .Distinct()
            .ToList();

        if (!TahapEnums.TahapCodes.Contains(request.TahapCode))
        {
            result.Errors.Add("Selected Tahap is not valid.");
        }

        if (request.IndividualCompetitionCategoryIds.Count > 2)
        {
            result.Errors.Add("A draft can only include up to 2 individual competitions from this modal.");
        }

        if (request.TeamCompetitionCategoryIds.Count > 2)
        {
            result.Errors.Add("A draft can only include up to 2 team competitions from this modal.");
        }

        ValidateOptionalEmail(request.Email, result.Errors);

        var categories = selectedCategoryIds.Count == 0
            ? []
            : await _dbContext.CompetitionCategories.AsNoTracking()
                .Include(category => category.Competition).ThenInclude(competition => competition.CompetitionYear)
                .Where(category =>
                    selectedCategoryIds.Contains(category.Id) &&
                    category.IsActive &&
                    category.Competition.IsActive &&
                    category.Competition.CompetitionYear.IsActive)
                .ToListAsync();

        if (categories.Count != selectedCategoryIds.Count)
        {
            result.Errors.Add("One or more selected competitions are not valid or inactive.");
        }

        foreach (var category in categories)
        {
            if (!TeamManagementHelper.CategoryAppliesToTahap(category.CategoryCode, request.TahapCode))
            {
                result.Errors.Add($"{category.Competition.NameEnglish} / {TeamManagementHelper.GetCategoryShortName(category.CategoryCode)} does not apply to the selected Tahap.");
            }
        }

        result.CompetitionYearId = await ResolveCompetitionYearIdAsync(categories, result.Errors);

        return result;
    }

    private async Task<TeamParticipantSubmitValidationResult> ValidateSubmitRequestAsync(
        TeamParticipantSubmitRequest request,
        Guid accessibleTeamId,
        Guid? editedParticipantId = null,
        IReadOnlyCollection<Guid>? existingIndividualCategoryIds = null,
        IReadOnlyCollection<Guid>? existingTeamCategoryIds = null)
    {
        var result = new TeamParticipantSubmitValidationResult();
        var selectedCategoryIds = request.IndividualCompetitionCategoryIds.Concat(request.TeamCompetitionCategoryIds).Distinct().ToList();

        if (request.TeamId != accessibleTeamId) result.Errors.Add("Selected team is not valid for your account.");
        if (string.IsNullOrWhiteSpace(request.FullNameAsPerId)) result.Errors.Add("Full name as per ID is required.");
        else if (request.FullNameAsPerId.Length > 200) result.Errors.Add("Full name as per ID must not be more than 200 characters.");
        if (string.IsNullOrWhiteSpace(request.FullNameGurmukhi)) result.Errors.Add("Full name in Gurmukhi is required.");
        else if (request.FullNameGurmukhi.Length > 200) result.Errors.Add("Full name in Gurmukhi must not be more than 200 characters.");
        if (!TahapEnums.TahapCodes.Contains(request.TahapCode)) result.Errors.Add("Selected Tahap is not valid.");
        if (request.IndividualCompetitionCategoryIds.Count > 2) result.Errors.Add("A participant can only be assigned up to 2 individual competitions from this modal.");
        if (request.TeamCompetitionCategoryIds.Count > 2) result.Errors.Add("A participant can only be assigned up to 2 team competitions from this modal.");

        if (request.IndividualCompetitionCategoryIds.Count + request.TeamCompetitionCategoryIds.Count == 0)
        {
            result.Errors.Add("Please select at least one individual or team competition before submitting for approval.");
        }

        ValidateIdFields(request, result.Errors);
        ValidateMobileFields(request, result.Errors);
        ValidateOptionalEmail(request, result.Errors);

        var categories = selectedCategoryIds.Count == 0
            ? []
            : await _dbContext.CompetitionCategories.AsNoTracking()
                .Include(category => category.Competition).ThenInclude(competition => competition.CompetitionYear)
                .Where(category => selectedCategoryIds.Contains(category.Id) && category.IsActive && category.Competition.IsActive && category.Competition.CompetitionYear.IsActive)
                .ToListAsync();

        if (categories.Count != selectedCategoryIds.Count)
        {
            result.Errors.Add("One or more selected competitions are not valid or inactive.");
        }

        foreach (var category in categories)
        {
            if (!TeamManagementHelper.CategoryAppliesToTahap(category.CategoryCode, request.TahapCode))
            {
                result.Errors.Add($"{category.Competition.NameEnglish} / {category.CategoryCode} is not valid for {TeamManagementHelper.GetCategoryShortName(request.TahapCode)}.");
            }
        }

        foreach (var categoryId in request.IndividualCompetitionCategoryIds)
        {
            var category = categories.FirstOrDefault(row => row.Id == categoryId);
            if (category != null && category.EventType != RegistrationEnums.EntryTypeIndividual) result.Errors.Add("One or more selected individual competitions are not individual events.");
        }

        foreach (var categoryId in request.TeamCompetitionCategoryIds)
        {
            var category = categories.FirstOrDefault(row => row.Id == categoryId);
            if (category != null && category.EventType != RegistrationEnums.EntryTypeTeam) result.Errors.Add("One or more selected team competitions are not team events.");
        }

        result.CompetitionYearId = await ResolveCompetitionYearIdAsync(categories, result.Errors);

        if (result.CompetitionYearId.HasValue)
        {
            if (!string.IsNullOrWhiteSpace(request.NricNumber) && await _dbContext.Participants.AsNoTracking().AnyAsync(participant =>
                    participant.CompetitionYearId == result.CompetitionYearId.Value &&
                    participant.NricNumber == request.NricNumber &&
                    (!editedParticipantId.HasValue || participant.Id != editedParticipantId.Value)))
            {
                result.Errors.Add("A participant with this NRIC already exists for the selected competition year.");
            }

            if (!string.IsNullOrWhiteSpace(request.PassportNumber) && await _dbContext.Participants.AsNoTracking().AnyAsync(participant =>
                    participant.CompetitionYearId == result.CompetitionYearId.Value &&
                    participant.PassportNumber == request.PassportNumber &&
                    (!editedParticipantId.HasValue || participant.Id != editedParticipantId.Value)))
            {
                result.Errors.Add("A participant with this passport number already exists for the selected competition year.");
            }
        }

        result.SelectedIndividualCategories = categories
            .Where(category => request.IndividualCompetitionCategoryIds.Contains(category.Id) && category.EventType == RegistrationEnums.EntryTypeIndividual)
            .ToList();
        result.SelectedTeamCategories = categories
            .Where(category => request.TeamCompetitionCategoryIds.Contains(category.Id) && category.EventType == RegistrationEnums.EntryTypeTeam)
            .ToList();

        await ValidateSelectedCompetitionLimitsAsync(request, accessibleTeamId, result, editedParticipantId, existingIndividualCategoryIds, existingTeamCategoryIds);

        return result;
    }

    private static void ValidateIdFields(TeamParticipantSubmitRequest request, List<string> errors)
    {
        if (!ProfileEnums.IdTypes.Contains(request.IdType))
        {
            errors.Add("ID type must be NRIC or Passport.");
            return;
        }

        if (request.IdType == ProfileEnums.IdTypeNric)
        {
            if (string.IsNullOrWhiteSpace(request.NricNumber)) errors.Add("NRIC number is required.");
            else if (request.NricNumber.Length != 12 || !request.NricNumber.All(char.IsDigit)) errors.Add("NRIC number must be exactly 12 digits.");
            ApplyNricDerivedFields(request, errors);
        }
        else if (request.IdType == ProfileEnums.IdTypePassport)
        {
            if (string.IsNullOrWhiteSpace(request.PassportNumber)) errors.Add("Passport number is required.");
            if (!request.PassportExpiryDate.HasValue) errors.Add("Passport expiry date is required.");
            if (!ProfileEnums.Genders.Contains(request.Gender)) errors.Add("Gender must be Male or Female.");
            if (!request.DateOfBirth.HasValue) errors.Add("Date of birth is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.PassportNumber) && request.PassportNumber.Length > 20)
        {
            errors.Add("Passport number must not be more than 20 characters.");
        }
    }

    private static void ApplyNricDerivedFields(TeamParticipantSubmitRequest request, List<string> errors)
    {
        if (request.NricNumber == null || request.NricNumber.Length != 12 || !request.NricNumber.All(char.IsDigit)) return;

        request.Gender = int.Parse(request.NricNumber[^1].ToString(), CultureInfo.InvariantCulture) % 2 == 1 ? ProfileEnums.GenderMale : ProfileEnums.GenderFemale;
        var yy = int.Parse(request.NricNumber[..2], CultureInfo.InvariantCulture);
        var mm = int.Parse(request.NricNumber.Substring(2, 2), CultureInfo.InvariantCulture);
        var dd = int.Parse(request.NricNumber.Substring(4, 2), CultureInfo.InvariantCulture);
        var currentYearTwoDigits = DateTime.UtcNow.Year % 100;
        var yyyy = yy <= currentYearTwoDigits ? 2000 + yy : 1900 + yy;

        try
        {
            request.DateOfBirth = new DateOnly(yyyy, mm, dd);
        }
        catch
        {
            errors.Add("NRIC birth date is invalid.");
        }
    }

    private static void ValidateMobileFields(TeamParticipantSubmitRequest request, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(request.MobileLocalNumber) && request.MobileLocalNumber.Length < 6) errors.Add("Mobile number is too short.");
        if (request.MobileNumber?.Length > 30) errors.Add("Mobile number must not be more than 30 characters.");

        if (string.IsNullOrWhiteSpace(request.ParentGuardianMobileLocalNumber)) errors.Add("Parent / guardian mobile is required.");
        else if (request.ParentGuardianMobileLocalNumber.Length < 6) errors.Add("Parent / guardian mobile is too short.");
        if (request.ParentGuardianMobile?.Length > 30) errors.Add("Parent / guardian mobile must not be more than 30 characters.");

        if (string.IsNullOrWhiteSpace(request.ParentGuardianName)) errors.Add("Parent / guardian name is required.");
        else if (request.ParentGuardianName.Length > 200) errors.Add("Parent / guardian name must not be more than 200 characters.");
    }

    private static void ValidateOptionalEmail(TeamParticipantSubmitRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return;
        if (request.Email.Length > 256)
        {
            errors.Add("Email must not be more than 256 characters.");
            return;
        }

        try
        {
            var address = new MailAddress(request.Email);
            if (!string.Equals(address.Address, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Please enter a valid email address.");
            }
        }
        catch
        {
            errors.Add("Please enter a valid email address.");
        }
    }

    private static void ValidateOptionalEmail(string? email, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        if (email.Length > 256)
        {
            errors.Add("Email must not be more than 256 characters.");
            return;
        }

        try
        {
            var address = new MailAddress(email);
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Please enter a valid email address.");
            }
        }
        catch
        {
            errors.Add("Please enter a valid email address.");
        }
    }

    private static string BuildDraftPayloadJson(ParticipantDraftSaveRequest request)
    {
        return JsonSerializer.Serialize(new
        {
            request.FullNameAsPerId,
            request.FullNameGurmukhi,
            request.IdType,
            request.NricNumber,
            request.PassportNumber,
            request.PassportExpiryDate,
            request.MobileCountryCode,
            request.MobileLocalNumber,
            request.Email,
            request.ParentGuardianName,
            request.ParentGuardianMobileCountryCode,
            request.ParentGuardianMobileLocalNumber,
            request.Gender,
            request.DateOfBirth
        });
    }

    private async Task<Guid?> ResolveCompetitionYearIdAsync(List<CompetitionCategory> selectedCategories, List<string> errors)
    {
        if (selectedCategories.Count > 0)
        {
            var yearIds = selectedCategories.Select(category => category.Competition.CompetitionYearId).Distinct().ToList();
            if (yearIds.Count > 1)
            {
                errors.Add("Selected competitions must belong to the same competition year.");
                return null;
            }

            return yearIds[0];
        }

        var activeCompetitionYear = await _dbContext.CompetitionYears.AsNoTracking()
            .Where(year => year.IsActive)
            .OrderByDescending(year => year.YearNo)
            .Select(year => (Guid?)year.Id)
            .FirstOrDefaultAsync();

        if (!activeCompetitionYear.HasValue)
        {
            errors.Add("No active competition year was found.");
        }

        return activeCompetitionYear;
    }

    private async Task ValidateSelectedCompetitionLimitsAsync(
        TeamParticipantSubmitRequest request,
        Guid teamId,
        TeamParticipantSubmitValidationResult result,
        Guid? editedParticipantId = null,
        IReadOnlyCollection<Guid>? existingIndividualCategoryIds = null,
        IReadOnlyCollection<Guid>? existingTeamCategoryIds = null)
    {
        var existingIndividualCategoryIdSet = existingIndividualCategoryIds == null
            ? new HashSet<Guid>()
            : existingIndividualCategoryIds.ToHashSet();
        var existingTeamCategoryIdSet = existingTeamCategoryIds == null
            ? new HashSet<Guid>()
            : existingTeamCategoryIds.ToHashSet();

        foreach (var category in result.SelectedIndividualCategories)
        {
            if (existingIndividualCategoryIdSet.Contains(category.Id)) continue;

            var duplicateParticipantEntry = await _dbContext.RegistrationEntryMembers.AsNoTracking().AnyAsync(member =>
                member.IsActive &&
                member.Participant.IsActive &&
                member.RegistrationEntry.IsActive &&
                member.RegistrationEntry.CompetitionCategoryId == category.Id &&
                (!editedParticipantId.HasValue || member.ParticipantId != editedParticipantId.Value) &&
                member.Participant.NricNumber != null &&
                request.NricNumber != null &&
                member.Participant.NricNumber == request.NricNumber);

            if (duplicateParticipantEntry) result.Errors.Add($"This participant is already registered for {category.Competition.NameEnglish} / {TeamManagementHelper.GetCategoryShortName(category.CategoryCode)}.");

            if (category.IndividualEventMaxParticipants.HasValue)
            {
                var existingCount = await _dbContext.RegistrationEntryMembers.AsNoTracking().CountAsync(member =>
                    member.IsActive &&
                    member.Participant.IsActive &&
                    member.RegistrationEntry.IsActive &&
                    member.RegistrationEntry.TeamId == teamId &&
                    member.RegistrationEntry.CompetitionCategoryId == category.Id &&
                    (!editedParticipantId.HasValue || member.ParticipantId != editedParticipantId.Value) &&
                    member.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeIndividual);

                if (existingCount + 1 > category.IndividualEventMaxParticipants.Value)
                {
                    result.Errors.Add($"{category.Competition.NameEnglish} / {TeamManagementHelper.GetCategoryShortName(category.CategoryCode)} already has the maximum {category.IndividualEventMaxParticipants.Value} individual participant(s) for this team.");
                }
            }
        }

        foreach (var category in result.SelectedTeamCategories)
        {
            if (existingTeamCategoryIdSet.Contains(category.Id)) continue;

            var requiredTeamSize = category.TeamEventTeamSize ?? 0;
            if (requiredTeamSize <= 0)
            {
                result.Errors.Add($"{category.Competition.NameEnglish} / {TeamManagementHelper.GetCategoryShortName(category.CategoryCode)} does not have a valid team size configured.");
                continue;
            }

            var existingMemberCount = await _dbContext.RegistrationEntryMembers.AsNoTracking().CountAsync(member =>
                member.IsActive &&
                member.Participant.IsActive &&
                member.RegistrationEntry.IsActive &&
                member.RegistrationEntry.TeamId == teamId &&
                member.RegistrationEntry.CompetitionCategoryId == category.Id &&
                (!editedParticipantId.HasValue || member.ParticipantId != editedParticipantId.Value) &&
                member.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeTeam);

            if (existingMemberCount + 1 > requiredTeamSize)
            {
                result.Errors.Add($"{category.Competition.NameEnglish} / {TeamManagementHelper.GetCategoryShortName(category.CategoryCode)} would exceed the configured team size of {requiredTeamSize}.");
            }

            if (category.TeamEventMaxEntriesPerTeam.HasValue)
            {
                var existingTeamEntryCount = await _dbContext.RegistrationEntries.AsNoTracking().CountAsync(entry =>
                    entry.IsActive &&
                    entry.TeamId == teamId &&
                    entry.CompetitionCategoryId == category.Id &&
                    entry.EntryType == RegistrationEnums.EntryTypeTeam);

                if (existingTeamEntryCount == 0) continue;

                var hasAvailableEntry = await _dbContext.RegistrationEntries.AsNoTracking().AnyAsync(entry =>
                    entry.IsActive &&
                    entry.TeamId == teamId &&
                    entry.CompetitionCategoryId == category.Id &&
                    entry.EntryType == RegistrationEnums.EntryTypeTeam &&
                    entry.RegistrationEntryMembers.Count(member => member.IsActive && member.Participant.IsActive && (!editedParticipantId.HasValue || member.ParticipantId != editedParticipantId.Value)) < requiredTeamSize);

                if (!hasAvailableEntry && existingTeamEntryCount + 1 > category.TeamEventMaxEntriesPerTeam.Value)
                {
                    result.Errors.Add($"{category.Competition.NameEnglish} / {TeamManagementHelper.GetCategoryShortName(category.CategoryCode)} already has the maximum {category.TeamEventMaxEntriesPerTeam.Value} team entry/entries for this team.");
                }
            }
        }
    }

    private async Task<RegistrationEntry?> LoadBestExistingTeamEntryAsync(CompetitionCategory category, Guid teamId)
    {
        var requiredTeamSize = category.TeamEventTeamSize ?? 0;
        if (requiredTeamSize <= 0) return null;

        var entries = await _dbContext.RegistrationEntries
            .Include(entry => entry.RegistrationEntryMembers)
                .ThenInclude(member => member.Participant)
            .Where(entry =>
                entry.IsActive &&
                entry.TeamId == teamId &&
                entry.CompetitionCategoryId == category.Id &&
                entry.EntryType == RegistrationEnums.EntryTypeTeam)
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync();

        return entries.FirstOrDefault(entry => entry.RegistrationEntryMembers.Count(member => member.IsActive && member.Participant.IsActive) < requiredTeamSize);
    }

    private static RegistrationEntry CreateRegistrationEntry(CompetitionCategory category, Guid teamId, Guid participantId, Guid actorId, DateTime now, string entryStatus)
    {
        var entry = new RegistrationEntry
        {
            Id = Guid.NewGuid(),
            CompetitionCategoryId = category.Id,
            TeamId = teamId,
            EntryType = category.EventType,
            EntryName = null,
            EntryStatus = entryStatus,
            ApprovedBy = null,
            RejectedReason = null,
            Notes = null,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actorId
        };

        entry.RegistrationEntryMembers.Add(new RegistrationEntryMember
        {
            Id = Guid.NewGuid(),
            RegistrationEntryId = entry.Id,
            ParticipantId = participantId,
            MemberOrder = 1,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actorId
        });

        return entry;
    }

    private static void CaptureRegistrationEntrySnapshot(Dictionary<Guid, object> snapshots, RegistrationEntry entry)
    {
        if (snapshots.ContainsKey(entry.Id)) return;
        snapshots[entry.Id] = TeamManagementHelper.RegistrationEntrySnapshot(entry);
    }

    private static bool TryBuildParticipantUniqueConstraintResult(
        DbUpdateException exception,
        string message,
        out TeamParticipantSubmitResultDTO result)
    {
        result = new TeamParticipantSubmitResultDTO();

        var exceptionText = exception.ToString();
        var constraintName = exception.InnerException?.GetType().GetProperty("ConstraintName")?.GetValue(exception.InnerException) as string;

        if (constraintName == "ux_participants_year_nric" || exceptionText.Contains("ux_participants_year_nric", StringComparison.OrdinalIgnoreCase))
        {
            result = new TeamParticipantSubmitResultDTO
            {
                IsSuccess = false,
                NoticeTitle = message,
                Errors = ["A participant with this NRIC already exists for the selected competition year."]
            };
            return true;
        }

        if (constraintName == "ux_participants_year_passport" || exceptionText.Contains("ux_participants_year_passport", StringComparison.OrdinalIgnoreCase))
        {
            result = new TeamParticipantSubmitResultDTO
            {
                IsSuccess = false,
                NoticeTitle = message,
                Errors = ["A participant with this passport number already exists for the selected competition year."]
            };
            return true;
        }

        return false;
    }

    private static object DraftSnapshot(TeamParticipantRegistrationDraft draft) => new
    {
        draft.Id,
        draft.CompetitionYearId,
        draft.TeamId,
        draft.TahapCode,
        draft.DraftStatus,
        draft.DraftPayloadJson,
        draft.SelectedIndividualCompetitionCategoryIdsJson,
        draft.SelectedTeamCompetitionCategoryIdsJson,
        draft.SubmittedParticipantId,
        draft.SubmittedAt,
        draft.SubmittedBy,
        draft.CancelledAt,
        draft.CancelledBy,
        draft.CancelledReason,
        draft.IsActive,
        draft.CreatedAt,
        draft.CreatedBy,
        draft.UpdatedAt,
        draft.UpdatedBy
    };

    private sealed class TeamParticipantSubmitValidationResult
    {
        public Guid? CompetitionYearId { get; set; }
        public List<string> Errors { get; set; } = [];
        public List<CompetitionCategory> SelectedIndividualCategories { get; set; } = [];
        public List<CompetitionCategory> SelectedTeamCategories { get; set; } = [];
        public bool IsValid => Errors.Count == 0 && CompetitionYearId.HasValue;
    }

    private sealed class ParticipantDraftValidationResult
    {
        public Guid? CompetitionYearId { get; set; }
        public List<string> Errors { get; set; } = [];
    }

}
