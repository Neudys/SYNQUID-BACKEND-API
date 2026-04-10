namespace Synquid.Application.Interfaces;

public interface IAuditService
{
    Task Log(Guid userId, string action, string entity, Guid entityId, string? details = null);
}
