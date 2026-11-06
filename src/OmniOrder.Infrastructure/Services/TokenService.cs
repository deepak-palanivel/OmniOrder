using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResponse GenerateDemoToken(DemoTokenRequest request)
    {
        var key = _configuration["Jwt:Key"] ?? "OmniOrder-SuperSecret-JWT-Key-For-Development-2026-Must-Be-256-Bits!";
        var issuer = _configuration["Jwt:Issuer"] ?? "OmniOrder";
        var audience = _configuration["Jwt:Audience"] ?? "OmniOrderClient";
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.Role, request.Role),
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", request.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResponse(tokenString, request.Username, request.Role, expiresAt);
    }
}
