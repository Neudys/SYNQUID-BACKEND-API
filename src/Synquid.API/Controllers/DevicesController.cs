using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.API.Extensions;
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
    public async Task<ActionResult> saveDevices([FromBody] DevicePost device)
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

            return Ok(new
            {
                codigoError = 0,
                message = "Dispositivo guardado correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> getInfoDevice(String id)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (device == null) return NotFound("Dispositivo no encontrado");

            return Ok(new
            {
                codigoError = 0,
                deviceInfo = device,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> updateDevice(String id, [FromBody] updateDevice update)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (device == null) return NotFound("Dispositivo no encontrado");

            device.Name = update.name ?? device.Name;
            device.Location = update.location ?? device.Location;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                codigoError = 0,
                mensaje = "Actualizado correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> deleteDevice(String id)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (device == null) return NotFound("Dispositivo no encontrado");

            device.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                codigoError = 0,
                mensaje = "Desactivado correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<Device>>> GetAll()
    {
        try
        {
            return await _context.Devices
                .Where(i => i.IsActive)
                .OrderBy(i => i.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("{id}/regenerateKey")]
    public async Task<ActionResult> regenerateKey([FromBody] idReceived rkp)
    {
        try
        {
            Device? d = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(rkp.Id));
            if (d == null) return NotFound("Dispositivo no encontrado");

            string apiKey = GenerateApiKey();

            // guarda solo el hash, la key en claro solo se devuelve esta unica vez
            d.ApiKeyHash = BCrypt.Net.BCrypt.HashPassword(apiKey);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                apiKey = apiKey,
                codigoError = 0,
                message = "Key regenerada",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("{id}/heartbeat")]
    public async Task<ActionResult> GetHeartBeat([FromBody] DeviceRequest deviceInfo)
    {
        try
        {
            Guid guid = Guid.Parse(Request.RouteValues["id"]?.ToString() ?? string.Empty);
            Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == guid);
            if (device == null) return BadRequest("El dispositivo no existe");

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateDeviceStatus(string id, [FromBody] updateDeviceStatus statusUpdate)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (device == null)
                return NotFound("Dispositivo no encontrado");

            device.Status = statusUpdate.status;
            device.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                codigoError = 0,
                mensaje = "Estado del dispositivo actualizado correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id}/logs")]
    public async Task<ActionResult> GetDeviceLogs(string id, [FromQuery] int page = 1)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            Device? device = await _context.Devices.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (device == null)
                return NotFound("Dispositivo no encontrado");

            List<AttendanceRecord> logs = await _context.AttendanceRecords
                .Where(a => a.DeviceId == Guid.Parse(id))
                .OrderByDescending(a => a.TimestampUtc)
                .Skip((page - 1) * 20)
                .Take(20)
                .ToListAsync();

            int total = await _context.AttendanceRecords
                .Where(a => a.DeviceId == Guid.Parse(id))
                .CountAsync();

            return Ok(new
            {
                codigoError = 0,
                deviceId = id,
                page = page,
                totalLogs = total,
                logs = logs,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("byInstitution/{institutionId}")]
    public async Task<ActionResult> GetDevicesByInstitution(string institutionId)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            Institution? institution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == Guid.Parse(institutionId));

            if (institution == null)
                return NotFound("Institución no encontrada");

            List<Device> devices = await _context.Devices
                .Where(d => d.InstitutionId == Guid.Parse(institutionId) && d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            return Ok(new
            {
                codigoError = 0,
                institutionId = institutionId,
                totalDevices = devices.Count,
                devices = devices,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    private string GenerateApiKey()
    {
        // genera 32 bytes criptograficamente seguros y los convierte a base64
        byte[] bytes = new byte[32];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return Convert.ToBase64String(bytes);
    }
}

public class updateDeviceStatus
{
    public int status { get; set; } = 0;
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