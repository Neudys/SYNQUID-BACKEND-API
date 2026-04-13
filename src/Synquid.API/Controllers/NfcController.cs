using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
public class NfcController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public NfcController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult> RegisterNfc([FromBody] registerNfc nfc)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        NfcCard? existing = await _context.NfcCards.FirstOrDefaultAsync(x => x.HashUid == nfc.hashUid);

        if (existing != null)
            return BadRequest("Esta tarjeta NFC ya está registrada");

        NfcCard card = new NfcCard
        {
            Id = Guid.NewGuid(),
            HashUid = nfc.hashUid,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await _context.NfcCards.AddAsync(card);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            message = "Tarjeta NFC registrada correctamente",
            nfcId = card.Id,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{uid}")]
    public async Task<ActionResult> GetNfcByUid(string uid)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        NfcCard? card = await _context.NfcCards
            .Include(c => c.User)
            .FirstOrDefaultAsync(x => x.HashUid == uid);

        if (card == null)
            return NotFound("Tarjeta NFC no encontrada");

        return Ok(new
        {
            codigoError = 0,
            nfc = card,
            userId = card.UserId,
            userName = card.User?.FirstName + " " + card.User?.LastName,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNfc(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        NfcCard? card = await _context.NfcCards.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (card == null)
            return NotFound("Tarjeta NFC no encontrada");

        _context.NfcCards.Remove(card);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            message = "Tarjeta NFC eliminada correctamente",
            timestamp = DateTime.UtcNow
        });
    }

}

public class registerNfc
{
    public string hashUid { get; set; } = string.Empty;
}