using System.Security.Claims;
using System.Text;
using Basic.Core.Entities;
using Basic.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BasicSTS.API.Controllers;

public record CredentialsRequest(string Username, string Password);

public record RefreshRequest(string RefreshToken);

[ApiController]
[Route("api/auth")]
public class AuthController(UserService users, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(CredentialsRequest request)
    {
        try
        {
            var user = await users.RegisterAsync(request.Username, request.Password);
            return CreatedAtAction(nameof(Me), new { }, new { user.Id, user.Username });
        }
        catch (ArgumentException)
        {
            // No reason in the response: revealing "username taken" etc. enables user enumeration.
            return BadRequest();
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(CredentialsRequest request)
    {
        var user = await users.ValidateCredentialsAsync(request.Username, request.Password);
        if (user is null) return Unauthorized(new { error = "Invalid username or password." });

        return Ok(new
        {
            token = CreateAccessToken(user.Id, user.Username),
            refreshToken = CreateToken(user.Id, user.Username, RefreshAudience,
                TimeSpan.FromDays(config.GetValue<int>("Jwt:RefreshTokenDays"))),
            user.Id,
            user.Username
        });
    }

    // ponytail: stateless refresh JWT (absolute expiry, no rotation/revocation); store tokens if revocation is ever needed.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(request.RefreshToken,
            new TokenValidationParameters
            {
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = RefreshAudience,
                IssuerSigningKey = SigningKey,
                ClockSkew = TimeSpan.Zero
            });
        if (!result.IsValid) return Unauthorized();

        var identity = result.ClaimsIdentity;
        return Ok(new
        {
            token = CreateAccessToken(
                int.Parse(identity.FindFirst(ClaimTypes.NameIdentifier)!.Value),
                identity.FindFirst(ClaimTypes.Name)!.Value)
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Username = User.Identity!.Name
    });

    // Refresh tokens carry their own audience so the resource APIs (audience "BasicApp") reject them.
    private string RefreshAudience => config["Jwt:Audience"] + ".refresh";

    private SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

    private string CreateAccessToken(int id, string username) =>
        CreateToken(id, username, config["Jwt:Audience"]!,
            TimeSpan.FromMinutes(config.GetValue<int>("Jwt:AccessTokenMinutes")));

    private string CreateToken(int id, string username, string audience, TimeSpan lifetime) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = config["Jwt:Issuer"],
            Audience = audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, username)
            ]),
            Expires = DateTime.UtcNow.Add(lifetime),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
}
