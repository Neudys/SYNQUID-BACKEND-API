using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Synquid.Domain.Entities;
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;  // "CREATE", "UPDATE", "DELETE"
    public string Entity { get; set; } = string.Empty;  // "User", "Group", "Device"
    public Guid EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Navegación
    public User User { get; set; } = null!;
}