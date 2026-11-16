using System.Text.Json;
using PbmEventPortal.Data.NpgSQL;
using PbmEventPortal.Data.NpgSQL.Entities;

namespace PbmEventPortal.Application.Services.Auditing;

public class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    private readonly PbmDbContext _dbContext;

    public AuditLogService(PbmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        string entityName,
        Guid? entityId,
        string actionName,
        Guid? changedBy,
        string changeReason,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("Entity name cannot be empty.", nameof(entityName));
        }

        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new ArgumentException("Action name cannot be empty.", nameof(actionName));
        }

        if (string.IsNullOrWhiteSpace(changeReason))
        {
            throw new ArgumentException("Change reason cannot be empty.", nameof(changeReason));
        }

        var now = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            ActionName = actionName,
            ChangedBy = changedBy,
            ChangedAt = now,
            ChangeReason = changeReason,
            OldValuesJson = SerializeJson(oldValues),
            NewValuesJson = SerializeJson(newValues),
            CreatedAt = now,
            CreatedBy = changedBy,
            UpdatedAt = null,
            UpdatedBy = null
        };

        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializeJson(object? value)
    {
        if (value == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, JsonSerializerOptions);
    }
}
