using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synquid.Domain.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DeviceId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public DateTime TimestampLocal { get; set; }
    public int Type { get; set; } // 0=NFC, 1=HCE, 2=Manual
    public int Status { get; set; } // 0=Present, 1=Absent, 2=Justified, 3=Late
    public string? NfcHash { get; set; }
    public Guid? RegisteredById { get; set; }
    public bool IsSynced { get; set; } = true;
    public string? Nonce { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public User User { get; set; } = null!;
    public Device? Device { get; set; }
    public User? RegisteredBy { get; set; }
}
