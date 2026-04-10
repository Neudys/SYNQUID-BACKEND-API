using Synquid.Application.Interfaces;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly SynquidDbContext _context;

    public AuditService(SynquidDbContext context)
    {
        _context = context;
    }

    public async Task Log(Guid userId, string action, string entity, Guid entityId, string? details = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
