using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.RegistrationEntries;

namespace PbmEventPortal.Web.Pages.Admin.RegistrationEntries;

public class CreateModel : PageModel
{
    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public CreateModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public RegistrationEntryCreateInputViewModel Input { get; set; } = new();

    public RegistrationEntryLookupOptionsViewModel Options { get; private set; } = new();
    public bool? ResultIsSuccess { get; private set; }
    public string? ResultTitle { get; private set; }
    public string? ResultMessage { get; private set; }
    public Guid? CreatedRegistrationEntryId { get; private set; }

    public async Task OnGetAsync()
    {
        Input = new RegistrationEntryCreateInputViewModel { ParticipantIds = [] };
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();
        if (actorId == null) return Challenge();

        NormalizeInput();
        var validationResult = await ValidateInputAsync();
        await LoadOptionsAsync();

        if (!ModelState.IsValid || validationResult.Category == null)
        {
            ResultIsSuccess = false;
            ResultTitle = "Registration entry creation failed";
            ResultMessage = "Please review the following and try again.";
            return Page();
        }

        var now = DateTime.UtcNow;
        Input.EntryType = validationResult.Category.EventType;

        var entry = new RegistrationEntry
        {
            Id = Guid.NewGuid(),
            CompetitionCategoryId = Input.CompetitionCategoryId!.Value,
            TeamId = Input.TeamId!.Value,
            EntryType = validationResult.Category.EventType,
            EntryName = Input.EntryName,
            EntryStatus = Input.EntryStatus,
            ApprovedBy = Input.EntryStatus == RegistrationEnums.EntryStatusApproved ? actorId.Value : null,
            RejectedReason = Input.EntryStatus == RegistrationEnums.EntryStatusRejected ? Input.RejectedReason : null,
            Notes = Input.Notes,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value
        };

        var participantIds = GetSelectedParticipantIds();
        var order = 1;
        foreach (var participantId in participantIds)
        {
            entry.RegistrationEntryMembers.Add(new RegistrationEntryMember
            {
                Id = Guid.NewGuid(),
                RegistrationEntryId = entry.Id,
                ParticipantId = participantId,
                MemberOrder = (short)order,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = actorId.Value
            });
            order++;
        }

        _dbContext.RegistrationEntries.Add(entry);
        await _auditLogService.LogAsync("registration_entries", entry.Id, "CREATE", actorId.Value,
            "System Admin created registration entry.", null, Snapshot(entry));

        CreatedRegistrationEntryId = entry.Id;
        ResultIsSuccess = true;
        ResultTitle = "Registration entry created";
        ResultMessage = "The registration entry has been created successfully.";
        Input = new RegistrationEntryCreateInputViewModel { ParticipantIds = [] };
        await LoadOptionsAsync();
        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private void NormalizeInput()
    {
        Input.EntryStatus = string.IsNullOrWhiteSpace(Input.EntryStatus) ? string.Empty : Input.EntryStatus.Trim().ToUpperInvariant();
        Input.EntryName = string.IsNullOrWhiteSpace(Input.EntryName) ? null : Input.EntryName.Trim();
        Input.RejectedReason = string.IsNullOrWhiteSpace(Input.RejectedReason) ? null : Input.RejectedReason.Trim();
        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();
        Input.ApprovedBy = null;
        Input.ParticipantIds ??= [];
    }

    private List<Guid> GetSelectedParticipantIds()
    {
        return Input.ParticipantIds
            .Where(x => x.HasValue && x.Value != Guid.Empty)
            .Select(x => x!.Value)
            .ToList();
    }

    private async Task<ValidationContextResult> ValidateInputAsync()
    {
        if (!RegistrationEnums.EntryStatuses.Contains(Input.EntryStatus)) ModelState.AddModelError(nameof(Input.EntryStatus), "Entry status is not valid.");
        if (Input.EntryStatus == RegistrationEnums.EntryStatusRejected && string.IsNullOrWhiteSpace(Input.RejectedReason)) ModelState.AddModelError(nameof(Input.RejectedReason), "Rejected reason is required for rejected entries.");
        if (Input.EntryName?.Length > 200) ModelState.AddModelError(nameof(Input.EntryName), "Entry name must not be more than 200 characters.");

        CompetitionCategory? category = null;
        if (!Input.CompetitionCategoryId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Competition category is required.");
        }
        else
        {
            category = await _dbContext.CompetitionCategories.AsNoTracking()
                .Include(x => x.Competition).ThenInclude(x => x.CompetitionYear)
                .FirstOrDefaultAsync(x => x.Id == Input.CompetitionCategoryId.Value && x.IsActive && x.Competition.IsActive && x.Competition.CompetitionYear.IsActive);

            if (category == null) ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category is not valid or inactive.");
            else Input.EntryType = category.EventType;
        }

        if (!Input.TeamId.HasValue) ModelState.AddModelError(nameof(Input.TeamId), "Team is required.");
        else if (!await _dbContext.Teams.AsNoTracking().AnyAsync(x => x.Id == Input.TeamId.Value && x.IsActive)) ModelState.AddModelError(nameof(Input.TeamId), "Selected team is not valid or inactive.");

        var participantIds = GetSelectedParticipantIds();
        if (participantIds.Count != participantIds.Distinct().Count()) ModelState.AddModelError(string.Empty, "The same participant cannot be selected more than once.");

        if (Input.TeamId.HasValue && category != null)
        {
            var requiredParticipantCount = GetRequiredParticipantCount(category);
            if (requiredParticipantCount <= 0)
            {
                ModelState.AddModelError(nameof(Input.CompetitionCategoryId), "Selected competition category does not have a valid participant/team size configured.");
            }
            else if (participantIds.Count != requiredParticipantCount)
            {
                ModelState.AddModelError(string.Empty, $"This {category.EventType.ToLowerInvariant()} entry requires exactly {requiredParticipantCount} participant(s).");
            }

            if (participantIds.Count > 0)
            {
                var participants = await _dbContext.Participants.AsNoTracking()
                    .Where(x => participantIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.TeamId, x.CompetitionYearId, x.TahapCode, x.IsActive })
                    .ToListAsync();

                if (participants.Count != participantIds.Distinct().Count()) ModelState.AddModelError(string.Empty, "One or more selected participants could not be found.");
                if (participants.Any(x => !x.IsActive)) ModelState.AddModelError(string.Empty, "Only active participants can be added to a new registration entry.");
                if (participants.Any(x => x.TeamId != Input.TeamId.Value)) ModelState.AddModelError(string.Empty, "All selected participants must belong to the selected team.");
                if (participants.Any(x => x.CompetitionYearId != category.Competition.CompetitionYearId)) ModelState.AddModelError(string.Empty, "All selected participants must belong to the selected competition year.");

                if (category.EventType == RegistrationEnums.EntryTypeIndividual && participants.Any(x => x.TahapCode != category.CategoryCode))
                {
                    ModelState.AddModelError(string.Empty, "For individual entries, the participant Tahap must match the selected category.");
                }

                var duplicateParticipantEntry = await _dbContext.RegistrationEntryMembers.AsNoTracking().AnyAsync(member =>
                    member.IsActive &&
                    participantIds.Contains(member.ParticipantId) &&
                    member.RegistrationEntry.IsActive &&
                    member.RegistrationEntry.CompetitionCategoryId == category.Id);
                if (duplicateParticipantEntry) ModelState.AddModelError(string.Empty, "One or more selected participants are already registered for this competition category.");
            }

            if (category.EventType == RegistrationEnums.EntryTypeIndividual && category.IndividualEventMaxParticipants.HasValue)
            {
                var existingCount = await _dbContext.RegistrationEntryMembers.AsNoTracking().CountAsync(member =>
                    member.IsActive &&
                    member.RegistrationEntry.IsActive &&
                    member.RegistrationEntry.TeamId == Input.TeamId.Value &&
                    member.RegistrationEntry.CompetitionCategoryId == category.Id &&
                    member.RegistrationEntry.EntryType == RegistrationEnums.EntryTypeIndividual);

                if (existingCount + participantIds.Count > category.IndividualEventMaxParticipants.Value)
                {
                    ModelState.AddModelError(string.Empty, $"This team can only register {category.IndividualEventMaxParticipants.Value} participant(s) for this individual category.");
                }
            }

            if (category.EventType == RegistrationEnums.EntryTypeTeam && category.TeamEventMaxEntriesPerTeam.HasValue)
            {
                var existingTeamEntries = await _dbContext.RegistrationEntries.AsNoTracking().CountAsync(entry =>
                    entry.IsActive &&
                    entry.TeamId == Input.TeamId.Value &&
                    entry.CompetitionCategoryId == category.Id &&
                    entry.EntryType == RegistrationEnums.EntryTypeTeam);

                if (existingTeamEntries + 1 > category.TeamEventMaxEntriesPerTeam.Value)
                {
                    ModelState.AddModelError(string.Empty, $"This team can only register {category.TeamEventMaxEntriesPerTeam.Value} team entry/entries for this category.");
                }
            }
        }

        return new ValidationContextResult(category);
    }

