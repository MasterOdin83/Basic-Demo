using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Basic.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddBasicData(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddCors(o => o.AddPolicy("ui", p => p
    .WithOrigins(builder.Configuration["Cors:UiOrigin"]!.Split(';'))
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ponytail: in-memory counters per instance; if the App Service scales out each instance counts separately.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
