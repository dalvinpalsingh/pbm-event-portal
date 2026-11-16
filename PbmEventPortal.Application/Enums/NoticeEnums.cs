namespace PbmEventPortal.Application.Enums;

public static class NoticeEnums
{
    #region Team Management
    public const string CompetitionAssignmentsFailedToLoadTitle = "Competition assignments failed to load.";
    public const string CompetitionAssignmentsFailedToLoadDetails = "Competition assignments failed to load.";

    public const string CompetitionAssignmentsFoundTitle = "Competition assignments found.";
    public const string CompetitionAssignmentsFoundDetails = "Competition assignments found";

    public const string UnexpectedErrorTitle = "Unexpected error.";
    public const string UnexpectedErrorDetails = "Please try again, or contact the Admin if issue persists.";

    public const string ForbiddenTeamAccessTitle = "Forbidden.";
    public const string ForbiddenTeamAccessDetails = "You are not authorized to access this team.";

    public const string ParticipantNotFoundTitle = "Participant not found.";
    public const string ParticipantNotFoundDetails = "The selected participant could not be found or is inactive.";

    public const string ParticipantsFoundTitle = "Participants found.";
    public const string ParticipantsFoundDetails = "The list of participants found.";

    public const string ParticipantsNotFoundTitle = "Participants not found.";
    public const string ParticipantsNotFoundDetails = "The list of participants could not be found.";

    public const string NoTeamAssignedTitle = "No team assigned";
    public const string NoTeamAssignedDetails = "You have Team Manager access, but no active team has been assigned yet. Please contact the Admin.";

    public const string TahapIsRequiredTitle = "Tahap is required";
    public const string TahapIsRequiredDetails = "Tahap is required before competition assignments can be loaded.";

    public const string TeamFoundTitle = "Team found successfully.";
    public const string TeamFoundDetails = "Team found successfully.";

    public const string TeamIdNotProvidedTitle = "Team Id not provided.";
    public const string TeamIdNotProvidedDetails = "Please select a team.";

    public const string TeamManagementWorkspaceFailedToLoadTitle = "Team Management workspace failed to load.";
    public const string TeamManagementWorkspaceFailedToLoadDetails = "Team Management workspace failed to load.";

    public const string TeamManagementWorkspaceLoadedTitle = "Team Management loaded.";
    public const string TeamManagementWorkspaceLoadedDetails = "Team Management loaded.";

    public const string TeamNotFoundTitle = "Team not found.";
    public const string TeamNotFoundDetails = "The selected team could not be found or is inactive.";

    public const string UserNotAuthenticated = "User not authenticated.";
    public const string UserNotAuthenticatedDetails = "Please login again, or contact the Admin if issue persists.";

    public const string UserNotFoundTitle = "User not found.";
    public const string UserNotFoundDetails = "The selected user could not be found or is inactive.";
    #endregion
}
