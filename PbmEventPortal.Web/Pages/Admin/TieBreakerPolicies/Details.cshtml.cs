using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Admin.TieBreakerPolicies;

namespace PbmEventPortal.Web.Pages.Admin.TieBreakerPolicies;

public class DetailsModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public DetailsModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public TieBreakerPolicyDetailsDTO? TieBreakerPolicy { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var policy = await _dbContext.TieBreakerPolicies
            .AsNoTracking()
            .Include(policy => policy.CompetitionYear)
            .Include(policy => policy.TieBreakerPolicyRules)
            .FirstOrDefaultAsync(policy => policy.Id == id);

        if (policy == null)
        {
            return NotFound();
        }

        TieBreakerPolicy = new TieBreakerPolicyDetailsDTO
        {
            Id = policy.Id,
            CompetitionYearNo = policy.CompetitionYear.YearNo,
            CompetitionYearDisplayName = policy.CompetitionYear.DisplayName,
            PolicyName = policy.PolicyName,
            AppliesTo = policy.AppliesTo,
            IsActive = policy.IsActive,
            Rules = policy.TieBreakerPolicyRules
                .OrderBy(rule => rule.PriorityNo)
                .Select(rule => new TieBreakerPolicyRuleDetailsViewModel
                {
                    Id = rule.Id,
                    PriorityNo = rule.PriorityNo,
                    RuleCode = rule.RuleCode,
                    SortDirection = rule.SortDirection,
                    IsActive = rule.IsActive
                })
                .ToList()
        };

        return Page();
    }
}
