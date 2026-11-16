using PbmEventPortal.Application.DTOs.Common;

namespace PbmEventPortal.Application.Services.Security;

public interface IUserContext
{
    UserAccessDTO GetCurrentUser();
}
