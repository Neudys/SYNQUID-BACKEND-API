using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synquid.API.Extensions;
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
        try
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] _RequestLogin login)
    {
        try
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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult> logout()
    {
        try
        {
            string authHeader = Request.Headers["Authorization"].ToString();
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(authHeader, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Ok(new
            {
                errorCode = 0,
                message = "Sesión cerrada correctamente",
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] refreshToken r)
    {
        try
        {
            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(r.token, _config);

            if (principal == null)
                return Unauthorized("Token inválido o expirado");

            string? userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            User? user = _context.Users.Find(Guid.Parse(userId ?? ""));

            if (user == null)
                return NotFound();

            string newToken = CreateToken(user);
            return Ok(new { token = newToken });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("forgotPassword")]
    public async Task<ActionResult> forgotPassword([FromBody] ForgotPasswordRequest forgotPasswordRequest)
    {
        try
        {
            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Email == forgotPasswordRequest.Email);

            if (user == null)
                return NotFound("El usuario no se encontró");

            try
            {
                string resetToken = Guid.NewGuid().ToString();

                var passwordResetToken = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = resetToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.PasswordResetTokens.AddAsync(passwordResetToken);
                await _context.SaveChangesAsync();

                string resetLink = $"https://tudominio.com/reset-password?token={resetToken}";
                string emailBody = $@"
                <h2>Recuperación de contraseña</h2>
                <p>Haz clic en el enlace para resetear tu contraseña:</p>
                <a href='{resetLink}'>Resetear contraseña</a>
                <p>Este enlace expira en 15 minutos.</p>
            ";

                await SendMail(forgotPasswordRequest.Email, "Recuperación de contraseña", emailBody);
            }
            catch
            {
                return BadRequest("Error al enviar email. Contacta al administrador");
            }

            return Ok(new
            {
                errorCode = 0,
                message = "Email de recuperación enviado correctamente",
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("resetPassword")]
    public async Task<ActionResult> ResetPassword([FromBody] resetPasswordRequest request)
    {
        try
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == request.token && !t.IsUsed);

            if (resetToken == null)
                return BadRequest("Token inválido o expirado");

            if (resetToken.ExpiresAt < DateTime.UtcNow)
                return BadRequest("El token de reset ha expirado");

            User? user = await _context.Users.FindAsync(resetToken.UserId);

            if (user == null)
                return NotFound("Usuario no encontrado");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            resetToken.IsUsed = true;
            resetToken.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                errorCode = 0,
                message = "Contraseña restablecida correctamente",
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    public class resetPasswordRequest
    {
        public string token { get; set; } = string.Empty;
        public string newPassword { get; set; } = string.Empty;
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

    [HttpPost("validateToken")]
    public ActionResult ValidateTokenPublic([FromHeader(Name = "Authorization")] string authorization)
    {
        try
        {
            var token = authorization?.Replace("Bearer ", "").Trim();

            if (string.IsNullOrEmpty(token))
                return Unauthorized("Token requerido");

            ClaimsPrincipal? principal = AuthenticationExtensions.ValidateTokenStatic(token, _config);

            if (principal == null)
                return Unauthorized("Token inválido");

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { userId, role, valid = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost("verifyEmail")]
    public async Task<ActionResult> VerifyEmail([FromBody] verifyEmailRequest request)
    {
        try
        {
            User? user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.email);

            if (user == null)
                return NotFound("Usuario no encontrado");

            user.EmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                errorCode = 0,
                message = "Email verificado correctamente",
                timestamp = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
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

public class resetPasswordRequest
{
    public string email { get; set; } = string.Empty;
    public string newPassword { get; set; } = string.Empty;
}

public class verifyEmailRequest
{
    public string email { get; set; } = string.Empty;
}