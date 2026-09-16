using System.Text;
using System.Threading.RateLimiting;
using Basic.Data;
using BasicSTS.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Captcha off is a Development-only convenience (tests, local runs). QA/prod must carry a
// Turnstile secret (Captcha__Secret App Setting; appsettings.QA.json ships Cloudflare's test key).
if (!builder.Environment.IsDevelopment() && string.IsNullOrEmpty(builder.Configuration["Captcha:Secret"]))
    throw new InvalidOperationException("Captcha:Secret is required outside Development (Cloudflare Turnstile secret key).");

// Bare 4xx responses stay body-less: rejection details would enable user enumeration.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressMapClientErrors = true);
builder.Services.AddOpenApi();
builder.Services.AddBasicData(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddCors(o => o.AddPolicy("ui", p => p
    .WithOrigins(builder.Configuration["Cors:UiOrigin"]!.Split(';'))
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddHttpClient<CaptchaVerifier>(c => c.Timeout = TimeSpan.FromSeconds(10));

// Brute force / DoS: 10 auth attempts per minute per IP on login, register and refresh, and 100
// requests per minute per IP overall. Fixed window, no queue, 429 when exceeded.
// ponytail: in-memory counters per instance; if the App Service scales out each instance counts separately.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        // Default 5-min skew would keep a 3-min demo token alive for 8; expiry must be exact.
        ClockSkew = TimeSpan.Zero
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

// Behind Azure's proxy the per-IP rate-limit partitions must see the real client IP, not the proxy's.
if (!app.Environment.IsDevelopment())
{
    var fh = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
    fh.KnownIPNetworks.Clear(); fh.KnownProxies.Clear();
    app.UseForwardedHeaders(fh);
}

app.UseCors("ui");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// API explorer without Postman: /openapi/v1.json + /scalar UI.
app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

app.Run();

public partial class Program { }
