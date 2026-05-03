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
public class StudentController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public StudentController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet("myGroups")]
    public async Task<ActionResult> GetMyGroups()
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? roleStr = principal.FindFirst("role")?.Value;

            if (!int.TryParse(roleStr, out int role) || role != 3)
                return Forbid();

            if (!Guid.TryParse(userId, out Guid uid))
                return BadRequest("userId inválido");

            List<GroupMember> memberships = await _context.GroupMembers
                .Include(gm => gm.Group)
                    .ThenInclude(g => g.Professor)
                .Where(gm => gm.UserId == uid && gm.IsActive && gm.Group.IsActive)
                .ToListAsync();

            var data = memberships.Select(gm => new
            {
                groupId = gm.Group.Id,
                groupName = gm.Group.Name,
                level = gm.Group.Level,
                institutionId = gm.Group.InstitutionId,
                professorId = gm.Group.ProfessorId,
                professorName = $"{gm.Group.Professor.FirstName} {gm.Group.Professor.LastName}"
            });

            return Ok(new
            {
                codigoError = 0,
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

    [HttpGet("schedule")]
    public async Task<ActionResult> GetSchedule([FromQuery] string? date)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? roleStr = principal.FindFirst("role")?.Value;

            if (!int.TryParse(roleStr, out int role) || role != 3)
                return Forbid();

            if (!Guid.TryParse(userId, out Guid uid))
                return BadRequest("userId inválido");

            // si no mandan fecha usa hoy como default
            DateTime targetDate = date != null && DateTime.TryParse(date, out DateTime parsed)
                ? parsed
                : DateTime.UtcNow.Date;

            // extrae el dia de la semana para filtrar los horarios que corresponden
            int dayOfWeek = (int)targetDate.DayOfWeek;

            List<Guid> groupIds = await _context.GroupMembers
                .Where(gm => gm.UserId == uid && gm.IsActive)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            List<Schedule> slots = await _context.Schedules
                .Include(s => s.Group)
                    .ThenInclude(g => g.Professor)
                .Where(s => groupIds.Contains(s.GroupId) && s.DayOfWeek == dayOfWeek && s.IsActive && s.Group.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var data = slots.Select(s => new
            {
                scheduleId = s.Id,
                groupId = s.GroupId,
                groupName = s.Group.Name,
                level = s.Group.Level,
                professorId = s.Group.ProfessorId,
                professorName = $"{s.Group.Professor.FirstName} {s.Group.Professor.LastName}",
                dayOfWeek = s.DayOfWeek,
                startTime = s.StartTime.ToString("HH:mm"),
                endTime = s.EndTime.ToString("HH:mm"),
                lateToleranceMinutes = s.LateToleranceMinutes
            });

            return Ok(new
            {
                codigoError = 0,
                date = targetDate.ToString("yyyy-MM-dd"),
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
