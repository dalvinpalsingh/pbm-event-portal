namespace PbmEventPortal.Application.Services.Auditing;

public interface IAuditLogService
{
    Task LogAsync(
        string entityName,
        Guid? entityId,
        string actionName,
        Guid? changedBy,
        string changeReason,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default);
}