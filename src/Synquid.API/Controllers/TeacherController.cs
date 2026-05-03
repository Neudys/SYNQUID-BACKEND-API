using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.API.Extensions;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeacherController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public TeacherController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet("myGroups")]
    public async Task<ActionResult> GetMyGroups([FromQuery] string? teacherId)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? roleStr = principal.FindFirst("role")?.Value;

            if (!int.TryParse(roleStr, out int role) || role > 2)
                return Forbid();

            if (role == 2 && teacherId != null && teacherId != userId)
                return Forbid();

            Guid filterBy = role == 2
                ? Guid.Parse(userId!)
                : (teacherId != null ? Guid.Parse(teacherId) : Guid.Empty);

            List<Group> groups = await _context.Groups
                .Where(g => g.IsActive && (filterBy == Guid.Empty || g.ProfessorId == filterBy))
                .ToListAsync();

            var data = new List<object>();

            foreach (Group g in groups)
            {
                int studentCount = await _context.GroupMembers
                    .CountAsync(gm => gm.GroupId == g.Id && gm.IsActive);

                data.Add(new
                {
                    groupId = g.Id,
                    groupName = g.Name,
                    institutionId = g.InstitutionId,
                    studentCount,
                    description = g.Level
                });
            }

            return Ok(new
            {
                codigoError = 0,
                data,
                total = data.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("groups/{groupId}/students")]
    public async Task<ActionResult> GetGroupStudents(string groupId, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? roleStr = principal.FindFirst("role")?.Value;

            if (!int.TryParse(roleStr, out int role) || role > 2)
                return Forbid();

            if (!Guid.TryParse(groupId, out Guid gid))
                return BadRequest("groupId inválido");

            Group? group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == gid && g.IsActive);
            if (group == null)
                return NotFound("Grupo no encontrado");

            if (role == 2 && group.ProfessorId != Guid.Parse(userId!))
                return Forbid();

            int total = await _context.GroupMembers
                .CountAsync(gm => gm.GroupId == gid && gm.IsActive);

            List<GroupMember> members = await _context.GroupMembers
                .Include(gm => gm.User)
                    .ThenInclude(u => u.NfcCards)
                .Where(gm => gm.GroupId == gid && gm.IsActive && gm.User.IsActive)
                .OrderBy(gm => gm.User.FirstName).ThenBy(gm => gm.User.LastName)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var data = members.Select(gm =>
            {
                NfcCard? nfc = gm.User.NfcCards.FirstOrDefault(n => n.IsActive);
                return new
                {
                    userId = gm.User.Id,
                    firstName = gm.User.FirstName,
                    lastName = gm.User.LastName,
                    email = gm.User.Email,
                    nfcCardId = nfc?.Id,
                    nfcCardUid = nfc?.HashUid
                };
            });

            return Ok(new
            {
                codigoError = 0,
                data,
                total,
                page,
                limit,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("schedule")]
    public async Task<ActionResult> GetSchedule([FromQuery] string? from, [FromQuery] string? to)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? roleStr = principal.FindFirst("role")?.Value;

            if (!int.TryParse(roleStr, out int role) || role > 2)
                return Forbid();

            if (!Guid.TryParse(userId, out Guid uid))
                return BadRequest("userId inválido");

            DateTime fromDate = from != null && DateTime.TryParse(from, out DateTime parsedFrom)
                ? parsedFrom.Date
                : DateTime.UtcNow.Date;

            DateTime toDate = to != null && DateTime.TryParse(to, out DateTime parsedTo)
                ? parsedTo.Date
                : fromDate.AddDays(6);

            // Calcula los días de la semana presentes en el rango
            List<int> daysInRange = new List<int>();
            for (DateTime d = fromDate; d <= toDate; d = d.AddDays(1))
                daysInRange.Add((int)d.DayOfWeek);

            daysInRange = daysInRange.Distinct().ToList();

            List<Schedule> slots = await _context.Schedules
                .Include(s => s.Group)
                .Where(s => s.Group.ProfessorId == uid && s.IsActive && s.Group.IsActive && daysInRange.Contains(s.DayOfWeek))
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .ToListAsync();

            var data = slots.Select(s => new
            {
                scheduleId = s.Id,
                groupId = s.GroupId,
                groupName = s.Group.Name,
                level = s.Group.Level,
                institutionId = s.Group.InstitutionId,
                dayOfWeek = s.DayOfWeek,
                startTime = s.StartTime.ToString("HH:mm"),
                endTime = s.EndTime.ToString("HH:mm"),
                lateToleranceMinutes = s.LateToleranceMinutes
            });

            return Ok(new
            {
                codigoError = 0,
                from = fromDate.ToString("yyyy-MM-dd"),
                to = toDate.ToString("yyyy-MM-dd"),
                data,
                total = data.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}