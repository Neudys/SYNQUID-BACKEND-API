using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class authController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public authController(SynquidDbContext context)
    {
        _context = context;
    }
    public authController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("/Register")]
    public async Task<ActionResult> Register([FromBody] User usuario)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(d => d.Email == usuario.Email);
        if (user != null) return BadRequest("El Email ya esta en uso");

        usuario.Id = Guid.NewGuid();
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);
        usuario.Role = 3;

        await _context.Users.AddAsync(usuario);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Usuario registrado correctamente",
            timestamp = DateTime.UtcNow
        });
    }

    public string CreateToken(User user)
    {
        var secretKey = _config["JwtSettings:SecretKey"];
        var issuer = _config["JwtSettings:Issuer"];
        var audience = _config["JwtSettings:Audience"];
        var expiryMinutes = double.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "30");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("institutionId", user.InstitutionId?.ToString() ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}



