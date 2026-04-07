using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;            


namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("Register")]
    public async Task<ActionResult> Register([FromBody] User usuario)
    {
        if (usuario == null) return BadRequest("El cuerpo de la solicitud no puede estar vacío");
        if (string.IsNullOrWhiteSpace(usuario.Email)) return BadRequest("El email es requerido");
        if (!usuario.Email.Contains("@")) return BadRequest("Email inválido");
        if (string.IsNullOrWhiteSpace(usuario.PasswordHash) || usuario.PasswordHash.Length < 6) return BadRequest("La contraseña debe tener mínimo 6 caracteres");

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

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] _RequestLogin login) 
    {
        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Email == login.email);
        if (user == null) return NotFound("Usuario no encontrado");
        bool valid = BCrypt.Net.BCrypt.Verify(login.password, user.PasswordHash);
        if (!valid) return Unauthorized("Contraseña incorrecta");

        string token = CreateToken(user);

        return Ok(new
        {
            errorCode = 0,
            message = "Log correcto",
            timestamp = DateTime.UtcNow,
            token = token.ToString()
        });
    }


    [HttpPost("logout")]
    public async Task<ActionResult> logout() 
    {
        string authHeader = Request.Headers["Authorization"].ToString();
        ClaimsPrincipal principal = ValidateToken(authHeader);
        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;


        return Ok(new
        {
            errorCode = 0,
            message = "Sesión cerrada correctamente",
            timestamp = DateTime.UtcNow,
        });
    }


    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] refreshToken r)
    {
        ClaimsPrincipal principal = ValidateToken(r.token);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        User? user = _context.Users.Find(Guid.Parse(userId ?? ""));

        if (user == null)
            return NotFound();

        string newToken = CreateToken(user);
        return Ok(new { token = newToken });
    }

    [HttpPost("forgotPassword")]
    public async Task<ActionResult> forgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
    {
        User? user = await _context.Users.FirstOrDefaultAsync(x => x.Email == forgotPasswordRequest.Email);


        if (user == null) return NotFound("El usuario no se encontro");
        try {
            await SendMail(forgotPasswordRequest.Email, "Cambio de contraseña", "CAMBIO WASAAAA!!!");
        }
        catch
        {
            return BadRequest("Error fatal porfavor comuniquece con su proveedor");
        }


        return Ok(new
        {
            errorCode = 0,
            message = "Email enviado correctamente",
            timestamp = DateTime.UtcNow,
        });
    }

    public async Task<ActionResult> changePassword([FromBody] requestChangePassword r) 
    {
        ClaimsPrincipal principal = ValidateToken(r.token);

        if (principal == null)
            return Unauthorized("Token inválido o expirado");

        string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        User? user = _context.Users.Find(Guid.Parse(userId ?? ""));

        if (user == null) return NotFound();

        bool valid = BCrypt.Net.BCrypt.Verify(r.oldPassword, user.PasswordHash);
        if (!valid) return Unauthorized("Contraseña incorrecta");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(r.newPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            errorCode = 0,
            message = "Email enviado correctamente",
            timestamp = DateTime.UtcNow,
        });
    }

    private async Task SendMail(string to, string subject, string body)
    {
        var adminUser = _config["AdminUser"];
        var adminPassword = _config["AdminPassword"];
        var smtpName = _config["SMTPName"];
        var smtpPort = _config["SMTPPort"];

        if (string.IsNullOrEmpty(adminUser) || string.IsNullOrEmpty(adminPassword) ||
            string.IsNullOrEmpty(smtpName) || string.IsNullOrEmpty(smtpPort))
        {
            throw new InvalidOperationException("Falta config SMTP en appsettings");
        }

        var Mensaje = new MailMessage();
        Mensaje.To.Add(new MailAddress(to));
        Mensaje.From = new MailAddress(adminUser);
        Mensaje.Subject = subject;
        Mensaje.Body = body;
        Mensaje.IsBodyHtml = true;

        using (var smtp = new SmtpClient())
        {
            var credencial = new NetworkCredential
            {
                UserName = adminUser,
                Password = adminPassword,
            };
            smtp.Credentials = credencial;
            smtp.Host = smtpName;
            smtp.Port = int.Parse(smtpPort);
            smtp.EnableSsl = true;
            await smtp.SendMailAsync(Mensaje);
        }
    }

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
    private string CreateToken(User user)
    {
        var secretKey = _config["JwtSettings:SecretKey"];
        var issuer = _config["JwtSettings:Issuer"];
        var audience = _config["JwtSettings:Audience"];
        var expiryMinutes = double.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "1440");

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
public class ForgotPasswordRequest
{
    public string Email { get; set; }
}

public class requestChangePassword
{
    public string token { get; set; } = string.Empty;
    public string newPassword { get; set; } = string.Empty;
    public string oldPassword { get; set; } = string.Empty;
}

public class refreshToken
{
    public string token { get; set; } = string.Empty;

}

public class _RequestLogin 
{
    public string email { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}



