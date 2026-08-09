using System.Security.Claims;
using System.Security.Cryptography;
using Basic.Core.Entities;
using Basic.Core.Repositories;
using Basic.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasicSTS.API.Controllers;

public record CredentialsRequest(string Username, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController(UserService users, IConfiguration config, ISessionStore sessions) : ControllerBase
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

        var expiresAtUtc = DateTime.UtcNow.AddDays(config.GetValue<int>("Session:LifetimeDays"));
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await sessions.CreateAsync(new Session
        {
            Id = sessionId,
            UserId = user.Id,
            Username = user.Username,
            ExpiresAtUtc = expiresAtUtc
        });

        // Secure reflects the actual scheme of the incoming request (correct behind
        // Azure's TLS-terminating proxy too — see UseForwardedHeaders in Program.cs)
        // rather than a hardcoded flag, so local http dev keeps working.
        Response.Cookies.Append(SessionAuthenticationHandler.CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = new DateTimeOffset(expiresAtUtc),
            Path = "/"
        });
        return Ok(new { user.Username });
    }

    // JS can't clear an HttpOnly cookie itself — logout has to be a real server call
    // that both drops the session record and expires the cookie.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(SessionAuthenticationHandler.CookieName, out var sessionId))
        {
            await sessions.RemoveAsync(sessionId);
        }
        Response.Cookies.Delete(SessionAuthenticationHandler.CookieName, new CookieOptions { Path = "/" });
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Username = User.Identity!.Name
    });
}
