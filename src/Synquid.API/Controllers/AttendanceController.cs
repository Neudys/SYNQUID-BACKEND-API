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

        List<User> users = await _context.Users.ToListAsync();

        if (users.Any(u => u.Email == request.Email))
        {
            return BadRequest(new
            {
                message = "El correo ya está registrado"
            });
        } 

        return Ok(new
        {
            found = true,
            message = "Tarjeta encontrada"
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
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}