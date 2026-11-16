using PbmEventPortal.Application.DTOs.Common;
using PbmEventPortal.Application.DTOs.TeamManagement;

namespace PbmEventPortal.Application.Services.TeamManagement;
public interface ITeamManagementService
{
    #region Participant Services
    Task<ResultDTO> GetCompetitionAssignmentsAsync(Guid? teamId, string? tahapCode, Guid? participantId);
    Task<TeamManagerAccessDTO> GetPageAccessAsync(Guid? teamId);
    Task<List<TeamParticipantSummaryDTO>> GetParticipantsAsync(Guid? teamId);

    #endregion

    #region Draft Services

    #endregion


    #region Common Services
    Task<Guid?> GetAccessibleTeamIdAsync(Guid? requestedTeamId);
    Task<ResultDTO> GetTeamManagementWorkspaceAsync(Guid? teamId);
    Task<List<TeamDTO>> GetTeamsAsync();
    

    #endregion



    Task<TeamParticipantSubmitResultDTO> CreateTeamParticipantAsync(Guid teamId, TeamParticipantSubmitRequest request);
    Task<ResultDTO> DeleteParticipantDraftAsync(Guid teamId, Guid draftId);
    Task<ResultDTO> DeleteTeamParticipantAsync(Guid teamId, Guid participantId);
    Task<TeamParticipantSubmitResultDTO> EditTeamParticipantAsync(Guid teamId, TeamParticipantSubmitRequest request);
    
    
    
    Task<List<TeamDraftSummaryDTO>> GetDraftApplicationsAsync(Guid? teamId);
    
    Task<ParticipantDraftDetailsResultDTO> GetParticipantDraftDetailsAsync(Guid teamId, Guid draftId);
    Task<TeamParticipantDetailsDTO> GetTeamParticipantDetailsAsync(Guid teamId, Guid participantId);
    
    Task<ParticipantDraftSaveResultDTO> SaveParticipantDraftAsync(Guid teamId, ParticipantDraftSaveRequest request);
}
