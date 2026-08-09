using System.Text.Json.Serialization;
using Basic.API;
using Basic.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddBasicData(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddCors(o => o.AddPolicy("ui", p => p
    .WithOrigins(builder.Configuration["Cors:UiOrigin"]!.Split(';'))
    .AllowAnyHeader()
    .AllowAnyMethod()
    // Session cookie must ride along on cross-origin fetches from the UI's own origin.
    .AllowCredentials()));

builder.Services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

// So the session-cookie flow's Secure flag reads correctly behind Azure's
// TLS-terminating proxy (set on BasicSTS.API, which issues the cookie — this API
// only validates it, but forwarding headers correctly matters here too for any
// future scheme-dependent logic).
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });

app.UseCors("ui");
app.UseAuthentication();
app.UseAuthorization();

// API explorer without Postman: /openapi/v1.json + /scalar UI.
app.MapOpenApi();
app.MapScalarApiReference();

app.MapControllers();

app.Run();

public partial class Program { }
