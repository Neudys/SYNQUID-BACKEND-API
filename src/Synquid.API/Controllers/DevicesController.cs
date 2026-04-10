using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{

    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public DevicesController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
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

    [HttpGet("{id}")]
    public async Task<ActionResult> getInfoDevice(String id) 
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal principal = ValidateToken(authHeader);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (device == null) NotFound("Dispositivo no encontrado");

        return Ok(new
        {
            codigoError = 0,
            deviceInfo = device,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> updateDevice(String id, [FromQuery] updateDevice update)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal principal = ValidateToken(authHeader);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (device == null) NotFound("Dispositivo no encontrado");

        device.Name = update.name ?? device.Name;
        device.Location = update.location ?? device.Location;

        _context.SaveChanges();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Actualizado correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> deleteDevice(String id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal principal = ValidateToken(authHeader);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (device == null) NotFound("Dispositivo no encontrado");

        device.IsActive = false;

        _context.SaveChanges();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Desactivado correctamente",
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
    public async Task<ActionResult> regenerateKey([FromBody] idReceived rkp ) 
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

    private ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!);

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["JwtSettings:Audience"],
                ValidateLifetime = true
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateApiKey()
    {
        byte[] bytes = new byte[32];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        
        return Convert.ToBase64String(bytes);
    }
}

public class idReceived 
{
    public string Id { get; set; } = string.Empty;
}
public class updateDevice 
{
    public string name { get; set; } = string.Empty;
    public string location { get; set; } = string.Empty;
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

