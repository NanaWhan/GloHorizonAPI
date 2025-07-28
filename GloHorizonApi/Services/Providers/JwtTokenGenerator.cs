using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GloHorizonApi.Extensions;
using GloHorizonApi.Models.DomainModels;
using Microsoft.IdentityModel.Tokens;

namespace GloHorizonApi.Services.Providers;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var secretKey = _configuration.GetValue<string>("ApiSettings:Secret");
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT Secret key is not configured");

        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(
                new Claim[]
                {
                    new Claim("Id", user.Id),
                    new Claim("Email", user.Email),
                    new Claim("PhoneNumber", user.PhoneNumber),
                    new Claim("FullName", user.FullName),
                    new Claim(ClaimTypes.Role, CommonConstants.Roles.User)
                }
            ),
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = "GLO-HORIZON PROJECT"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var stringToken = tokenHandler.WriteToken(token);

        return stringToken;
    }

    public string GenerateToken(Admin admin)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var secretKey = _configuration.GetValue<string>("ApiSettings:Secret");
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT Secret key is not configured");

        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(
                new Claim[]
                {
                    new Claim("Id", admin.Id),
                    new Claim("Email", admin.Email),
                    new Claim("FullName", admin.FullName),
                    new Claim("Role", admin.Role.ToString()),
                    new Claim(ClaimTypes.Role, CommonConstants.Roles.Admin)
                }
            ),
            Expires = DateTime.UtcNow.AddDays(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = "GLO-HORIZON PROJECT"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var stringToken = tokenHandler.WriteToken(token);

        return stringToken;
    }

    public static string GenerateUserJwtToken(
        IConfiguration configuration,
        string id,
        string username
    )
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT Secret key is not configured");

        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(
                new Claim[]
                {
                    new Claim("Id", id),
                    new Claim("Username", username),
                    new Claim(ClaimTypes.Role, CommonConstants.Roles.Admin)
                }
            ),
            Expires = DateTime.UtcNow.AddDays(365),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = "GLO-HORIZON PROJECT"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var stringToken = tokenHandler.WriteToken(token);

        return stringToken;
    }
} 