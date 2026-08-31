using SchoolPlatform.Domain.Common;

namespace SchoolPlatform.Domain.Audit;

public sealed class AuditLog : TenantEntity
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid tenantId,
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId = null,
        string? oldValuesJson = null,
        string? newValuesJson = null)
    {
        TenantId = tenantId;
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
    }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid? EntityId { get; private set; }

    public string? OldValuesJson { get; private set; }

    public string? NewValuesJson { get; private set; }
}
