using PbmEventPortal.Application.DTOs.Common;
using PbmEventPortal.Application.Enums;
using PbmEventPortal.Application.Services.Security;
using System.Security.Claims;

namespace PbmEventPortal.Web.Security;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    private bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    private Guid? Id
    {
        get
        {
            var value = GetClaimValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    private string? FullNameAsPerId
    {
        get
        {
            var value = GetClaimValue(ClaimTypes.Name);
            return value ?? string.Empty;
        }
    }

    private string? FullNameGurmukhi
    {
        get
        {
            var value = GetClaimValue(ClaimTypes.GivenName);
            return value ?? string.Empty;
        }
    }

    private string? Email
    {
        get
        {
            var value = GetClaimValue(ClaimTypes.Email);
            return value ?? string.Empty;
        }
    }

    private List<string> Roles
    {
        get
        {
            var values = User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return values ?? [];
        }
    }

    private readonly List<string> AdminRoles = [RoleEnums.SystemAdmin, RoleEnums.EventAdmin];
    private bool HasAnyAdminRole => HasAnyRole(AdminRoles);

    private string? GetClaimValue(string claimType)
    {
        return User?.FindFirstValue(claimType);
    }

    private bool HasAnyRole(List<string> roleCodes)
    {
        return roleCodes != null && roleCodes.Any(role => Roles.Contains(role));
    }

    public UserAccessDTO GetCurrentUser()
    {
        if (!IsAuthenticated)
        {
            // Return an empty or anonymous context if not logged in
            return new UserAccessDTO();
        }

        // Extract and translate the claims into UserAccessDTO
        return new UserAccessDTO
        {
            IsUserAuthenticated = IsAuthenticated,
            UserId = Id,
            UserFullNameAsPerId = FullNameAsPerId,
            UserFullNameGurmukhi = FullNameGurmukhi,
            UserEmail = Email,
            UserRoles = Roles,
            UserHasAnyAdminRole = HasAnyAdminRole
        };
    }
}
