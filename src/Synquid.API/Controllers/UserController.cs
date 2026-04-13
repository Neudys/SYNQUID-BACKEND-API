using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;
using System.Security.Claims;
using Synquid.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Synquid.Application.DTOs;
using Synquid.API.Extensions;
using Synquid.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
namespace Synquid.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;
    public UserController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    [HttpGet("me")]
    public async Task<ActionResult<UserResponseDto>> PerfilUser([FromHeader(Name = "Authorization")] string authorization)
    {
        var token = authorization?.Replace("Bearer ", "").Trim();
        if (string.IsNullOrEmpty(token))
            return Unauthorized("Token requerido");

        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(token, _config);
        if (principal == null)
            return Unauthorized("Token inválido");

        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        User? u = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(userId ?? ""));

        if (u == null) return NotFound("Usuario no encontrado");

        return Ok(UserResponseDto.FromUser(u));
    }
    [HttpPost]
    public async Task<ActionResult> saveUser([FromBody] postUser user)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
        if (principal == null) return Unauthorized("Token inválido");
        User u = new User
        {
            Id = Guid.NewGuid(),
            FirstName = user.name,
            LastName = user.lastName,
            Email = user.email,
            Role = user.rol,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(u);
        await _context.SaveChangesAsync();
        return Ok(new
        {
            codigoError = 0,
            mensaje = "Usuario creado correctamente",
            timestamp = DateTime.UtcNow
        });
    }
    [HttpGet]
    public async Task<ActionResult<List<User>>> Users([FromQuery] int page = 1)
    {
        List<User> usuarios = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(i => i.FirstName)
            .Skip((page - 1) * 20)
            .Take(20)
            .ToListAsync();
        int total = await _context.Users.CountAsync(u => u.IsActive);
        return Ok(new
        {
            codigoError = 0,
            page = page,
            totalUsers = total,
            users = usuarios,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserById(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null) return NotFound("Usuario no encontrado");

        return Ok(new
        {
            codigoError = 0,
            userData = UserResponseDto.FromUser(user),
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser(string id, [FromBody] updateUser update)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null) return NotFound("Usuario no encontrado");

        user.FirstName = update.firstName ?? user.FirstName;
        user.LastName = update.lastName ?? user.LastName;
        user.Email = update.email ?? user.Email;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Usuario actualizado correctamente",
            timestamp = DateTime.UtcNow
        });
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null) return NotFound("Usuario no encontrado");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Usuario eliminado correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPatch("{id}/role")]
    public async Task<ActionResult> UpdateUserRole(string id, [FromBody] updateRole roleUpdate)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        string? roleStr = principal.FindFirst("role")?.Value;
        if (string.IsNullOrEmpty(roleStr) ||
            (int.Parse(roleStr) != UserRoles.SuperAdmin && int.Parse(roleStr) != UserRoles.Admin))
        {
            return Forbid("Solo administradores pueden cambiar roles");
        }

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null) return NotFound("Usuario no encontrado");

        user.Role = roleUpdate.role;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Rol actualizado correctamente",
            timestamp = DateTime.UtcNow
        });
    }
    [HttpPost("{id}/nfc")]
    public async Task<ActionResult> AssignNfcCard(string id, [FromBody] assignNfc nfcData)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null) return NotFound("Usuario no encontrado");

        NfcCard? nfcCard = await _context.NfcCards.FirstOrDefaultAsync(x => x.Id == Guid.Parse(nfcData.nfcCardId));

        if (nfcCard == null) return NotFound("Tarjeta NFC no encontrada");

        nfcCard.UserId = user.Id;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Tarjeta NFC asignada correctamente",
            timestamp = DateTime.UtcNow
        });
    }
    [HttpDelete("{id}/nfc")]
    public async Task<ActionResult> UnassignNfcCard(string id)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

        if (user == null) return NotFound("Usuario no encontrado");

        NfcCard? nfcCard = await _context.NfcCards.FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (nfcCard == null) return NotFound("Tarjeta NFC no asignada al usuario");

        nfcCard.IsActive = false;
        nfcCard.UserId = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            codigoError = 0,
            mensaje = "Tarjeta NFC desasignada correctamente",
            timestamp = DateTime.UtcNow
        });
    }
    
    public class postUser
    {
        public string name { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public int rol { get; set; } = 0;
    }
    public class updateUser
    {
        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }
    public class updateRole
    {
        public int role { get; set; } = 0;
    }
    public class assignNfc
    {
        public string nfcCardId { get; set; } = string.Empty;
    }
}