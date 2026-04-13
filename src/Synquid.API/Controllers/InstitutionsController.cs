using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.API.Extensions;
using Synquid.Application.Interfaces;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstitutionsController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;

    public InstitutionsController(SynquidDbContext context, IConfiguration config, IAuditService audit)
    {
        _context = context;
        _config = config;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<List<Institution>>> GetAll()
    {
        return await _context.Institutions
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Institution>> GetById(Guid id)
    {
        Institution? institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return NotFound();
        return institution;
    }

    [HttpPost]
    public async Task<ActionResult<Institution>> Create(Institution institution)
    {
        institution.Id = Guid.NewGuid();
        institution.CreatedAt = DateTime.UtcNow;

        _context.Institutions.Add(institution);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = institution.Id }, institution);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Institution updated)
    {
        Institution? institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return NotFound();

        institution.Name = updated.Name;
        institution.Address = updated.Address;
        institution.Phone = updated.Phone;
        institution.ContactEmail = updated.ContactEmail;
        institution.Type = updated.Type;
        institution.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Institution? institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return NotFound();

        institution.IsActive = false;
        institution.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/users")]
    public async Task<ActionResult> GetInstitutionUsers(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Institution? institution = await _context.Institutions.FindAsync(Guid.Parse(id));

        if (institution == null)
            return NotFound("Institución no encontrada");

        List<User> users = await _context.Users
            .Where(u => u.InstitutionId == Guid.Parse(id) && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        return Ok(new
        {
            codigoError = 0,
            institutionId = id,
            totalUsers = users.Count,
            users = users,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("{id}/users")]
    public async Task<ActionResult> AssignUserToInstitution(string id, [FromBody] assignUserToInstitution request)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Institution? institution = await _context.Institutions.FindAsync(Guid.Parse(id));

        if (institution == null)
            return NotFound("Institución no encontrada");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(request.userId));

        if (user == null)
            return NotFound("Usuario no encontrado");

        user.InstitutionId = Guid.Parse(id);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            message = "Usuario asignado a la institución correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("{id}/users/{userId}")]
    public async Task<ActionResult> UnassignUserFromInstitution(string id, string userId)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Institution? institution = await _context.Institutions.FindAsync(Guid.Parse(id));

        if (institution == null)
            return NotFound("Institución no encontrada");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(userId));

        if (user == null)
            return NotFound("Usuario no encontrado");

        user.InstitutionId = null;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            message = "Usuario desasignado de la institución correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{id}/devices")]
    public async Task<ActionResult> GetInstitutionDevices(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Institution? institution = await _context.Institutions.FindAsync(Guid.Parse(id));

        if (institution == null)
            return NotFound("Institución no encontrada");

        List<Device> devices = await _context.Devices
            .Where(d => d.InstitutionId == Guid.Parse(id) && d.IsActive)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(new
        {
            codigoError = 0,
            institutionId = id,
            totalDevices = devices.Count,
            devices = devices,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{id}/attendance")]
    public async Task<ActionResult> GetInstitutionAttendance(string id, [FromQuery] int page = 1)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        Institution? institution = await _context.Institutions.FindAsync(Guid.Parse(id));

        if (institution == null)
            return NotFound("Institución no encontrada");

        List<AttendanceRecord> records = await _context.AttendanceRecords
            .Include(a => a.Device)
            .Where(a => a.Device.InstitutionId == Guid.Parse(id))
            .OrderByDescending(a => a.TimestampUtc)
            .Skip((page - 1) * 20)
            .Take(20)
            .ToListAsync();

        int total = await _context.AttendanceRecords
            .Include(a => a.Device)
            .Where(a => a.Device.InstitutionId == Guid.Parse(id))
            .CountAsync();

        return Ok(new
        {
            codigoError = 0,
            institutionId = id,
            page = page,
            totalRecords = total,
            records = records,
            timestamp = DateTime.UtcNow
        });
    }
}

public class assignUserToInstitution
{
    public string userId { get; set; } = string.Empty;
}