using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Auditing;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;
using PbmEventPortal.Web.ViewModels.Admin.TieBreakerPolicies;

namespace PbmEventPortal.Web.Pages.Admin.TieBreakerPolicies;

public class EditModel : PageModel
{
    private static readonly IReadOnlyList<SelectListItem> AppliesToOptionsSource =
    [
        new SelectListItem { Value = ResultCalculationEnums.OverallTahapWinnerPolicyAppliesTo, Text = "Overall Tahap Winner" },
        new SelectListItem { Value = "CUSTOM", Text = "Custom" }
    ];

    private static readonly IReadOnlyList<SelectListItem> RuleCodeOptionsSource =
    [
        new SelectListItem { Value = "TOTAL_POINTS", Text = "Total Points" },
        new SelectListItem { Value = "AVERAGE_POINTS", Text = "Average Points" },
        new SelectListItem { Value = "EVENTS_PARTICIPATED", Text = "Events Participated" },
        new SelectListItem { Value = "INDIVIDUAL_EVENT_POINTS", Text = "Individual Event Points" },
        new SelectListItem { Value = "TEAM_EVENT_POINTS", Text = "Team Event Points" }
    ];

    private static readonly IReadOnlyList<SelectListItem> SortDirectionOptionsSource =
    [
        new SelectListItem { Value = "DESC", Text = "Descending" },
        new SelectListItem { Value = "ASC", Text = "Ascending" }
    ];

    private readonly PbmDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    public EditModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public TieBreakerPolicyEditInputDTO Input { get; set; } = new();

    public List<SelectListItem> AppliesToOptions { get; private set; } = [];

    public List<SelectListItem> RuleCodeOptions { get; private set; } = [];

    public List<SelectListItem> SortDirectionOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var policy = await LoadPolicyAsync(id, asNoTracking: true);

        if (policy == null)
        {
            return NotFound();
        }

        Input = MapToInput(policy);
        LoadLookupOptions();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        var policy = await LoadPolicyAsync(id, asNoTracking: false);

        if (policy == null)
        {
            return NotFound();
        }

        PopulateReadOnlyInputFields(policy);
        NormalizeInput(policy);
        await ValidateInputAsync(policy);
        LoadLookupOptions();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var oldPolicySnapshot = CreatePolicySnapshot(policy);
        var oldPolicyIsActive = policy.IsActive;
        var oldRuleSnapshots = policy.TieBreakerPolicyRules.ToDictionary(
            rule => rule.Id,
            rule => CreateRuleSnapshot(rule));

        var inputRulesById = Input.Rules.ToDictionary(rule => rule.Id);
        var changedRules = policy.TieBreakerPolicyRules
            .Where(rule => inputRulesById.ContainsKey(rule.Id) && IsRuleChanged(rule, inputRulesById[rule.Id]))
            .ToList();

        var policyChanged = policy.PolicyName != Input.PolicyName ||
                            policy.AppliesTo != Input.AppliesTo ||
                            policy.IsActive != Input.IsActive;

        if (policyChanged || changedRules.Count > 0)
        {
            var now = DateTime.UtcNow;

            if (policyChanged)
            {
                policy.PolicyName = Input.PolicyName;
                policy.AppliesTo = Input.AppliesTo;
                policy.IsActive = Input.IsActive;
                policy.UpdatedAt = now;
                policy.UpdatedBy = actorId.Value;
            }

            foreach (var rule in changedRules)
            {
                var inputRule = inputRulesById[rule.Id];
                rule.RuleCode = inputRule.RuleCode;
                rule.SortDirection = inputRule.SortDirection;
                rule.IsActive = inputRule.IsActive;
                rule.UpdatedAt = now;
                rule.UpdatedBy = actorId.Value;
            }

            await _dbContext.SaveChangesAsync();

            if (policyChanged)
            {
                var actionName = oldPolicyIsActive != policy.IsActive
                    ? policy.IsActive ? "ACTIVATE" : "DEACTIVATE"
                    : "UPDATE";

                var changeReason = actionName switch
                {
                    "ACTIVATE" => "System Admin reactivated tie-breaker policy.",
                    "DEACTIVATE" => "System Admin deactivated tie-breaker policy.",
                    _ => "System Admin updated tie-breaker policy."
                };

                await _auditLogService.LogAsync(
                    entityName: "tie_breaker_policies",
                    entityId: policy.Id,
                    actionName: actionName,
                    changedBy: actorId.Value,
                    changeReason: changeReason,
                    oldValues: oldPolicySnapshot,
                    newValues: CreatePolicySnapshot(policy));
            }

            foreach (var rule in changedRules)
            {
                await _auditLogService.LogAsync(
                    entityName: "tie_breaker_policy_rules",
                    entityId: rule.Id,
                    actionName: "UPDATE",
                    changedBy: actorId.Value,
                    changeReason: "System Admin updated tie-breaker policy rule.",
                    oldValues: oldRuleSnapshots[rule.Id],
                    newValues: CreateRuleSnapshot(rule));
            }
        }

