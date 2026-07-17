using System.Text;
using Basic.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Bare 4xx responses stay body-less: rejection details would enable user enumeration.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o => o.SuppressMapClientErrors = true);
builder.Services.AddBasicData(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddCors(o => o.AddPolicy("ui", p => p
    .WithOrigins(builder.Configuration["Cors:UiOrigin"]!.Split(';'))
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseCors("ui");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
