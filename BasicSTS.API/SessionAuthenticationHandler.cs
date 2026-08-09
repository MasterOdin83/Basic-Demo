using System.Security.Claims;
using System.Text.Encodings.Web;
using Basic.Core.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BasicSTS.API;

// The only auth scheme in the BFF flow: an opaque HttpOnly session cookie resolved
// against the shared ISessionStore. No JWTs anywhere in this path — the browser
// never holds a bearer credential of any kind. Same handler (independently, by
// design) in Basic.API — both APIs trust the same cookie/store, neither trusts a
// token minted by the other.
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
