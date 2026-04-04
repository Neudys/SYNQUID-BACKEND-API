using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly SynquidDbContext _context;

    public AttendanceController(SynquidDbContext context)
    {
        _context = context;
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

        Device device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == moduleUid);
        if (device == null) return NotFound("Dispositivo no encontrado");

        NfcCard card = await _context.NfcCards.FirstOrDefaultAsync(c => c.Id == cardUid);
        if (card == null) return NotFound("Tarjeta no encontrada");

        User user = await _context.Users .FirstOrDefaultAsync(u => u.InstitutionId == device.InstitutionId);
        if (user == null) return NotFound("Usuario no encontrado en el instituto a quien le pertenece el dispositivo");

        _context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = user.Id,
            DeviceId = device.Id,
            TimestampUtc = request.date
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            found = true,
            message = "Tarjeta registrada correctamente"
        });
    }

}

// El objeto que recibe del body
public class CheckRequest
{
    public string Uid { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Uuid { get; set; } = string.Empty;
    public string ModuleUid { get; set; } = string.Empty;
    public DateTime date { get; set; } = DateTime.Now;

}