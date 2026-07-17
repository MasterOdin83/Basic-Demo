using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Basic.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Basic.Test;

internal static class TestApp
{
    // Same dev values as both APIs' appsettings.json.
    public const string JwtKey = "dev-only-secret-key-basic-demo-32chars!!";

    public static (WebApplicationFactory<TMarker> Factory, SqliteConnection Connection) Create<TMarker>() where TMarker : class
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var factory = new WebApplicationFactory<TMarker>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
            }));
        return (factory, connection);
    }

    public static string TokenFor(int userId, string username) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "BasicSTS",
            Audience = "BasicApp",
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username)
            ]),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256)
        });
}

public class StsEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<BasicSTS.API.Controllers.AuthController> _factory;
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;

    public StsEndpointTests()
    {
        (_factory, _connection) = TestApp.Create<BasicSTS.API.Controllers.AuthController>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Register_login_and_me_flow()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { username = "alice", password = "password123" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = "alice", password = "password123" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        // Unauthorized without token, authorized with it.
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await _client.SendAsync(request);
        me.EnsureSuccessStatusCode();
        Assert.Equal("alice", (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("username").GetString());
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = "demo", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Register_with_short_password_is_bad_request()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new { username = "bob", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);
    }
}

public class TasksEndpointTests : IDisposable
{
    private readonly WebApplicationFactory<Basic.API.Controllers.TasksController> _factory;
    private readonly SqliteConnection _connection;
    private readonly HttpClient _client;

    public TasksEndpointTests()
    {
        (_factory, _connection) = TestApp.Create<Basic.API.Controllers.TasksController>();
        _client = _factory.CreateClient();
        // Seeded demo user gets Id 1.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestApp.TokenFor(1, "demo"));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Without_token_tasks_are_unauthorized_but_statuses_are_public()
    {
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/tasks")).StatusCode);

        var statuses = await anonymous.GetFromJsonAsync<string[]>("/api/tasks/statuses");
        Assert.NotNull(statuses);
        Assert.Equal(["Pending", "InProgress", "Done"], statuses);
    }

    [Fact]
    public async Task GetAll_returns_seeded_tasks()
    {
        var tasks = await _client.GetFromJsonAsync<JsonElement>("/api/tasks");
        Assert.Equal(3, tasks.GetArrayLength());
    }

    [Fact]
    public async Task Crud_round_trip()
    {
        var create = await _client.PostAsJsonAsync("/api/tasks", new { title = "New task", description = "From test", status = "Pending", dueDate = (string?)null });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var update = await _client.PutAsJsonAsync($"/api/tasks/{id}", new { title = "Edited", description = "", status = "Done", dueDate = (string?)null });
        update.EnsureSuccessStatusCode();
        Assert.Equal("Edited", (await update.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/tasks/{id}")).StatusCode);
    }

    [Fact]
    public async Task Create_with_empty_title_is_bad_request()
    {
        var create = await _client.PostAsJsonAsync("/api/tasks", new { title = "", description = "", status = "Pending", dueDate = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Anothers_task_is_not_visible()
    {
        // Token for a second, non-existent-data user: sees an empty list, not demo's tasks.
        using var other = _factory.CreateClient();
        other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestApp.TokenFor(999, "intruder"));

        var tasks = await other.GetFromJsonAsync<JsonElement>("/api/tasks");
        Assert.Equal(0, tasks.GetArrayLength());
    }
}