        var refreshedPolicy = await LoadPolicyAsync(policy.Id, asNoTracking: true);
        Input = MapToInput(refreshedPolicy!);

        ResultIsSuccess = true;
        ResultTitle = policyChanged || changedRules.Count > 0
            ? "Tie-breaker policy updated"
            : "No changes detected";
        ResultMessage = policyChanged || changedRules.Count > 0
            ? "The tie-breaker policy has been updated successfully."
            : "No tie-breaker policy changes were detected.";

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private async Task<TieBreakerPolicy?> LoadPolicyAsync(Guid id, bool asNoTracking)
    {
        var query = _dbContext.TieBreakerPolicies
            .Include(policy => policy.CompetitionYear)
            .Include(policy => policy.TieBreakerPolicyRules)
            .Where(policy => policy.Id == id);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync();
    }

    private static TieBreakerPolicyEditInputDTO MapToInput(TieBreakerPolicy policy)
    {
        return new TieBreakerPolicyEditInputDTO
        {
            Id = policy.Id,
            CompetitionYearId = policy.CompetitionYearId,
            CompetitionYearNo = policy.CompetitionYear.YearNo,
            CompetitionYearDisplayName = policy.CompetitionYear.DisplayName,
            PolicyName = policy.PolicyName,
            AppliesTo = policy.AppliesTo,
            IsActive = policy.IsActive,
            Rules = policy.TieBreakerPolicyRules
                .OrderBy(rule => rule.PriorityNo)
                .Select(rule => new TieBreakerPolicyRuleEditInputViewModel
                {
                    Id = rule.Id,
                    PriorityNo = rule.PriorityNo,
                    RuleCode = rule.RuleCode,
                    SortDirection = rule.SortDirection,
                    IsActive = rule.IsActive
                })
                .ToList()
        };
    }

    private void PopulateReadOnlyInputFields(TieBreakerPolicy policy)
    {
        Input.Id = policy.Id;
        Input.CompetitionYearId = policy.CompetitionYearId;
        Input.CompetitionYearNo = policy.CompetitionYear.YearNo;
        Input.CompetitionYearDisplayName = policy.CompetitionYear.DisplayName;
    }

    private void NormalizeInput(TieBreakerPolicy policy)
    {
        var savedRulesById = policy.TieBreakerPolicyRules.ToDictionary(rule => rule.Id);

        Input.PolicyName = string.IsNullOrWhiteSpace(Input.PolicyName)
            ? string.Empty
            : Input.PolicyName.Trim();

        Input.AppliesTo = string.IsNullOrWhiteSpace(Input.AppliesTo)
            ? string.Empty
            : Input.AppliesTo.Trim().ToUpperInvariant();

        Input.Rules = Input.Rules
            .Where(rule => savedRulesById.ContainsKey(rule.Id))
            .Select(rule => new TieBreakerPolicyRuleEditInputViewModel
            {
                Id = rule.Id,
                PriorityNo = savedRulesById[rule.Id].PriorityNo,
                RuleCode = string.IsNullOrWhiteSpace(rule.RuleCode)
                    ? string.Empty
                    : rule.RuleCode.Trim().ToUpperInvariant(),
                SortDirection = string.IsNullOrWhiteSpace(rule.SortDirection)
                    ? string.Empty
                    : rule.SortDirection.Trim().ToUpperInvariant(),
                IsActive = rule.IsActive
            })
            .OrderBy(rule => rule.PriorityNo)
            .ToList();
    }

    private async Task ValidateInputAsync(TieBreakerPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(Input.PolicyName))
        {
            ModelState.AddModelError(nameof(Input.PolicyName), "Policy name is required.");
        }
        else if (Input.PolicyName.Length > 100)
        {
            ModelState.AddModelError(nameof(Input.PolicyName), "Policy name must not be more than 100 characters.");
        }
        else
        {
            var policyNameExists = await _dbContext.TieBreakerPolicies
                .AsNoTracking()
                .AnyAsync(existingPolicy =>
                    existingPolicy.Id != policy.Id &&
                    existingPolicy.CompetitionYearId == policy.CompetitionYearId &&
                    existingPolicy.PolicyName == Input.PolicyName);

            if (policyNameExists)
            {
                ModelState.AddModelError(nameof(Input.PolicyName), "A tie-breaker policy with this name already exists for the selected competition year.");
            }
        }

