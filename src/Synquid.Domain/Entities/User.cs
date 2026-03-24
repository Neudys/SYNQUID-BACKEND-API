using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synquid.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Role { get; set; } // 0=SuperAdmin, 1=Admin, 2=Professor, 3=Student
    public Guid? InstitutionId { get; set; }
    public string? GoogleId { get; set; }
    public string? AvatarUrl { get; set; }
    public string Language { get; set; } = "es";
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    public Institution? Institution { get; set; }
    public ICollection<NfcCard> NfcCards { get; set; } = new List<NfcCard>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
}
