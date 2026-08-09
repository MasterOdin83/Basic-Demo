using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Basic.Core.Entities;
using Basic.Core.Repositories;
using Basic.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basic.Test;

internal static class TestApp
{
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

    // Seeds a session directly (bypassing /login) and returns its id, for tests
    // that want an authenticated client without exercising the login flow itself.
    public static async Task<string> SeedSessionAsync(IServiceProvider services, int userId, string username, DateTime? expiresAtUtc = null)
    {
        using var scope = services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<ISessionStore>();
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        await sessions.CreateAsync(new Session
        {
            Id = id,
            UserId = userId,
            Username = username,
            ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddDays(1)
        });
        return id;
    }
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
        // Fresh, cookie-less client first: /me must reject before any session exists.
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

        var register = await _client.PostAsJsonAsync("/api/auth/register", new { username = "alice", password = "password123" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = "alice", password = "password123" });
        login.EnsureSuccessStatusCode();

        // _client has HandleCookies on (WebApplicationFactory default) — the session
        // cookie from login rides along automatically, no manual header needed.
        var me = await _client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        Assert.Equal("alice", (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("username").GetString());
    }

    [Fact]
    public async Task Login_sets_session_cookie_and_omits_tokens_from_body()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { username = "erin", password = "password123" });
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = "erin", password = "password123" });
        login.EnsureSuccessStatusCode();

        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
        // ASP.NET Core renders cookie flags lowercase ("httponly", not "HttpOnly") —
        // they're case-insensitive tokens per RFC 6265, so the check should be too.
        Assert.Contains(cookies!, c => c.StartsWith("session=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.TryGetProperty("token", out _));
        Assert.False(body.TryGetProperty("refreshToken", out _));
        Assert.Equal("erin", body.GetProperty("username").GetString());
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

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        (await _client.GetAsync("/openapi/v1.json")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Logout_clears_the_server_side_session()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { username = "frank", password = "password123" });
        await _client.PostAsJsonAsync("/api/auth/login", new { username = "frank", password = "password123" });
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/auth/me")).StatusCode);

        // The cookie itself isn't readable/clearable by JS (HttpOnly) — logout has to
        // be a real server round-trip that drops the session record.
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Register_rejection_reveals_no_reason()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new { username = "carol", password = "password123" });
        var duplicate = await _client.PostAsJsonAsync("/api/auth/register", new { username = "carol", password = "password456" });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Empty(await duplicate.Content.ReadAsStringAsync());
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
        var sessionId = TestApp.SeedSessionAsync(_factory.Services, 1, "demo").GetAwaiter().GetResult();
        _client.DefaultRequestHeaders.Add("Cookie", $"session={sessionId}");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Without_session_tasks_are_unauthorized_but_statuses_are_public()
    {
        using var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/tasks")).StatusCode);

        var statuses = await anonymous.GetFromJsonAsync<string[]>("/api/tasks/statuses");
        Assert.NotNull(statuses);
        Assert.Equal(["Pending", "InProgress", "Done"], statuses);
    }

    [Fact]
    public async Task Expired_session_is_unauthorized()
    {
        using var expired = _factory.CreateClient();
        var sessionId = await TestApp.SeedSessionAsync(_factory.Services, 1, "demo", DateTime.UtcNow.AddMinutes(-1));
        expired.DefaultRequestHeaders.Add("Cookie", $"session={sessionId}");

        Assert.Equal(HttpStatusCode.Unauthorized, (await expired.GetAsync("/api/tasks")).StatusCode);
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
    public async Task OpenApi_document_is_served()
    {
        (await _client.GetAsync("/openapi/v1.json")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Create_without_status_defaults_to_pending()
    {
        var create = await _client.PostAsJsonAsync("/api/tasks", new { title = "No status sent", description = "", dueDate = (string?)null });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal("Pending", (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
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
        // Session for a second, non-existent-data user: sees an empty list, not demo's tasks.
        using var other = _factory.CreateClient();
        var sessionId = await TestApp.SeedSessionAsync(_factory.Services, 999, "intruder");
        other.DefaultRequestHeaders.Add("Cookie", $"session={sessionId}");

        var tasks = await other.GetFromJsonAsync<JsonElement>("/api/tasks");
        Assert.Equal(0, tasks.GetArrayLength());
    }
}