        if (string.IsNullOrWhiteSpace(Input.AppliesTo))
        {
            ModelState.AddModelError(nameof(Input.AppliesTo), "Applies to is required.");
        }
        else if (!AppliesToOptionsSource.Any(option => option.Value == Input.AppliesTo))
        {
            ModelState.AddModelError(nameof(Input.AppliesTo), "Selected applies to value is not valid.");
        }

        ValidateRules(policy);
    }

    private void ValidateRules(TieBreakerPolicy policy)
    {
        if (Input.Rules.Count != policy.TieBreakerPolicyRules.Count)
        {
            ModelState.AddModelError(nameof(Input.Rules), "The submitted tie-breaker rule set does not match the saved rule set.");
            return;
        }

        var savedRuleIds = policy.TieBreakerPolicyRules.Select(rule => rule.Id).ToHashSet();
        var submittedRuleIds = new HashSet<Guid>();
        var activeRuleCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < Input.Rules.Count; index++)
        {
            var rule = Input.Rules[index];
            var rowLabel = $"Rule row {index + 1}";

            if (!savedRuleIds.Contains(rule.Id))
            {
                ModelState.AddModelError($"Input.Rules[{index}].Id", $"{rowLabel}: Invalid tie-breaker rule row.");
            }
            else if (!submittedRuleIds.Add(rule.Id))
            {
                ModelState.AddModelError($"Input.Rules[{index}].Id", $"{rowLabel}: This tie-breaker rule row was submitted more than once.");
            }

            if (string.IsNullOrWhiteSpace(rule.RuleCode))
            {
                ModelState.AddModelError($"Input.Rules[{index}].RuleCode", $"{rowLabel}: Rule is required.");
            }
            else if (!RuleCodeOptionsSource.Any(option => option.Value == rule.RuleCode))
            {
                ModelState.AddModelError($"Input.Rules[{index}].RuleCode", $"{rowLabel}: Selected rule is not valid.");
            }
            else if (rule.IsActive && !activeRuleCodes.Add(rule.RuleCode))
            {
                ModelState.AddModelError($"Input.Rules[{index}].RuleCode", $"{rowLabel}: Active rule {rule.RuleCode} is duplicated.");
            }

            if (string.IsNullOrWhiteSpace(rule.SortDirection))
            {
                ModelState.AddModelError($"Input.Rules[{index}].SortDirection", $"{rowLabel}: Sort direction is required.");
            }
            else if (!SortDirectionOptionsSource.Any(option => option.Value == rule.SortDirection))
            {
                ModelState.AddModelError($"Input.Rules[{index}].SortDirection", $"{rowLabel}: Selected sort direction is not valid.");
            }
        }
    }

    private void LoadLookupOptions()
    {
        AppliesToOptions = AppliesToOptionsSource.Select(CloneSelectListItem).ToList();
        RuleCodeOptions = RuleCodeOptionsSource.Select(CloneSelectListItem).ToList();
        SortDirectionOptions = SortDirectionOptionsSource.Select(CloneSelectListItem).ToList();
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Tie-breaker policy update failed";
        ResultMessage = "Please review the following and try again.";
    }

    private static SelectListItem CloneSelectListItem(SelectListItem item)
    {
        return new SelectListItem
        {
            Value = item.Value,
            Text = item.Text
        };
    }

    private static bool IsRuleChanged(TieBreakerPolicyRule savedRule, TieBreakerPolicyRuleEditInputViewModel inputRule)
    {
        return savedRule.RuleCode != inputRule.RuleCode ||
               savedRule.SortDirection != inputRule.SortDirection ||
               savedRule.IsActive != inputRule.IsActive;
    }

    private static object CreatePolicySnapshot(TieBreakerPolicy policy)
    {
        return new
        {
            policy.Id,
            policy.CompetitionYearId,
            policy.PolicyName,
            policy.AppliesTo,
            policy.IsActive,
            policy.CreatedAt,
            policy.CreatedBy,
            policy.UpdatedAt,
            policy.UpdatedBy
        };
    }

    private static object CreateRuleSnapshot(TieBreakerPolicyRule rule)
    {
        return new
        {
            rule.Id,
            rule.PolicyId,
            rule.PriorityNo,
            rule.RuleCode,
            rule.SortDirection,
            rule.IsActive,
            rule.CreatedAt,
            rule.CreatedBy,
            rule.UpdatedAt,
            rule.UpdatedBy
        };
    }
}
