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

        return Ok(new { token = CreateToken(user), user.Id, user.Username });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Username = User.Identity!.Name
    });

    private string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            ]),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        });
    }
}
