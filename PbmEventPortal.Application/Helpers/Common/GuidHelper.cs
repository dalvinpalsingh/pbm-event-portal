namespace PbmEventPortal.Application.Helpers.Common;

public class GuidHelper
{
    public static bool IsValidGuid(Guid? guid)
    {
        return guid.HasValue && guid != Guid.Empty;
    }
}
