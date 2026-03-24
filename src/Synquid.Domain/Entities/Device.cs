using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synquid.Domain.Entities;

public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public Guid InstitutionId { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public int Status { get; set; } = 0; // 0=Active, 1=Disconnected, 2=Error
    public DateTime? LastHeartbeat { get; set; }
    public string? FirmwareVersion { get; set; }
    public float? CpuTemp { get; set; }
    public int? MemoryUsageMb { get; set; }
    public int OfflineRecordsPending { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    public Institution Institution { get; set; } = null!;
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
