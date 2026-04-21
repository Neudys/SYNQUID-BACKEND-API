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
using System.Diagnostics;

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
        try
        {
            var token = authorization?.Replace("Bearer ", "").Trim();
            if (string.IsNullOrEmpty(token))
                return Unauthorized("Token requerido");

            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(token, _config);

            if (principal == null)
                return Unauthorized("Token inválido");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
                return Unauthorized("Token no contiene ID válido");

            User? u = await _context.Users.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (u == null)
                return NotFound("Usuario no encontrado");

            return Ok(UserResponseDto.FromUser(u));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult> SaveUser([FromBody] postUser user)
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);
            if (principal == null)
                return Unauthorized("Token inválido");

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.email);
            if (existingUser != null)
                return BadRequest("El email ya está registrado");

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
                userId = u.Id,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers([FromQuery] int page = 1)
    {
        try
        {
            if (page < 1)
                return BadRequest("El número de página debe ser mayor a 0");

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUserById(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest("ID inválido");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

            if (user == null)
                return NotFound("Usuario no encontrado");

            return Ok(new
            {
                codigoError = 0,
                userData = UserResponseDto.FromUser(user),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser(string id, [FromBody] updateUser update)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest("ID inválido");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

            if (user == null)
                return NotFound("Usuario no encontrado");

            if (!string.IsNullOrEmpty(update.email) && update.email != user.Email)
            {
                var emailExists = await _context.Users.FirstOrDefaultAsync(u => u.Email == update.email && u.Id != userId);
                if (emailExists != null)
                    return BadRequest("El email ya está registrado");
            }

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest("ID inválido");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

            if (user == null)
                return NotFound("Usuario no encontrado");

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPatch("{id}/role")]
    public async Task<ActionResult> UpdateUserRole(string id, [FromBody] updateRole roleUpdate)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest("ID inválido");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? roleStr = principal.FindFirst("role")?.Value;
            if (string.IsNullOrEmpty(roleStr) ||
                !int.TryParse(roleStr, out var userRole) ||
                (userRole != UserRoles.SuperAdmin && userRole != UserRoles.Admin))
            {
                return Forbid("Solo administradores pueden cambiar roles");
            }

            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

            if (user == null)
                return NotFound("Usuario no encontrado");

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("{id}/nfc")]
    public async Task<ActionResult> AssignNfcCard(string id, [FromBody] assignNfc nfcData)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest("ID de usuario inválido");

            if (!Guid.TryParse(nfcData.nfcCardId, out var nfcCardId))
                return BadRequest("ID de tarjeta NFC inválido");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

            if (user == null)
                return NotFound("Usuario no encontrado");

            NfcCard? nfcCard = await _context.NfcCards.FirstOrDefaultAsync(x => x.Id == nfcCardId && x.IsActive);

            if (nfcCard == null)
                return NotFound("Tarjeta NFC no encontrada");

            if (nfcCard.UserId != null && nfcCard.UserId != userId)
                return BadRequest("La tarjeta NFC ya está asignada a otro usuario");

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id}/nfc")]
    public async Task<ActionResult> UnassignNfcCard(string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
                return BadRequest("ID inválido");

            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

            if (user == null)
                return NotFound("Usuario no encontrado");

            NfcCard? nfcCard = await _context.NfcCards.FirstOrDefaultAsync(x => x.UserId == user.Id && x.IsActive);

            if (nfcCard == null)
                return NotFound("Tarjeta NFC no asignada al usuario");

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
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
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? email { get; set; }
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