using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.API.Extensions;
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
    [Authorize]
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

    [HttpGet("{id}")]
    public async Task<ActionResult> GetAttendanceRecord(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        AttendanceRecord? record = await _context.AttendanceRecords
            .Include(a => a.User)
            .Include(a => a.Device)
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (record == null)
            return NotFound("Registro de asistencia no encontrado");

        return Ok(new
        {
            codigoError = 0,
            record = record,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAttendanceRecord(string id, [FromBody] updateAttendanceRecord update)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        AttendanceRecord? record = await _context.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (record == null)
            return NotFound("Registro de asistencia no encontrado");

        record.TimestampUtc = update.timestampUtc ?? record.TimestampUtc;
        record.TimestampLocal = update.timestampLocal ?? record.TimestampLocal;
        record.Status = update.status ?? record.Status;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            message = "Registro de asistencia actualizado correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAttendance([FromQuery] exportAttendance request)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        List<AttendanceRecord> records = await _context.AttendanceRecords
            .Include(a => a.User)
            .Where(a => a.TimestampUtc.Date >= request.from.Date && a.TimestampUtc.Date <= request.to.Date)
            .OrderBy(a => a.TimestampUtc)
            .ToListAsync();

        var csv = "UserId,UserName,DeviceId,TimestampUTC,TimestampLocal,Type,Status,IsSynced\n";

        foreach (var record in records)
        {
            csv += $"{record.UserId},{record.User?.FirstName} {record.User?.LastName},{record.DeviceId},{record.TimestampUtc},{record.TimestampLocal},{record.Type},{record.Status},{record.IsSynced}\n";
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"attendance_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetAttendanceStats([FromQuery] statsRequest request)
    {
        var from = DateTime.SpecifyKind(request.from, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(request.to, DateTimeKind.Utc);

        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        List<AttendanceRecord> records = await _context.AttendanceRecords
            .Where(a => a.TimestampUtc.Date >= from && a.TimestampUtc.Date <= to)
            .ToListAsync();

        int totalRecords = records.Count;
        int presentCount = records.Count(a => a.Status == 0);
        int absentCount = records.Count(a => a.Status == 1);
        int lateCount = records.Count(a => a.Status == 2);

        return Ok(new
        {
            codigoError = 0,
            from = request.from.Date,
            to = request.to.Date,
            totalRecords = totalRecords,
            present = presentCount,
            absent = absentCount,
            late = lateCount,
            averageAttendance = totalRecords > 0 ? (double)presentCount / totalRecords * 100 : 0,
            timestamp = DateTime.UtcNow
        });
    }

    // POST /api/attendance/register
    [Authorize]
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

    [Authorize]
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
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
        if (principal == null) return Unauthorized("Token invalido");
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

        await _context.SaveChangesAsync();

        return Ok(new
        {
            found = true,
            message = "Asistencia registrada correctamente"
        });
    }

    [Authorize]
    [HttpGet("All")]
    public async Task<ActionResult<List<AttendanceRecord>>> GetAll()
    {
        return await _context.AttendanceRecords.ToListAsync();
    }

    [HttpGet("today")]
    public async Task<ActionResult> TodayAttendance([FromQuery] todayAttendance request)
    {
        string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(token, _config);
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
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(token, _config);
        if (principal == null) return Unauthorized("Token inválido");

        if (!Guid.TryParse(attendance.userId, out var userId))
            return BadRequest("userId no es un Guid válido");

        if (!Guid.TryParse(attendance.groupId, out var groupId))
            return BadRequest("groupId no es un Guid válido");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        Group? group = await _context.Groups.FirstOrDefaultAsync(x => x.Id == groupId);

        if (group == null) return NotFound("Grupo no encontrado");
        if (user == null) return NotFound("Profesor no encontrado");
        if (group.ProfessorId != user.Id)
            return BadRequest("el profesor no esta asignado al grupo");

        var fromUtc = DateTime.SpecifyKind(attendance.from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(attendance.to, DateTimeKind.Utc);

        List<AttendanceRecord> records = await _context.AttendanceRecords
            .Where(x =>
                x.TimestampUtc >= fromUtc &&
                x.TimestampUtc <= toUtc)
            .Skip((attendance.page - 1) * attendance.limit)
            .Take(attendance.limit)
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
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(token, _config);
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
}

public class updateAttendanceRecord
{
    public DateTime? timestampUtc { get; set; }
    public DateTime? timestampLocal { get; set; }
    public int? status { get; set; }
    public string notes { get; set; } = string.Empty;
}

public class exportAttendance
{
    public DateTime from { get; set; } = DateTime.UtcNow.AddDays(-7);
    public DateTime to { get; set; } = DateTime.UtcNow;
}

public class statsRequest
{
    public DateTime from { get; set; } = DateTime.UtcNow.AddDays(-30);
    public DateTime to { get; set; } = DateTime.UtcNow;
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