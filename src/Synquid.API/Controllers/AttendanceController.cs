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

    [HttpGet("{id}")]
    public async Task<ActionResult> GetAttendanceRecord(string id)
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAttendanceRecord(string id, [FromBody] updateAttendanceRecord update)
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAttendance([FromQuery] exportAttendance request)
    {
        try
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

            // construye el CSV manualmente concatenando cada fila
            var csv = "UserId,UserName,DeviceId,TimestampUTC,TimestampLocal,Type,Status,IsSynced\n";

            foreach (var record in records)
            {
                csv += $"{record.UserId},{record.User?.FirstName} {record.User?.LastName},{record.DeviceId},{record.TimestampUtc},{record.TimestampLocal},{record.Type},{record.Status},{record.IsSynced}\n";
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"attendance_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetAttendanceStats([FromQuery] statsRequest request)
    {
        try
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

            // agrupa conteos por cada estado posible
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("Register")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            Guid moduleUid = Guid.Parse(request.ModuleUid);
            Guid cardUid = Guid.Parse(request.Uuid);

            // valida la cadena: dispositivo -> tarjeta -> usuario -> misma institucion
            Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == moduleUid);
            if (device == null) return NotFound("Dispositivo no encontrado");

            NfcCard? card = await _context.NfcCards.FirstOrDefaultAsync(c => c.Id == cardUid);
            if (card == null) return NotFound("Tarjeta no encontrada");

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == card.UserId);
            if (user == null) return NotFound("Usuario no encontrado para esta tarjeta");
            if (!user.IsActive) return BadRequest("El usuario asociado a esta tarjeta no está activo");
            if (user.InstitutionId != device.InstitutionId)
                return BadRequest("El usuario no pertenece a la institución de este dispositivo");

            // guarda el registro raw independientemente de si hay horario o no
            var attendanceRecord = new AttendanceRecord
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
            };
            _context.AttendanceRecords.Add(attendanceRecord);
            await _context.SaveChangesAsync();

            // extrae fecha y hora local para buscar el horario correspondiente
            DateOnly dateOnly = DateOnly.FromDateTime(request.timeStampLocal);
            int dayOfWeek = (int)request.timeStampLocal.DayOfWeek;
            TimeOnly currentTime = TimeOnly.FromDateTime(request.timeStampLocal);

            // busca los grupos activos del usuario para filtrar horarios
            var groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == user.Id && gm.IsActive)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            if (groupIds.Count == 0)
                return Ok(new { found = true, message = "Asistencia registrada pero usuario no está en ningún grupo", attendanceRecordId = attendanceRecord.Id });

            // busca el horario activo que coincida con el dia y la hora actual
            Schedule? schedule = await _context.Schedules
                .Include(s => s.Group)
                .Where(s => groupIds.Contains(s.GroupId) &&
                           s.DayOfWeek == dayOfWeek &&
                           s.StartTime <= currentTime &&
                           s.EndTime >= currentTime &&
                           s.IsActive &&
                           s.Group.IsActive)
                .FirstOrDefaultAsync();

            if (schedule == null)
                return Ok(new
                {
                    found = true,
                    scheduleFound = false,
                    message = "Asistencia registrada pero no hay horario activo en esta hora",
                    attendanceRecordId = attendanceRecord.Id
                });

            // verifica si ya existe un DailyAttendance para este alumno/horario/dia
            DailyAttendance? existing = await _context.DailyAttendances
                .FirstOrDefaultAsync(da =>
                    da.Date == dateOnly &&
                    da.UserId == user.Id &&
                    da.ScheduleId == schedule.Id);

            string message;

            if (existing != null)
            {
                // el NFC nunca sobreescribe una decision ya tomada, el profesor manda
                message = "Presencia ya registrada";
            }
            else
            {
                // primera pasada: calcula si llego dentro de la tolerancia o tarde
                bool isLate = currentTime > schedule.StartTime.AddMinutes(schedule.LateToleranceMinutes);
                int status = isLate ? 3 : 0; // 3=Tarde, 0=Presente

                existing = new DailyAttendance
                {
                    Date = dateOnly,
                    UserId = user.Id,
                    ScheduleId = schedule.Id,
                    GroupId = schedule.GroupId,
                    ProfessorId = schedule.Group.ProfessorId,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.DailyAttendances.Add(existing);
                message = isLate ? "Alumno registrado como tarde" : "Alumno registrado como presente";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                found = true,
                scheduleFound = true,
                message,
                attendanceRecordId = attendanceRecord.Id,
                dailyAttendanceId = existing.Id,
                status = existing.Status,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("sync")]
    public async Task<ActionResult> SyncAttendance([FromBody] List<syncAttendance> syncRequest)
    {
        try
        {
            // acumula errores por item para devolver un reporte parcial al final
            var errors = new List<string>();
            var successCount = 0;

            foreach (syncAttendance r in syncRequest)
            {
                try
                {
                    Guid moduleUid = Guid.Parse(r.ModuleUid);

                    Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == moduleUid);
                    if (device == null) { errors.Add($"Dispositivo {r.ModuleUid} no encontrado"); continue; }
                    if (!device.IsActive) { errors.Add($"Dispositivo {r.ModuleUid} no está activo"); continue; }

                    // sync busca por HashUid porque el dispositivo manda el hash, no el id
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

                    // misma logica de horario que Register pero sin respuesta intermedia
                    DateOnly dateOnly = DateOnly.FromDateTime(r.timeStampLocal);
                    int dayOfWeek = (int)r.timeStampLocal.DayOfWeek;
                    TimeOnly currentTime = TimeOnly.FromDateTime(r.timeStampLocal);

                    var groupIds = await _context.GroupMembers
                        .Where(gm => gm.UserId == user.Id && gm.IsActive)
                        .Select(gm => gm.GroupId)
                        .ToListAsync();

                    if (groupIds.Count > 0)
                    {
                        Schedule? schedule = await _context.Schedules
                            .Include(s => s.Group)
                            .Where(s => groupIds.Contains(s.GroupId) &&
                                       s.DayOfWeek == dayOfWeek &&
                                       s.StartTime <= currentTime &&
                                       s.EndTime >= currentTime &&
                                       s.IsActive &&
                                       s.Group.IsActive)
                            .FirstOrDefaultAsync();

                        if (schedule != null)
                        {
                            var lastDailyAttendance = await _context.DailyAttendances
                                .Where(da => da.Date == dateOnly &&
                                            da.UserId == user.Id &&
                                            da.ScheduleId == schedule.Id)
                                .OrderByDescending(da => da.CreatedAt)
                                .FirstOrDefaultAsync();

                            if (lastDailyAttendance == null)
                            {
                                _context.DailyAttendances.Add(new DailyAttendance
                                {
                                    Date = DateOnly.FromDateTime(r.timeStampLocal),
                                    UserId = user.Id,
                                    ScheduleId = schedule.Id,
                                    GroupId = schedule.GroupId,
                                    ProfessorId = schedule.Group.ProfessorId,
                                    Status = 0,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                            else if (lastDailyAttendance.Status == 0)
                            {
                                lastDailyAttendance.Status = 1;
                                lastDailyAttendance.UpdatedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                _context.DailyAttendances.Add(new DailyAttendance
                                {
                                    Date = DateOnly.FromDateTime(r.timeStampLocal),
                                    UserId = user.Id,
                                    ScheduleId = schedule.Id,
                                    GroupId = schedule.GroupId,
                                    ProfessorId = schedule.Group.ProfessorId,
                                    Status = 0,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    successCount++;
                }
                catch (Exception itemEx)
                {
                    errors.Add($"Error procesando sincronización: {itemEx.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                processed = successCount,
                failed = errors.Count,
                errors
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("manual")]
    public async Task<ActionResult> manualChange([FromQuery] manual manualChange)
    {
        try
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == Guid.Parse(manualChange.userId));
            if (user == null) return NotFound("Usuario no encontrado");
            if (!user.IsActive) return BadRequest("El usuario asociado no está activo");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
            if (principal == null) return Unauthorized("Token invalido");

            // extrae el id del profesor desde el token para registrar quien hizo el cambio
            string? profesorId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (profesorId == null) return Unauthorized("No se pudo obtener el ID del profesor");

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
                RegisteredById = Guid.Parse(profesorId),
                CreatedAt = DateTime.UtcNow,
            });

            if (!string.IsNullOrEmpty(manualChange.scheduleId))
            {
                Schedule? schedule = await _context.Schedules
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.Id == Guid.Parse(manualChange.scheduleId) && s.IsActive);

                if (schedule != null)
                {
                    DateOnly dateOnly = DateOnly.FromDateTime(DateTime.Now);
                    var lastDailyAttendance = await _context.DailyAttendances
                        .Where(da => da.Date == dateOnly &&
                                    da.UserId == Guid.Parse(manualChange.userId) &&
                                    da.ScheduleId == schedule.Id)
                        .OrderByDescending(da => da.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (lastDailyAttendance == null)
                    {
                        _context.DailyAttendances.Add(new DailyAttendance
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now),
                            UserId = Guid.Parse(manualChange.userId),
                            ScheduleId = schedule.Id,
                            GroupId = schedule.GroupId,
                            ProfessorId = schedule.Group.ProfessorId,
                            Status = manualChange.status,
                            ModifiedById = Guid.Parse(profesorId),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        lastDailyAttendance.Status = manualChange.status;
                        lastDailyAttendance.ModifiedById = Guid.Parse(profesorId);
                        lastDailyAttendance.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                found = true,
                message = "Asistencia registrada correctamente"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("All")]
    public async Task<ActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        try
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;

            int totalCount = await _context.AttendanceRecords.CountAsync();

            List<AttendanceRecord> records = await _context.AttendanceRecords
                .Include(a => a.User)
                .Include(a => a.Device)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                records = records.Select(r => new
                {
                    id = r.Id,
                    timestampUtc = r.TimestampUtc,
                    user = r.User == null ? null : new { firstName = r.User.FirstName },
                    device = r.Device == null ? null : new { name = r.Device.Name }
                }),
                total = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / limit),
                page,
                limit
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("today")]
    public async Task<ActionResult> TodayAttendance([FromQuery] todayAttendance request)
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult> historyAttendance([FromQuery] historyAttendance attendance)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
            if (principal == null) return Unauthorized("Token inválido");

            string? callerIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(callerIdStr, out var callerId))
                return Unauthorized("Token no contiene un ID válido");

            int.TryParse(principal.FindFirst("role")?.Value, out int callerRole);
            bool isAdminOrAbove = callerRole <= 1; // 0=SuperAdmin, 1=Admin

            if (!Guid.TryParse(attendance.groupId, out var groupId))
                return BadRequest("groupId no es un Guid válido");

            Group? group = await _context.Groups
                .Include(g => g.Members.Where(m => m.IsActive))
                    .ThenInclude(m => m.User)
                .Include(g => g.Schedules.Where(s => s.IsActive))
                .FirstOrDefaultAsync(g => g.Id == groupId && g.IsActive);

            if (group == null) return NotFound("Grupo no encontrado");
            // admins y superadmins pueden ver cualquier grupo; profesores solo el suyo
            if (!isAdminOrAbove && group.ProfessorId != callerId)
                return Forbid();

            DateOnly fromDate = DateOnly.FromDateTime(attendance.from.Date);
            DateOnly toDate = DateOnly.FromDateTime(attendance.to.Date);

            int page = attendance.page < 1 ? 1 : attendance.page;
            int limit = attendance.limit < 1 ? 50 : attendance.limit;

            // consulta el resumen diario (procesado) y los registros raw del dispositivo por separado
            var attendances = await _context.DailyAttendances
                .Include(da => da.User)
                .Include(da => da.Schedule)
                .Where(da =>
                    da.GroupId == groupId &&
                    da.Date >= fromDate &&
                    da.Date <= toDate)
                .OrderBy(da => da.Date)
                .ThenBy(da => da.User.FirstName)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var rawRecords = await _context.AttendanceRecords
                .Where(ar =>
                    ar.TimestampLocal.Date >= attendance.from.Date &&
                    ar.TimestampLocal.Date <= attendance.to.Date &&
                    _context.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == ar.UserId && gm.IsActive))
                .OrderBy(ar => ar.TimestampLocal)
                .ToListAsync();

            return Ok(new
            {
                codigoError = 0,
                groupId = group.Id,
                groupName = group.Name,
                from = fromDate.ToString("yyyy-MM-dd"),
                to = toDate.ToString("yyyy-MM-dd"),
                page,
                limit,
                total = attendances.Count,
                attendances = attendances.Select(da => new
                {
                    id = da.Id,
                    date = da.Date.ToString("yyyy-MM-dd"),
                    userId = da.UserId,
                    userName = $"{da.User.FirstName} {da.User.LastName}",
                    scheduleId = da.ScheduleId,
                    startTime = da.Schedule.StartTime.ToString("HH:mm"),
                    endTime = da.Schedule.EndTime.ToString("HH:mm"),
                    status = da.Status,
                    modifiedById = da.ModifiedById,
                    createdAt = da.CreatedAt,
                    updatedAt = da.UpdatedAt
                }),
                rawRecords = rawRecords.Select(ar => new
                {
                    id = ar.Id,
                    userId = ar.UserId,
                    timestampLocal = ar.TimestampLocal,
                    timestampUtc = ar.TimestampUtc,
                    type = ar.Type,
                    status = ar.Status
                }),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("myHistory")]
    public async Task<ActionResult> myHistory([FromQuery] myHistoryRequest request)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
            if (principal == null) return Unauthorized("Token inválido");

            string? userIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token no contiene un ID válido");

            int page = request.page < 1 ? 1 : request.page;
            int limit = request.limit < 1 ? 50 : request.limit;

            DateOnly fromDate = DateOnly.FromDateTime(request.from.Date);
            DateOnly toDate = DateOnly.FromDateTime(request.to.Date);

            var attendances = await _context.DailyAttendances
                .Include(da => da.Schedule)
                .Include(da => da.Group)
                .Where(da => da.UserId == userId && da.Date >= fromDate && da.Date <= toDate)
                .OrderByDescending(da => da.Date)
                .ThenBy(da => da.Schedule.StartTime)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new
            {
                codigoError = 0,
                from = fromDate.ToString("yyyy-MM-dd"),
                to = toDate.ToString("yyyy-MM-dd"),
                page,
                limit,
                total = attendances.Count,
                attendances = attendances.Select(da => new
                {
                    id = da.Id,
                    date = da.Date.ToString("yyyy-MM-dd"),
                    groupId = da.GroupId,
                    groupName = da.Group.Name,
                    scheduleId = da.ScheduleId,
                    startTime = da.Schedule.StartTime.ToString("HH:mm"),
                    endTime = da.Schedule.EndTime.ToString("HH:mm"),
                    status = da.Status,
                    createdAt = da.CreatedAt
                }),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("daily")]
    public async Task<ActionResult> UpsertDailyAttendance([FromBody] UpsertDailyRequest request)
    {
        try
        {
            Console.WriteLine($"[daily/upsert] >>> Inicio. userId={request.UserId} groupId={request.GroupId} scheduleId={request.ScheduleId} date={request.Date:yyyy-MM-dd} status={request.Status}");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
            if (principal == null) return Unauthorized("Token inválido o expirado");

            string? callerIdStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!Guid.TryParse(callerIdStr, out var callerId))
                return Unauthorized("Token no contiene un ID válido");

            int.TryParse(principal.FindFirst("role")?.Value, out int callerRole);
            bool isAdminOrAbove = callerRole <= 1; // 0=SuperAdmin, 1=Admin

            if (!Guid.TryParse(request.UserId, out var userId))
                return BadRequest("userId inválido");

            if (request.Status < 0 || request.Status > 3)
                return BadRequest("Status inválido. Valores: 0=Presente, 1=Ausente, 2=Justificado, 3=Tarde");

            DateOnly date = DateOnly.FromDateTime(request.Date.Date);
            int dayOfWeek = (int)request.Date.DayOfWeek;

            Schedule? schedule = null;

            // resuelve el horario: primero intenta por scheduleId directo, sino por groupId
            if (!string.IsNullOrEmpty(request.ScheduleId) && Guid.TryParse(request.ScheduleId, out var scheduleId))
            {
                schedule = await _context.Schedules
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.Id == scheduleId && s.IsActive);
            }
            else if (!string.IsNullOrEmpty(request.GroupId) && Guid.TryParse(request.GroupId, out var groupId))
            {
                var studentGroupIds = await _context.GroupMembers
                    .Where(gm => gm.UserId == userId && gm.GroupId == groupId && gm.IsActive)
                    .Select(gm => gm.GroupId)
                    .ToListAsync();

                // intenta coincidir con el dia de la semana de la fecha enviada
                schedule = await _context.Schedules
                    .Include(s => s.Group)
                    .Where(s => studentGroupIds.Contains(s.GroupId) && s.DayOfWeek == dayOfWeek && s.IsActive && s.Group.IsActive)
                    .FirstOrDefaultAsync();

                // fallback: si no hay horario ese dia, toma cualquier horario activo del grupo
                if (schedule == null)
                {
                    schedule = await _context.Schedules
                        .Include(s => s.Group)
                        .Where(s => studentGroupIds.Contains(s.GroupId) && s.IsActive && s.Group.IsActive)
                        .FirstOrDefaultAsync();
                }
            }

            if (schedule == null) return NotFound("El grupo no tiene ningún horario activo");
            // admins y superadmins pueden editar cualquier grupo; profesores solo el suyo
            if (!isAdminOrAbove && schedule.Group.ProfessorId != callerId) return Forbid();

            User? student = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (student == null) return NotFound("Alumno no encontrado");

            DailyAttendance? existing = await _context.DailyAttendances
                .FirstOrDefaultAsync(da =>
                    da.UserId == userId &&
                    da.ScheduleId == schedule.Id &&
                    da.Date == date);

            // upsert: actualiza si ya existe, crea si no
            bool created = false;

            if (existing != null)
            {
                existing.Status = request.Status;
                existing.ModifiedById = callerId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new DailyAttendance
                {
                    Date = date,
                    UserId = userId,
                    ScheduleId = schedule.Id,
                    GroupId = schedule.GroupId,
                    ProfessorId = schedule.Group.ProfessorId,
                    Status = request.Status,
                    ModifiedById = callerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.DailyAttendances.Add(existing);
                created = true;
            }

            // siempre genera un AttendanceRecord manual como trazabilidad del cambio
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                UserId = userId,
                DeviceId = null,
                TimestampUtc = DateTime.UtcNow,
                TimestampLocal = DateTime.Now,
                Type = 1,
                Status = request.Status,
                NfcHash = null,
                IsSynced = true,
                RegisteredById = callerId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                codigoError = 0,
                created,
                id = existing.Id,
                userId = existing.UserId,
                scheduleId = existing.ScheduleId,
                date = existing.Date.ToString("yyyy-MM-dd"),
                status = existing.Status,
                modifiedById = existing.ModifiedById,
                updatedAt = existing.UpdatedAt,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("daily/group/{groupId}")]
    public async Task<ActionResult> GetDailyAttendanceByGroup(string groupId, [FromQuery] DateTime? date)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
            if (principal == null) return Unauthorized("Token inválido o expirado");

            if (!Guid.TryParse(groupId, out var gid))
                return BadRequest("groupId inválido");

            Group? group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == gid && g.IsActive);
            if (group == null) return NotFound("Grupo no encontrado");

            DateTime targetDate = (date ?? DateTime.Now).Date;

            var attendances = await _context.DailyAttendances
                .Include(da => da.User)
                .Include(da => da.Schedule)
                .Where(da => da.GroupId == gid && da.Date == DateOnly.FromDateTime(targetDate))
                .OrderBy(da => da.User.FirstName)
                .ThenBy(da => da.User.LastName)
                .ToListAsync();

            return Ok(new
            {
                codigoError = 0,
                groupId = gid,
                date = targetDate,
                totalRecords = attendances.Count,
                attendances = attendances.Select(da => new
                {
                    id = da.Id,
                    userId = da.UserId,
                    userName = $"{da.User.FirstName} {da.User.LastName}",
                    scheduleId = da.ScheduleId,
                    status = da.Status,
                    createdAt = da.CreatedAt,
                    updatedAt = da.UpdatedAt
                }),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
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
    public string groupId { get; set; } = string.Empty;
    public int page { get; set; } = 1;
    public int limit { get; set; } = 50;
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
    public string notes { get; set; } = string.Empty;
    public string? scheduleId { get; set; } // Opcional para asociar a horario
}

public class todayAttendance
{
    public string groupId { get; set; } = string.Empty;
    public string institutionId { get; set; } = string.Empty;
}

public class myHistoryRequest
{
    public DateTime from { get; set; } = DateTime.Now.AddDays(-30);
    public DateTime to { get; set; } = DateTime.Now;
    public int page { get; set; } = 1;
    public int limit { get; set; } = 50;
}

public class UpdateDailyStatusRequest
{
    public int Status { get; set; }
}

public class UpsertDailyRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? ScheduleId { get; set; }   // opcional si se manda GroupId
    public string? GroupId { get; set; }       // alternativa a ScheduleId
    public DateTime Date { get; set; } = DateTime.Now;
    public int Status { get; set; }
}