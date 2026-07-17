using Basic.Core.Entities;
using Basic.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basic.Test;

public class DatabaseInitializeTests
{
    [Fact]
    public async Task Initialize_twice_seeds_only_once()
    {
        var path = Path.Combine(Path.GetTempPath(), $"basic-init-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection().AddBasicData($"Data Source={path}").BuildServiceProvider();
        try
        {
            await services.InitializeDatabaseAsync();
            await services.InitializeDatabaseAsync();

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.Users.CountAsync());
            Assert.Equal(3, await db.Tasks.CountAsync());
        }
        finally
        {
            await services.DisposeAsync();
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}

public class EfRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly EfTaskRepository _tasks;
    private readonly EfUserRepository _users;

    public EfRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _tasks = new EfTaskRepository(_db);
        _users = new EfUserRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Task<User> SeedUser(string username = "demo") =>
        _users.AddAsync(new User { Username = username, PasswordHash = "hash" });

    [Fact]
    public async Task Task_crud_round_trip()
    {
        var user = await SeedUser();
        var task = await _tasks.AddAsync(new TaskItem { Title = "T1", UserId = user.Id });
        Assert.True(task.Id > 0);

        var loaded = await _tasks.GetByIdAsync(task.Id);
        Assert.NotNull(loaded);

        loaded.Title = "T1 edited";
        loaded.Status = TaskItemStatus.Done;
        await _tasks.UpdateAsync(loaded);
        Assert.Equal("T1 edited", (await _tasks.GetByIdAsync(task.Id))!.Title);

        await _tasks.DeleteAsync(loaded);
        Assert.Null(await _tasks.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task GetByUser_filters_and_sorts_by_due_date_with_nulls_last()
    {
        var alice = await SeedUser("alice");
        var bob = await SeedUser("bob");
        await _tasks.AddAsync(new TaskItem { Title = "no due date", UserId = alice.Id });
        await _tasks.AddAsync(new TaskItem { Title = "later", DueDate = DateTime.Today.AddDays(5), UserId = alice.Id });
        await _tasks.AddAsync(new TaskItem { Title = "soon", DueDate = DateTime.Today, UserId = alice.Id });
        await _tasks.AddAsync(new TaskItem { Title = "bobs", UserId = bob.Id });

        var result = await _tasks.GetByUserAsync(alice.Id);

        Assert.Equal(["soon", "later", "no due date"], result.Select(t => t.Title));
    }

    [Fact]
    public async Task User_lookup_by_username_and_id()
    {
        var user = await SeedUser();

        Assert.NotNull(await _users.GetByUsernameAsync("demo"));
        Assert.NotNull(await _users.GetByIdAsync(user.Id));
        Assert.Null(await _users.GetByUsernameAsync("nobody"));
    }

    [Fact]
    public async Task Duplicate_username_is_rejected_by_unique_index()
    {
        await SeedUser("demo");

        await Assert.ThrowsAsync<DbUpdateException>(() => SeedUser("demo"));
    }

    [Fact]
    public async Task Deleting_user_cascades_to_tasks()
    {
        var user = await SeedUser();
        await _tasks.AddAsync(new TaskItem { Title = "T", UserId = user.Id });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        Assert.Empty(await _db.Tasks.ToListAsync());
    }
}
