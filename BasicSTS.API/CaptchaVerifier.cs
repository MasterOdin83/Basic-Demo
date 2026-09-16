using System.Text.Json;

namespace BasicSTS.API;

// Cloudflare Turnstile, server side. Empty Captcha:Secret = captcha off — allowed only in
// Development (tests, local runs); Program.cs refuses to start without it anywhere else.
// Cloudflare's always-pass test pair: site key 1x00000000000000000000AA,
// secret 1x0000000000000000000000000000000AA (used in appsettings.Development/QA).
public class CaptchaVerifier(HttpClient http, IConfiguration config)
{
    public async Task<bool> VerifyAsync(string? token, string? remoteIp)
    {
        var secret = config["Captcha:Secret"];
        if (string.IsNullOrEmpty(secret)) return true;
        if (string.IsNullOrEmpty(token)) return false;

        var form = new Dictionary<string, string> { ["secret"] = secret, ["response"] = token };
        if (!string.IsNullOrEmpty(remoteIp)) form["remoteip"] = remoteIp;
        try
        {
            using var response = await http.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", new FormUrlEncodedContent(form));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.TryGetProperty("success", out var success) && success.GetBoolean();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Fail closed: a Cloudflare outage blocks logins for a minute, it never lets bots through.
            return false;
        }
    }
}
