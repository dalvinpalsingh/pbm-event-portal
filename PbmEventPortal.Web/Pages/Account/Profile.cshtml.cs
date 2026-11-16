using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Web.ViewModels.Account;

namespace PbmEventPortal.Web.Pages.Account;

public class ProfileModel : PageModel
{
    private readonly PbmDbContext _dbContext;

    public ProfileModel(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public AccountProfileDTO? UserProfile { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Challenge();
        }

        UserProfile = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => new AccountProfileDTO
            {
                Id = user.Id,
                FullNameAsPerId = user.FullNameAsPerId,
                FullNameGurmukhi = user.FullNameGurmukhi,
                IdType = user.IdType,
                NricNumber = user.NricNumber,
                PassportNumber = user.PassportNumber,
                PassportExpiryDate = user.PassportExpiryDate,
                MobileNumber = user.MobileNumber,
                Email = user.Email,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive
            })
            .FirstOrDefaultAsync();

        if (UserProfile == null)
        {
            return NotFound();
        }

        return Page();
    }
}