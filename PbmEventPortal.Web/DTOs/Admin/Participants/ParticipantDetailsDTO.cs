namespace PbmEventPortal.Web.ViewModels.Admin.Participants;

public class ParticipantDetailsDTO
{
    public Guid Id { get; set; }

    public string CompetitionYearDisplayName { get; set; } = string.Empty;

    public string TeamDisplay { get; set; } = string.Empty;

    public string FullNameAsPerId { get; set; } = string.Empty;

    public string FullNameGurmukhi { get; set; } = string.Empty;

    public string IdType { get; set; } = string.Empty;

    public string? NricNumber { get; set; }

    public string? PassportNumber { get; set; }

    public DateOnly? PassportExpiryDate { get; set; }

    public string? MobileNumber { get; set; }

    public string? Email { get; set; }

    public string ParentGuardianName { get; set; } = string.Empty;

    public string ParentGuardianMobile { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string TahapCode { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByName { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedByName { get; set; }

    public List<ParticipantRegistrationEntryViewModel> RegistrationEntries { get; set; } = [];

    public string IdDisplay => IdType == PbmEventPortal.Application.Enums.ProfileEnums.IdTypeNric ? NricNumber ?? "NRIC not specified" : PassportNumber ?? "Passport not specified";

    public string PassportExpiryDisplay => PassportExpiryDate.HasValue ? PassportExpiryDate.Value.ToString("yyyy-MM-dd") : "Not applicable";

    public string MobileDisplay => string.IsNullOrWhiteSpace(MobileNumber) ? "Not specified" : MobileNumber;

    public string EmailDisplay => string.IsNullOrWhiteSpace(Email) ? "Not specified" : Email;

    public string GenderDisplay => Gender switch
    {
        "M" => "Male",
        "F" => "Female",
        _ => Gender
    };

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    public string CreatedByDisplay => string.IsNullOrWhiteSpace(CreatedByName) ? "System / unknown" : CreatedByName;

    public string UpdatedByDisplay => string.IsNullOrWhiteSpace(UpdatedByName) ? "Not updated yet" : UpdatedByName;
}

public class ParticipantRegistrationEntryViewModel
{
    public Guid Id { get; set; }

    public string CompetitionDisplay { get; set; } = string.Empty;

    public string CategoryDisplay { get; set; } = string.Empty;

    public string EntryType { get; set; } = string.Empty;

    public string EntryStatus { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public short ParticipantOrder { get; set; }

    public string OtherParticipantsDisplay { get; set; } = string.Empty;

    public string StatusDisplay => IsActive ? EntryStatus : $"Inactive / {EntryStatus}";
}
