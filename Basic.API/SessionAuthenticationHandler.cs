using System.Security.Claims;
using System.Text.Encodings.Web;
using Basic.Core.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Basic.API;

// Same scheme as BasicSTS.API's own copy (independently, by design — see there for
// the full rationale): resolves the shared session cookie against ISessionStore.
// No JWTs, no Authorization header — [Authorize] on TasksController didn't need to
// change at all, it just consumes whatever ClaimsPrincipal this handler produces.
public class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISessionStore sessions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Session";
    public const string CookieName = "session";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(CookieName, out var sessionId))
            return AuthenticateResult.NoResult();

        var session = await sessions.GetAsync(sessionId);
        if (session is null) return AuthenticateResult.Fail("Invalid or expired session.");

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Username)
        ], SchemeName);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
