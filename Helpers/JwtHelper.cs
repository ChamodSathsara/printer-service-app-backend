using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PrinterServiceAPI.Models;

namespace PrinterServiceAPI.Helpers;

public class JwtSettings
{
    public string SecretKey               { get; set; } = string.Empty;
    public string Issuer                  { get; set; } = string.Empty;
    public string Audience                { get; set; } = string.Empty;
    public int    AccessTokenExpiryMinutes { get; set; } = 60;
    public int    RefreshTokenExpiryDays  { get; set; } = 7;
}

public interface IJwtHelper
{
    string   GenerateAccessToken(User user);
    string   GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime AccessTokenExpiry { get; }
    DateTime RefreshTokenExpiry { get; }
}

public class JwtHelper(JwtSettings settings) : IJwtHelper
{
    public DateTime AccessTokenExpiry  => DateTime.UtcNow.AddMinutes(settings.AccessTokenExpiryMinutes);
    public DateTime RefreshTokenExpiry => DateTime.UtcNow.AddDays(settings.RefreshTokenExpiryDays);

    public string GenerateAccessToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
            new Claim("techCode",  user.TechnicianCode),
            new Claim("fullName",  user.FullName),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer:             settings.Issuer,
            audience:           settings.Audience,
            claims:             claims,
            expires:            AccessTokenExpiry,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = false,  // allow expired for refresh flow
                ValidateIssuerSigningKey = true,
                ValidIssuer              = settings.Issuer,
                ValidAudience            = settings.Audience,
                IssuerSigningKey         = key
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