    private static int GetRequiredParticipantCount(CompetitionCategory category)
    {
        if (category.EventType == RegistrationEnums.EntryTypeIndividual) return 1;
        if (category.EventType == RegistrationEnums.EntryTypeTeam) return category.TeamEventTeamSize ?? 0;
        return 0;
    }

    private async Task LoadOptionsAsync()
    {
        Options.EntryStatusOptions = RegistrationEnums.EntryStatuses.Select(x => new SelectListItem { Value = x, Text = x }).ToList();

        var categories = await _dbContext.CompetitionCategories.AsNoTracking()
            .Where(x => x.IsActive && x.Competition.IsActive && x.Competition.CompetitionYear.IsActive)
            .OrderByDescending(x => x.Competition.CompetitionYear.YearNo).ThenBy(x => x.Competition.Code).ThenBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.CategoryCode,
                x.EventType,
                x.TeamEventTeamSize,
                x.Competition.CompetitionYearId,
                x.Competition.Code,
                x.Competition.NameEnglish
            })
            .ToListAsync();

        Options.CompetitionCategoryOptions = categories
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Code + " / " + x.CategoryCode + " / " + x.EventType + " - " + x.NameEnglish })
            .ToList();
        Options.CompetitionCategoryOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select competition category..." });

        Options.CategoryMetadata = categories
            .Select(x => new RegistrationEntryCategoryOptionViewModel
            {
                Id = x.Id,
                CompetitionYearId = x.CompetitionYearId,
                CategoryCode = x.CategoryCode,
                EventType = x.EventType,
                RequiredParticipantCount = x.EventType == RegistrationEnums.EntryTypeIndividual ? 1 : x.TeamEventTeamSize ?? 0
            })
            .ToList();

        Options.TeamOptions = await _dbContext.Teams.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Code + " - " + x.NameEnglish }).ToListAsync();
        Options.TeamOptions.Insert(0, new SelectListItem { Value = string.Empty, Text = "Select team..." });

        Options.ParticipantMetadata = await _dbContext.Participants.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Team.Code).ThenBy(x => x.TahapCode).ThenBy(x => x.FullNameAsPerId)
            .Select(x => new RegistrationEntryParticipantOptionViewModel
            {
                Id = x.Id,
                CompetitionYearId = x.CompetitionYearId,
                TeamId = x.TeamId,
                TahapCode = x.TahapCode,
                DisplayText = x.Team.Code + " / " + x.TahapCode + " - " + x.FullNameAsPerId
            })
            .ToListAsync();
    }

    private static object Snapshot(RegistrationEntry entry) => new
    {
        entry.Id,
        entry.CompetitionCategoryId,
        entry.TeamId,
        entry.EntryType,
        entry.EntryName,
        entry.EntryStatus,
        entry.ApprovedBy,
        entry.RejectedReason,
        entry.Notes,
        entry.IsActive,
        entry.CreatedAt,
        entry.CreatedBy,
        entry.UpdatedAt,
        entry.UpdatedBy,
        Participants = entry.RegistrationEntryMembers.OrderBy(x => x.MemberOrder).Select(x => new { x.Id, x.RegistrationEntryId, x.ParticipantId, ParticipantOrder = x.MemberOrder, x.IsActive, x.CreatedAt, x.CreatedBy, x.UpdatedAt, x.UpdatedBy }).ToList()
    };

    private sealed record ValidationContextResult(CompetitionCategory? Category);
}
