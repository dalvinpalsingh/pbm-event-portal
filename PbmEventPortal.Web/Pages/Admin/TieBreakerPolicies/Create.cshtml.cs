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

public class CreateModel : PageModel
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

    public CreateModel(PbmDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public TieBreakerPolicyCreateInputDTO Input { get; set; } = CreateDefaultInput();

    public List<SelectListItem> CompetitionYearOptions { get; private set; } = [];

    public List<SelectListItem> AppliesToOptions { get; private set; } = [];

    public List<SelectListItem> RuleCodeOptions { get; private set; } = [];

    public List<SelectListItem> SortDirectionOptions { get; private set; } = [];

    public bool? ResultIsSuccess { get; private set; }

    public string? ResultTitle { get; private set; }

    public string? ResultMessage { get; private set; }

    public Guid? CreatedTieBreakerPolicyId { get; private set; }

    public async Task OnGetAsync()
    {
        Input = CreateDefaultInput();
        await LoadLookupOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var actorId = GetCurrentUserId();

        if (actorId == null)
        {
            return Challenge();
        }

        NormalizeInput();
        await ValidateTieBreakerPolicyAsync();
        await LoadLookupOptionsAsync();

        if (!ModelState.IsValid)
        {
            ShowFailureResult();
            return Page();
        }

        var now = DateTime.UtcNow;
        var tieBreakerPolicy = new TieBreakerPolicy
        {
            Id = Guid.NewGuid(),
            CompetitionYearId = Input.CompetitionYearId!.Value,
            PolicyName = Input.PolicyName,
            AppliesTo = Input.AppliesTo,
            IsActive = Input.IsActive,
            CreatedAt = now,
            CreatedBy = actorId.Value,
            UpdatedAt = null,
            UpdatedBy = null
        };

        var rules = Input.Rules
            .OrderBy(rule => rule.PriorityNo!.Value)
            .Select(rule => new TieBreakerPolicyRule
            {
                Id = Guid.NewGuid(),
                PolicyId = tieBreakerPolicy.Id,
                PriorityNo = rule.PriorityNo!.Value,
                RuleCode = rule.RuleCode,
                SortDirection = rule.SortDirection,
                IsActive = rule.IsActive,
                CreatedAt = now,
                CreatedBy = actorId.Value,
                UpdatedAt = null,
                UpdatedBy = null
            })
            .ToList();

        tieBreakerPolicy.TieBreakerPolicyRules = rules;
        _dbContext.TieBreakerPolicies.Add(tieBreakerPolicy);

        await _auditLogService.LogAsync(
            entityName: "tie_breaker_policies",
            entityId: tieBreakerPolicy.Id,
            actionName: "CREATE",
            changedBy: actorId.Value,
            changeReason: "System Admin created tie-breaker policy.",
            oldValues: null,
            newValues: CreateTieBreakerPolicySnapshot(tieBreakerPolicy));

        foreach (var rule in rules)
        {
            await _auditLogService.LogAsync(
                entityName: "tie_breaker_policy_rules",
                entityId: rule.Id,
                actionName: "CREATE",
                changedBy: actorId.Value,
                changeReason: "System Admin created tie-breaker policy rule.",
                oldValues: null,
                newValues: CreateTieBreakerPolicyRuleSnapshot(rule));
        }

        CreatedTieBreakerPolicyId = tieBreakerPolicy.Id;
        ResultIsSuccess = true;
        ResultTitle = "Tie-breaker policy created";
        ResultMessage = $"{tieBreakerPolicy.PolicyName} has been created successfully.";
        Input = CreateDefaultInput();

        return Page();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : null;
    }

    private void NormalizeInput()
    {
        Input.PolicyName = string.IsNullOrWhiteSpace(Input.PolicyName)
            ? string.Empty
            : Input.PolicyName.Trim();

        Input.AppliesTo = string.IsNullOrWhiteSpace(Input.AppliesTo)
            ? string.Empty
            : Input.AppliesTo.Trim().ToUpperInvariant();

        Input.Rules = Input.Rules
            .Where(rule => rule.PriorityNo.HasValue || !string.IsNullOrWhiteSpace(rule.RuleCode))
            .Select(rule => new TieBreakerPolicyRuleCreateInputViewModel
            {
                PriorityNo = rule.PriorityNo,
                RuleCode = string.IsNullOrWhiteSpace(rule.RuleCode)
                    ? string.Empty
                    : rule.RuleCode.Trim().ToUpperInvariant(),
                SortDirection = string.IsNullOrWhiteSpace(rule.SortDirection)
                    ? string.Empty
                    : rule.SortDirection.Trim().ToUpperInvariant(),
                IsActive = rule.IsActive
            })
            .OrderBy(rule => rule.PriorityNo ?? short.MaxValue)
            .ToList();
    }

    private async Task ValidateTieBreakerPolicyAsync()
    {
        if (!Input.CompetitionYearId.HasValue)
        {
            ModelState.AddModelError(nameof(Input.CompetitionYearId), "Competition year is required.");
        }
        else
        {
            var competitionYearExists = await _dbContext.CompetitionYears
                .AsNoTracking()
                .AnyAsync(competitionYear => competitionYear.Id == Input.CompetitionYearId.Value && competitionYear.IsActive);

            if (!competitionYearExists)
            {
                ModelState.AddModelError(nameof(Input.CompetitionYearId), "Selected competition year is not valid or inactive.");
            }
        }

        if (string.IsNullOrWhiteSpace(Input.PolicyName))
        {
            ModelState.AddModelError(nameof(Input.PolicyName), "Policy name is required.");
        }
        else if (Input.PolicyName.Length > 100)
        {
            ModelState.AddModelError(nameof(Input.PolicyName), "Policy name must not be more than 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(Input.AppliesTo))
        {
            ModelState.AddModelError(nameof(Input.AppliesTo), "Applies to is required.");
        }
        else if (!AppliesToOptionsSource.Any(option => option.Value == Input.AppliesTo))
        {
            ModelState.AddModelError(nameof(Input.AppliesTo), "Selected applies to value is not valid.");
        }

        if (Input.CompetitionYearId.HasValue && !string.IsNullOrWhiteSpace(Input.PolicyName))
        {
            var policyNameExists = await _dbContext.TieBreakerPolicies
                .AsNoTracking()
                .AnyAsync(policy => policy.CompetitionYearId == Input.CompetitionYearId.Value && policy.PolicyName == Input.PolicyName);

            if (policyNameExists)
            {
                ModelState.AddModelError(nameof(Input.PolicyName), "A tie-breaker policy with this name already exists for the selected competition year.");
            }
        }

        ValidateRules();
    }

    private void ValidateRules()
    {
        if (!Input.RuleCount.HasValue)
        {
            ModelState.AddModelError(nameof(Input.RuleCount), "Rule count is required.");
        }
        else if (Input.RuleCount.Value < 1 || Input.RuleCount.Value > 5)
        {
            ModelState.AddModelError(nameof(Input.RuleCount), "Rule count must be between 1 and 5.");
        }

        if (Input.RuleCount.HasValue && Input.Rules.Count != Input.RuleCount.Value)
        {
            ModelState.AddModelError(nameof(Input.Rules), $"Exactly {Input.RuleCount.Value} tie-breaker rule rows are required.");
        }

        var submittedPriorities = new HashSet<short>();
        var activeRuleCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < Input.Rules.Count; index++)
        {
            var rule = Input.Rules[index];
            var rowLabel = $"Rule row {index + 1}";

            if (!rule.PriorityNo.HasValue)
            {
                ModelState.AddModelError($"Input.Rules[{index}].PriorityNo", $"{rowLabel}: Priority is required.");
            }
            else if (rule.PriorityNo.Value < 1)
            {
                ModelState.AddModelError($"Input.Rules[{index}].PriorityNo", $"{rowLabel}: Priority must be 1 or higher.");
            }
            else if (!submittedPriorities.Add(rule.PriorityNo.Value))
            {
                ModelState.AddModelError($"Input.Rules[{index}].PriorityNo", $"{rowLabel}: Priority {rule.PriorityNo.Value} is duplicated.");
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

    private async Task LoadLookupOptionsAsync()
    {
        CompetitionYearOptions = await _dbContext.CompetitionYears
            .AsNoTracking()
            .Where(competitionYear => competitionYear.IsActive)
            .OrderByDescending(competitionYear => competitionYear.YearNo)
            .Select(competitionYear => new SelectListItem
            {
                Value = competitionYear.Id.ToString(),
                Text = competitionYear.DisplayName
            })
            .ToListAsync();

        CompetitionYearOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select competition year..."
        });

        AppliesToOptions = AppliesToOptionsSource.Select(CloneSelectListItem).ToList();
        AppliesToOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select applies to..."
        });

        RuleCodeOptions = RuleCodeOptionsSource.Select(CloneSelectListItem).ToList();
        RuleCodeOptions.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = "Select rule..."
        });

        SortDirectionOptions = SortDirectionOptionsSource.Select(CloneSelectListItem).ToList();
    }

    private void ShowFailureResult()
    {
        ResultIsSuccess = false;
        ResultTitle = "Tie-breaker policy creation failed";
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

    private static TieBreakerPolicyCreateInputDTO CreateDefaultInput()
    {
        return new TieBreakerPolicyCreateInputDTO
        {
            AppliesTo = ResultCalculationEnums.OverallTahapWinnerPolicyAppliesTo,
            IsActive = true,
            RuleCount = 1,
            Rules =
            [
                new TieBreakerPolicyRuleCreateInputViewModel
                {
                    PriorityNo = 1,
                    RuleCode = "TOTAL_POINTS",
                    SortDirection = "DESC",
                    IsActive = true
                }
            ]
        };
    }

    private static object CreateTieBreakerPolicySnapshot(TieBreakerPolicy policy)
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

    private static object CreateTieBreakerPolicyRuleSnapshot(TieBreakerPolicyRule rule)
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
