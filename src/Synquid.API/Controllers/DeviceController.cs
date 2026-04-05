using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{

    private readonly SynquidDbContext _context;

    public DeviceController(SynquidDbContext context)
    {
        _context = context;
    }


    [HttpPost("{id}/heartbeat")]
    public async Task<ActionResult> GetHeartBeat([FromBody] DeviceRequest deviceInfo)
    {
        Guid guid = Guid.Parse(Request.RouteValues["id"]?.ToString() ?? string.Empty);
        Device device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == guid);
        if (device == null) BadRequest("El dispositivo no existe");

        device.CpuTemp = deviceInfo.CpuTemp;
        device.LastHeartbeat = deviceInfo.LastHeartbeat;
        device.MemoryUsageMb = deviceInfo.MemoryUsageMb;
        device.FirmwareVersion = deviceInfo.FirmwareVersion;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Heartbeat received",
            timestamp = DateTime.UtcNow
        });
    }
}

public class DeviceRequest 
{
    public float? CpuTemp { get; set; }
    public DateTime? LastHeartbeat { get; set; } = DateTime.Now;
    public int? MemoryUsageMb { get; set; }
    public string? FirmwareVersion { get; set; } 
}

