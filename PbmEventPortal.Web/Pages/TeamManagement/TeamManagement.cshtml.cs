using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.DTOs.Common;
using PbmEventPortal.Application.DTOs.TeamManagement;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Helpers.Common;
using PbmEventPortal.Application.Helpers.TeamManagement;
using PbmEventPortal.Application.Services.TeamManagement;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using System.Security.Claims;

namespace PbmEventPortal.Web.Pages.TeamManagement;

public class TeamManagementModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly ILogger<TeamManagementModel> _logger;
    private readonly ITeamManagementService _teamManagementService;

    public TeamManagementModel(PbmDbContext dbContext, ILogger<TeamManagementModel> logger, ITeamManagementService teamManagementService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _teamManagementService = teamManagementService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? TeamId { get; set; }

    public bool HasAnyAdminRole { get; private set; }
    public bool HasTeamManagerRole { get; private set; }
    public bool HasAssignedTeam { get; private set; }
    public string NoticeTitle { get; private set; } = string.Empty;
    public string NoticeDetails { get; private set; } = string.Empty;
    public TeamSummaryDTO? TeamSummary { get; private set; }
    public List<SelectListItem> Teams { get; private set; } = [];
    public List<TeamManagerSummaryDTO> TeamManagers { get; private set; } = [];
    public List<RegistrationStatusDTO> RegistrationStatusSummary { get; private set; } = [];
    public List<TeamDraftSummaryDTO> Drafts { get; private set; } = [];
    public List<TeamParticipantSummaryDTO> Participants { get; private set; } = [];

    #region Participant CRUD
    public async Task<IActionResult> OnGetAsync(int? year = null)
    {
        // to-do, filter results by year (useful for admin access to view past year data)
        year ??= DateTime.Now.Year;
        try
        {
            var access = await _teamManagementService.GetPageAccessAsync(TeamId);
            if (!access.ValidationResult.IsSuccess)
            {
                return Challenge();
            }
            HasAnyAdminRole = access.UserAccess.UserHasAnyAdminRole;
            HasTeamManagerRole = access.HasTeamManagerRole;
            HasAssignedTeam = access.HasAssignedTeam;
            TeamId = access.SelectedTeamId;
            SetNoticeMessages(access.ValidationResult);

            if (HasAnyAdminRole)
            {
                var teams = await _teamManagementService.GetTeamsAsync();

                Teams = teams
                    .Select(t => new SelectListItem
                    {
                        Value = t.TeamId.ToString(),
                        Text = t.TeamName,
                        Selected = TeamId == t.TeamId
                    })
                    .ToList();
            }

            if (GuidHelper.IsValidGuid(TeamId))
            {
                var result = await _teamManagementService.GetTeamManagementWorkspaceAsync(TeamId);
                if (!result.IsSuccess)
                {
                    return JsonFailure(result.NoticeDetails, StatusCodes.Status500InternalServerError);
                }

                var workspace = ((ResultDTO<TeamManagementWorkspaceDTO>)result).Data!;
                TeamSummary = workspace.TeamSummary ?? null;
                TeamManagers = workspace.TeamManagers;
                RegistrationStatusSummary = workspace.RegistrationStatuses;
                Participants = workspace.Participants;
                Drafts = workspace.Drafts;
            }
            _logger.LogInformation(NoticeEnums.TeamManagementWorkspaceLoadedTitle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, NoticeEnums.TeamManagementWorkspaceFailedToLoadTitle);
            TeamSummary = null;
            NoticeTitle = NoticeEnums.TeamManagementWorkspaceFailedToLoadTitle;
            NoticeDetails = NoticeEnums.TeamManagementWorkspaceFailedToLoadDetails;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetParticipantAsync(Guid? teamId, Guid? participantId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Challenge();

            if (!teamId.HasValue || teamId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Team is required before viewing a participant." }, StatusCodes.Status400BadRequest);
            }

            if (!participantId.HasValue || participantId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Participant is required before viewing." }, StatusCodes.Status400BadRequest);
            }

            var accessibleTeamId = await _teamManagementService.GetAccessibleTeamIdAsync(teamId);
            if (!accessibleTeamId.HasValue || accessibleTeamId.Value != teamId.Value)
            {
                return JsonResultDTO(new ResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "You do not have access to view participants for this team."
                }, StatusCodes.Status403Forbidden);
            }

            var participant = await _teamManagementService.GetTeamParticipantDetailsAsync(accessibleTeamId.Value, participantId.Value);
            return JsonResultDTO(participant, participant.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Participant details could not be loaded for team {TeamId}, participant {ParticipantId}.", teamId, participantId);
            return JsonResultDTO(new TeamParticipantDetailsDTO
            {
                IsSuccess = false,
                NoticeTitle = "Participant details could not be loaded."
            }, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<IActionResult> OnPostSubmitParticipantAsync([FromBody] TeamParticipantSubmitRequest? request)
    {
        try
        {
            var actorId = GetCurrentUserId();
            if (actorId == null) return Challenge();

            if (request == null)
            {
                return JsonResultDTO(new TeamParticipantSubmitResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "Participant details are required."
                }, StatusCodes.Status400BadRequest);
            }

            var accessibleTeamId = await _teamManagementService.GetAccessibleTeamIdAsync(request.TeamId);
            if (!accessibleTeamId.HasValue || accessibleTeamId.Value != request.TeamId)
            {
                return JsonResultDTO(new TeamParticipantSubmitResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "You do not have access to submit participants for this team."
                }, StatusCodes.Status403Forbidden);
            }

            var isEditMode = request.ParticipantId.HasValue && request.ParticipantId.Value != Guid.Empty;
            var result = isEditMode
                ? await _teamManagementService.EditTeamParticipantAsync(accessibleTeamId.Value, request)
                : await _teamManagementService.CreateTeamParticipantAsync(accessibleTeamId.Value, request);

            return JsonResultDTO(result, result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var isEditMode = request?.ParticipantId.HasValue == true && request.ParticipantId.Value != Guid.Empty;
            var message = isEditMode ? "Participant could not be updated." : "Participant could not be added.";

            _logger.LogError(exception, "Participant submit failed unexpectedly. EditMode: {IsEditMode}, TeamId: {TeamId}, ParticipantId: {ParticipantId}.", isEditMode, request?.TeamId, request?.ParticipantId);
            return JsonResultDTO(new TeamParticipantSubmitResultDTO
            {
                IsSuccess = false,
                NoticeTitle = message
            }, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<IActionResult> OnDeleteParticipantAsync(Guid? teamId, Guid? participantId)
    {
        try
        {
            var actorId = GetCurrentUserId();
            if (actorId == null) return Challenge();

            if (!teamId.HasValue || teamId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Team is required before deleting a participant." }, StatusCodes.Status400BadRequest);
            }

            if (!participantId.HasValue || participantId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Participant is required before deleting." }, StatusCodes.Status400BadRequest);
            }

            var accessibleTeamId = await _teamManagementService.GetAccessibleTeamIdAsync(teamId);
            if (!accessibleTeamId.HasValue || accessibleTeamId.Value != teamId.Value)
            {
                return JsonResultDTO(new ResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "You do not have access to delete participants for this team."
                }, StatusCodes.Status403Forbidden);
            }

            var result = await _teamManagementService.DeleteTeamParticipantAsync(accessibleTeamId.Value, participantId.Value);
            return JsonResultDTO(result, result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Participant delete failed unexpectedly for team {TeamId}, participant {ParticipantId}.", teamId, participantId);
            return JsonResultDTO(new ResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Participant could not be deleted."
            }, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<IActionResult> OnGetCompetitionAssignmentsAsync(Guid? teamId, string? tahapCode, Guid? participantId)
    {
        try
        {
            var result = await _teamManagementService.GetCompetitionAssignmentsAsync(teamId, tahapCode, participantId);
            if (!result.IsSuccess)
            {
                return JsonFailure(NoticeEnums.CompetitionAssignmentsFailedToLoadDetails, StatusCodes.Status500InternalServerError);
            }

            var optionRows = ((ResultDTO<List<TeamCompetitionAssignmentOptionDTO>>)result).Data!;

            return new JsonResult(new
            {
                success = true,
                individualCompetitions = optionRows.Where(row => row.EventType == RegistrationEnums.EntryTypeIndividual).ToList(),
                teamCompetitions = optionRows.Where(row => row.EventType == RegistrationEnums.EntryTypeTeam).ToList(),
                trackerRows = optionRows
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, NoticeEnums.CompetitionAssignmentsFailedToLoadDetails);
            return JsonFailure(NoticeEnums.CompetitionAssignmentsFailedToLoadDetails, StatusCodes.Status500InternalServerError);
        }
    }
    #endregion

    #region Draft CRUDs
    public async Task<IActionResult> OnGetDraftDetailsAsync(Guid? teamId, Guid? draftId)
    {
        try
        {
            var actorId = GetCurrentUserId();
            if (actorId == null) return Challenge();

            if (!teamId.HasValue || teamId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Team is required before viewing a draft." }, StatusCodes.Status400BadRequest);
            }

            if (!draftId.HasValue || draftId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Draft id is required." }, StatusCodes.Status400BadRequest);
            }

            var accessibleTeamId = await _teamManagementService.GetAccessibleTeamIdAsync(teamId);
            if (!accessibleTeamId.HasValue || accessibleTeamId.Value != teamId.Value)
            {
                return JsonResultDTO(new ResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "You do not have access to view this draft."
                }, StatusCodes.Status403Forbidden);
            }

            var result = await _teamManagementService.GetParticipantDraftDetailsAsync(accessibleTeamId.Value, draftId.Value);
            return JsonResultDTO(result, result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Draft details could not be loaded for team {TeamId}, draft {DraftId}.", teamId, draftId);
            return JsonResultDTO(new ParticipantDraftDetailsResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Draft could not be loaded."
            }, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<IActionResult> OnPostSaveDraftAsync([FromBody] ParticipantDraftSaveRequest? request)
    {
        try
        {
            var actorId = GetCurrentUserId();
            if (actorId == null) return Challenge();

            if (request == null)
            {
                return JsonResultDTO(new ParticipantDraftSaveResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "Draft details are required."
                }, StatusCodes.Status400BadRequest);
            }

            var accessibleTeamId = await _teamManagementService.GetAccessibleTeamIdAsync(request.TeamId);
            if (!accessibleTeamId.HasValue || accessibleTeamId.Value != request.TeamId)
            {
                return JsonResultDTO(new ParticipantDraftSaveResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "You do not have access to save drafts for this team."
                }, StatusCodes.Status403Forbidden);
            }

            var result = await _teamManagementService.SaveParticipantDraftAsync(accessibleTeamId.Value, request);
            return JsonResultDTO(result, result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Draft save failed unexpectedly for team {TeamId}, draft {DraftId}.", request?.TeamId, request?.DraftId);
            return JsonResultDTO(new ParticipantDraftSaveResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Draft could not be saved."
            }, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<IActionResult> OnPostDeleteDraftAsync([FromBody] ParticipantDraftDeleteRequest? request)
    {
        try
        {
            var actorId = GetCurrentUserId();
            if (actorId == null) return Challenge();

            if (request == null)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Draft delete request is required." }, StatusCodes.Status400BadRequest);
            }

            if (!request.DraftId.HasValue || request.DraftId.Value == Guid.Empty)
            {
                return JsonResultDTO(new ResultDTO { IsSuccess = false, NoticeTitle = "Draft id is required." }, StatusCodes.Status400BadRequest);
            }

            var accessibleTeamId = await _teamManagementService.GetAccessibleTeamIdAsync(request.TeamId);
            if (!accessibleTeamId.HasValue || accessibleTeamId.Value != request.TeamId)
            {
                return JsonResultDTO(new ResultDTO
                {
                    IsSuccess = false,
                    NoticeTitle = "You do not have access to delete drafts for this team."
                }, StatusCodes.Status403Forbidden);
            }

            var result = await _teamManagementService.DeleteParticipantDraftAsync(accessibleTeamId.Value, request.DraftId.Value);
            return JsonResultDTO(result, result.IsSuccess ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Draft delete failed unexpectedly for team {TeamId}, draft {DraftId}.", request?.TeamId, request?.DraftId);
            return JsonResultDTO(new ResultDTO
            {
                IsSuccess = false,
                NoticeTitle = "Draft could not be deleted."
            }, StatusCodes.Status500InternalServerError);
        }
    }
    #endregion

    #region Private methods
    private static JsonResult JsonResultDTO(ResultDTO result, int statusCode)
    {
        return new JsonResult(result)
        {
            StatusCode = statusCode
        };
    }

    private static JsonResult JsonFailure(string message, int statusCode)
    {
        return new JsonResult(new
        {
            success = false,
            message
        })
        {
            StatusCode = statusCode
        };
    }

    private void SetNoticeMessages(ResultDTO dto)
    {
        NoticeTitle = dto.NoticeTitle;
        NoticeDetails = dto.NoticeDetails;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
    #endregion
}
