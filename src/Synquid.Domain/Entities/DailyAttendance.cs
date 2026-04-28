using System;

namespace Synquid.Domain.Entities;

public class DailyAttendance
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public Guid UserId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid GroupId { get; set; }
    public Guid ProfessorId { get; set; }
    public int Status { get; set; } // 0=Present, 1=Absent, 2=Justified, 3=Late
    public Guid? ModifiedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    public User User { get; set; } = null!;
    public Schedule Schedule { get; set; } = null!;
    public Group Group { get; set; } = null!;
    public User Professor { get; set; } = null!;
    public User? ModifiedBy { get; set; }
}
