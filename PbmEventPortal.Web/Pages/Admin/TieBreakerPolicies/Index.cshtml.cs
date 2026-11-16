using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.TieBreakerPolicies;

namespace PbmEventPortal.Web.Pages.Admin.TieBreakerPolicies;

public class IndexModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public IndexModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<TieBreakerPolicyListItemDTO> TieBreakerPolicies { get; private set; } = [];

    public async Task OnGetAsync()
    {
        TieBreakerPolicies = await _dbContext.TieBreakerPolicies
            .AsNoTracking()
            .OrderByDescending(policy => policy.IsActive)
            .ThenByDescending(policy => policy.CompetitionYear.YearNo)
            .ThenBy(policy => policy.PolicyName)
            .Select(policy => new TieBreakerPolicyListItemDTO
            {
                Id = policy.Id,
                CompetitionYearNo = policy.CompetitionYear.YearNo,
                PolicyName = policy.PolicyName,
                AppliesTo = policy.AppliesTo,
                IsActive = policy.IsActive,
                Rules = policy.TieBreakerPolicyRules
                    .OrderBy(rule => rule.PriorityNo)
                    .Select(rule => new TieBreakerPolicyRuleListItemViewModel
                    {
                        PriorityNo = rule.PriorityNo,
                        RuleCode = rule.RuleCode,
                        SortDirection = rule.SortDirection,
                        IsActive = rule.IsActive
                    })
                    .ToList()
            })
            .ToListAsync();
    }
}
