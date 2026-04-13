using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Synquid.API.Extensions;

public static class AuthenticationExtensions
{
    public static ClaimsPrincipal? ValidateTokenStatic(
        string token,
        IConfiguration config)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(config["JwtSettings:SecretKey"]!);

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = config["JwtSettings:Issuer"],
                ValidateAudience = true,
                ValidAudience = config["JwtSettings:Audience"],
                ValidateLifetime = true
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}