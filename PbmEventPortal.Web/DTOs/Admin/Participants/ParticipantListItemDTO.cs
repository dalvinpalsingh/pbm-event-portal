namespace PbmEventPortal.Web.ViewModels.Admin.Participants;

public class ParticipantListItemDTO
{
    public Guid Id { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    public string TeamCode { get; set; } = string.Empty;

    public string TeamName { get; set; } = string.Empty;

    public string FullNameAsPerId { get; set; } = string.Empty;

    public string FullNameGurmukhi { get; set; } = string.Empty;

    public string IdType { get; set; } = string.Empty;

    public string? NricNumber { get; set; }

    public string? PassportNumber { get; set; }

    public string Gender { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string TahapCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string TeamDisplay => string.IsNullOrWhiteSpace(TeamName) ? TeamCode : $"{TeamCode} - {TeamName}";

    public string IdDisplay => IdType == PbmEventPortal.Application.Enums.ProfileEnums.IdTypeNric ? NricNumber ?? "NRIC not specified" : PassportNumber ?? "Passport not specified";

    public string GenderDisplay => Gender switch
    {
        "M" => "Male",
        "F" => "Female",
        _ => Gender
    };

    public string StatusDisplay => IsActive ? "Active" : "Inactive";
}
