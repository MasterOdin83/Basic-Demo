using Basic.Data;
using BasicSTS.API;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Bare 4xx responses stay body-less: rejection details would enable user enumeration.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressMapClientErrors = true);
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

// So Request.IsHttps (session cookie's Secure flag) reads correctly behind Azure's
// TLS-terminating proxy instead of always seeing the internal http hop.
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
