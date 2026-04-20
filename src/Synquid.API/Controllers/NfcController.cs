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

    [HttpPost("AssignCard")]
    public async Task<ActionResult> AssignCard([FromBody] AssignCardRequest request)
    {
        // Validaciones básicas
        if (string.IsNullOrEmpty(request.Uuid))
            return BadRequest("UUID de tarjeta requerido");
        if (string.IsNullOrEmpty(request.Email))
            return BadRequest("Email requerido");

        Guid cardUid = Guid.Parse(request.Uuid);
        Guid institutionId = Guid.Parse(request.InstitutionId);


        Institution? institution = await _context.Institutions.FirstOrDefaultAsync(i => i.Id == institutionId);
        if (institution == null) return NotFound("Institución no encontrada");


        User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.InstitutionId == institutionId);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.Name,
                Email = request.Email,
                InstitutionId = institutionId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }


        NfcCard? card = await _context.NfcCards.FirstOrDefaultAsync(c => c.Id == cardUid);

        if (card == null)
        {

            card = new NfcCard
            {
                Id = cardUid,
                HashUid = HashUid(request.Uuid),
                UserId = user.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.NfcCards.Add(card);
        }
        else
        {

            card.UserId = user.Id;
            card.IsActive = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            found = true,
            message = "Tarjeta asignada correctamente",
            userId = user.Id,
            cardId = card.Id
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


    private string HashUid(string uid)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(uid));
            return Convert.ToBase64String(hashedBytes);
        }
    }
    public class AssignCardRequest
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string InstitutionId { get; set; }
    }
    public class registerNfc
    {
        public string hashUid { get; set; } = string.Empty;
    }
}