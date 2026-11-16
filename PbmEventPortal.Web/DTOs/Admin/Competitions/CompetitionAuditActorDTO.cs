namespace PbmEventPortal.Web.ViewModels.Admin.Competitions;

public class CompetitionAuditActorDTO
{
    public Guid Id { get; set; }

    public string FullNameAsPerId { get; set; } = string.Empty;

    public string? FullNameGurmukhi { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(FullNameGurmukhi)
        ? FullNameAsPerId
        : $"{FullNameAsPerId} / {FullNameGurmukhi}";
}
