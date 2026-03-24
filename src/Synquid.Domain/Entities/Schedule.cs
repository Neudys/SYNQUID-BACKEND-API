using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synquid.Domain.Entities;

public class Schedule
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public int DayOfWeek { get; set; } // 0=Domingo ... 6=Sábado
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int LateToleranceMinutes { get; set; } = 10;
    public bool IsActive { get; set; } = true;

    // Navegación
    public Group Group { get; set; } = null!;
}
