using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;
using System.Security.Claims;
using Synquid.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
    public async Task<ActionResult<User>> perfilUser([FromBody] requestUser request)
    {
        ClaimsPrincipal principal = ValidateToken(request.token);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        User? u = await _context.Users.FirstOrDefaultAsync(x => x.Id == Guid.Parse(userId ?? ""));

        if (u == null) return NotFound("Usuario no encontrado");
        return Ok(u);
    }

    [HttpPost]
    public Task<ActionResult> saveUser([FromQuery] postUser user)
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal principal = ValidateToken(authHeader);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (principal == null) return Task.FromResult<ActionResult>(Unauthorized("Token invalido"));

        User u = new User{
            FirstName = user.name,
            LastName = user.lastName,
            Email = user.email,
            Role = user.rol,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.password)
        };

        return Task.FromResult<ActionResult>(Ok(new { 
            mensaje = "Usuario Creado correctamente"  
        }));
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> Users([FromQuery] int page = 1)
    {
        List<User> usuarios = await _context.Users
            .OrderBy(i => i.FirstName)
            .Skip((page -1 ) * 20)
            .ToListAsync();
        return Ok(usuarios);
    }

    [HttpPost("validarToken")]
    private ClaimsPrincipal ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!);

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["JwtSettings:Audience"],
                ValidateLifetime = true
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
    public class requestUser 
    {
        public string token { get; set; } = string.Empty; 
    }

    public class postUser 
    {
        public string name { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public int rol { get; set; } = 0;
    }

}

