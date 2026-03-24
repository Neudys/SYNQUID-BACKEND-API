using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Synquid.Domain.Entities;

public class Institution
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public int Type { get; set; } // 0=Educational, 1=Business
    public string? LogoUrl { get; set; }
    public string Timezone { get; set; } = "Europe/Madrid";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
