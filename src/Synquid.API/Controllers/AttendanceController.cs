using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.Application.Interfaces;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;

    public AttendanceController(SynquidDbContext context, IConfiguration config, IAuditService audit)
    {
        _context = context;
        _config = config;
        _audit = audit;
    }

    // POST /api/attendance/check
    [HttpPost("check")]
    public async Task<ActionResult> Check([FromBody] CheckRequest request)
    {
        // Buscar si existe una tarjeta con ese UID en la base de datos
        var card = await _context.NfcCards
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.HashUid == request.Uid && c.IsActive);

        if (card == null)
        {
            return Ok(new
            {
                found = false,
                message = "Tarjeta NO encontrada"
            });
        }

        return Ok(new
        {
            found = true,
            message = "Tarjeta encontrada",
            studentName = card.User.FirstName + " " + card.User.LastName,
            studentEmail = card.User.Email
        });
    }


    // POST /api/attendance/register
    [HttpPost("Register")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {

        Guid moduleUid = Guid.Parse(request.ModuleUid);
        Guid cardUid = Guid.Parse(request.Uuid);

        // encuentra el dispositivo y verifica que esté activo
        Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == moduleUid);
        if (device == null) return NotFound("Dispositivo no encontrado");

        // encuentra la tarjeta y verifica que esté activa
        NfcCard? card = await _context.NfcCards.FirstOrDefaultAsync(c => c.Id == cardUid);
        if (card == null) return NotFound("Tarjeta no encontrada");

        // encuentra el usuario asociado a la tarjeta y verifica que pertenezca a la misma institución que el dispositivo
        User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == card.UserId);
        if (user == null) return NotFound("Usuario no encontrado para esta tarjeta");
        if (!user.IsActive) return BadRequest("El usuario asociado a esta tarjeta no está activo");
        if (user.InstitutionId != device.InstitutionId) return BadRequest("El usuario no pertenece a la institución de este dispositivo");

        _context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = user.Id,
            DeviceId = device.Id,
            TimestampUtc = request.timeStampUtc,
            TimestampLocal = request.timeStampLocal,
            Type = 0,
            Status = 0,
            NfcHash = card.HashUid,
            IsSynced = request.IsSynced,
            CreatedAt = DateTime.UtcNow,
            Device = device,
            User = user,
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            found = true,
            message = "Asistencia registrada correctamente"
        });
    }

    [HttpPost("sync")]
    public async Task<ActionResult> SyncAttendance([FromBody] List<syncAttendance> syncRequest)
    {
        var errors = new List<string>();

        foreach (syncAttendance r in syncRequest)
        {
            Guid moduleUid = Guid.Parse(r.ModuleUid);

            Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == moduleUid);
            if (device == null) { errors.Add($"Dispositivo {r.ModuleUid} no encontrado"); continue; }
            if (!device.IsActive) { errors.Add($"Dispositivo {r.ModuleUid} no está activo"); continue; }

            NfcCard? card = await _context.NfcCards.FirstOrDefaultAsync(c => c.HashUid == r.Uuid);
            if (card == null) { errors.Add($"Tarjeta {r.Uuid} no encontrada"); continue; }
            if (!card.IsActive) { errors.Add($"Tarjeta {r.Uuid} está revocada"); continue; }

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == card.UserId);
            if (user == null) { errors.Add($"Usuario no encontrado para tarjeta {r.Uuid}"); continue; }
            if (!user.IsActive) { errors.Add($"Usuario {user.Id} no está activo"); continue; }
            if (user.InstitutionId != device.InstitutionId) { errors.Add($"Usuario {user.Id} no pertenece a la institución del dispositivo"); continue; }
            
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                UserId = user.Id,
                DeviceId = device.Id,
                TimestampUtc = r.timeStampUtc,
                TimestampLocal = r.timeStampLocal,
                Type = 0,
                Status = 0,
                NfcHash = card.HashUid,
                IsSynced = r.IsSynced,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            processed = syncRequest.Count - errors.Count,
            failed = errors.Count,
            errors
        });
    }

    [HttpPost("manual")]
    public async Task<ActionResult> manualChange([FromQuery] manual manualChange) 
    {
        User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(manualChange.userId));
        if (user == null) return NotFound("Usuario no encontrado");
        if (!user.IsActive) return BadRequest("El usuario asociado no está activo");

        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal principal = ValidateToken(authHeader);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        _context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = Guid.Parse(manualChange.userId),
            DeviceId = null,          
            TimestampUtc = DateTime.UtcNow,
            TimestampLocal = DateTime.Now,
            Type = 1,                  
            Status = manualChange.status,         
            NfcHash = null,           
            IsSynced = true,          
            RegisteredById = Guid.Parse(manualChange.profesorId),
            CreatedAt = DateTime.UtcNow,
        });

        return Ok(new
        {
            found = true,
            message = "Asistencia registrada correctamente"
        });
    }

    [HttpGet("today")]
    public async Task<ActionResult> TodayAttendance([FromQuery] todayAttendance request)
    {
        string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        ClaimsPrincipal principal = ValidateToken(token);
        if (principal == null) return Unauthorized("Token inválido");

        Institution? institution = await _context.Institutions
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.institutionId));
        if (institution == null) return NotFound("Institución no encontrada");

        Group? group = await _context.Groups
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.groupId));
        if (group == null) return NotFound("Grupo no encontrado");

        if (group.InstitutionId != institution.Id)
            return BadRequest("El grupo no pertenece a la institución");

        DateTime today = DateTime.UtcNow.Date;

        List<AttendanceRecord> records = await _context.AttendanceRecords
            .Where(x =>
                x.TimestampUtc.Date == today &&
                _context.GroupMembers.Any(gm => gm.GroupId == group.Id && gm.UserId == x.UserId))
            .ToListAsync();

        return Ok(new
        {
            date = today,
            groupId = group.Id,
            total = records.Count,
            records
        });
    }

    [HttpGet("history")]
    public async Task<ActionResult> historyAttendance([FromQuery] historyAttendance attendance) 
    {
        string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        ClaimsPrincipal principal = ValidateToken(token);
        if (principal == null) return Unauthorized("Token inválido");

        User? user = await _context.Users.FirstOrDefaultAsync( x => x.Id == Guid.Parse(attendance.userId));

        Group? group = await _context.Groups
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(attendance.groupId));

        if (group == null) return NotFound("Grupo no encontrado");
        if (user == null) return NotFound("Profesor no encontrado");

        if (group.ProfessorId != user.Id)
            return BadRequest("el profesor no esta asignado al grupo");

        List<AttendanceRecord> records = await _context.AttendanceRecords
           .Where(x =>
               x.TimestampUtc.Date >= attendance.from && x.TimestampUtc.Date <= attendance.to )
           .Skip((attendance.page - 1) * attendance.limit)
           .ToListAsync();

        return Ok(new
        {
            groupId = group.Id,
            total = records.Count,
            records
        });

    }

    [HttpGet("myHistory")]
    public async Task<ActionResult> myHistory([FromQuery] historyAttendance attendance) 
    {
        string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        ClaimsPrincipal principal = ValidateToken(token);
        if (principal == null) return Unauthorized("Token inválido");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(attendance.userId));

        if (user == null) return NotFound("Profesor no encontrado");

        List<AttendanceRecord> records = await _context.AttendanceRecords
           .Where(x => x.TimestampUtc.Date >= attendance.from && x.TimestampUtc.Date <= attendance.to && x.UserId == Guid.Parse(attendance.userId))
           .Skip((attendance.page - 1) * attendance.limit)
           .ToListAsync();

        return Ok(new
        {
            total = records.Count,
            records
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
}



public class CheckRequest
{
    public string Uid { get; set; } = string.Empty;
}

public class historyAttendance 
{
    public DateTime from { get; set; } = DateTime.Now;
    public DateTime to { get; set; } = DateTime.Now;
    public string userId { get; set; } = string.Empty;
    public string groupId { get; set; } = string.Empty;
    public int page { get; set; } = 0;
    public int limit { get; set; } = 0;
}

public class RegisterRequest
{
    public string Uuid { get; set; } = string.Empty;
    public string ModuleUid { get; set; } = string.Empty;
    public DateTime timeStampUtc { get; set; } = DateTime.Now;
    public DateTime timeStampLocal { get; set; } = DateTime.Now;
    public bool IsSynced { get; set; }
}

public class syncAttendance 
{
    public string Uuid { get; set; } = string.Empty;
    public string ModuleUid { get; set; } = string.Empty;
    public DateTime timeStampUtc { get; set; } = DateTime.Now;
    public DateTime timeStampLocal { get; set; } = DateTime.Now;
    public bool IsSynced { get; set; }
}

public class manual 
{
    public string userId { get; set; } = string.Empty; 
    public string profesorId { get; set; } = string.Empty;
    public int status { get; set; } = 0;
    public string notes { get; set; } = string.Empty ;
}

public class todayAttendance 
{
    public string groupId { get; set; } = string.Empty; 
    public string institutionId { get; set; } = string.Empty;
}