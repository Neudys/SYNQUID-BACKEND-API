using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{

    private readonly SynquidDbContext _context;

    public DevicesController(SynquidDbContext context)
    {
        _context = context;
    }


    [HttpPost]
    public async Task<ActionResult> saveDevices([FromBody] DevicePost device ) 
    {
        try
        {
            Institution? i = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == Guid.Parse(device.institutionId));
            if (i == null) return NotFound("Instituto no encontrado");

            Device deviceTemp = new Device
            {
                Id = Guid.NewGuid(),
                Name = device.name,
                Location = device.location,
                InstitutionId = Guid.Parse(device.institutionId),
                ApiKeyHash = GenerateApiKey(),
                Status = 0,
                LastHeartbeat = DateTime.UtcNow,
                FirmwareVersion = device.FirmwareVersion,
                CpuTemp = device.CpuTemp,
                MemoryUsageMb = device.MemoryUsageMb,
                OfflineRecordsPending = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Institution = i,
            };

            await _context.Devices.AddAsync(deviceTemp);
            await _context.SaveChangesAsync();
        }
        catch 
        {
            BadRequest("Error en el servidor, intentelo denuevo");
        }

        return Ok(new
        {
            codigoError = 0,
            message = "Dispositivo guardado correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<Device>>> GetAll()
    {
        return await _context.Devices
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    [HttpPost("{id}/regenerateKey")]
    public async Task<ActionResult> regenerateKey([FromBody] regenerateKeyPost rkp ) 
    {
        try
        {
            Device? d = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(rkp.Id));
            if (d == null) return NotFound("Dispositivo no encontrado");

            string apiKey = BCrypt.Net.BCrypt.HashPassword(GenerateApiKey());

            d.ApiKeyHash = apiKey;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                apiKey = apiKey,
                codigoError = 0,
                message = "Key regenerada",
                timestamp = DateTime.UtcNow
            });
        }
        catch 
        {
            return BadRequest("Error en el servidor");
        }
    }

    [HttpPost("{id}/heartbeat")]
    public async Task<ActionResult> GetHeartBeat([FromBody] DeviceRequest deviceInfo)
    {
        Guid guid = Guid.Parse(Request.RouteValues["id"]?.ToString() ?? string.Empty);
        Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == guid);
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



    private string GenerateApiKey()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        return Convert.ToBase64String(bytes);
    }
}

public class regenerateKeyPost 
{
    public string Id { get; set; } = string.Empty;
}
public class DevicePost 
{
    public string location { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;    
    public string institutionId { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public float CpuTemp { get; set; } = 0;
    public int MemoryUsageMb { get; set; } = 0;
}
public class DeviceRequest 
{
    public float? CpuTemp { get; set; }
    public DateTime? LastHeartbeat { get; set; } = DateTime.Now;
    public int? MemoryUsageMb { get; set; }
    public string? FirmwareVersion { get; set; } 
}

