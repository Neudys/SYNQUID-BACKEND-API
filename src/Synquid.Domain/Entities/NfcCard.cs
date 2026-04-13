using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synquid.Domain.Entities;

public class NfcCard
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string HashUid { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public int CardType { get; set; } // 0=Physical, 1=HCE
    public string? HceToken { get; set; }
    public DateTime? HceTokenExpiry { get; set; }
    public string? DeviceFingerprint { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public User User { get; set; } = null!;
}
