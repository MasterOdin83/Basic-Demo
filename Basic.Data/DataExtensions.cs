using Basic.Core.Entities;
using Basic.Core.Repositories;
using Basic.Core.Security;
using Basic.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Basic.Data;

public static class DataExtensions
{
    public static IServiceCollection AddBasicData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<TaskService>();
        services.AddScoped<UserService>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ponytail: both APIs share one SQLite file and can start at the same time;
        // the EnsureCreated loser gets "table already exists" and can safely move on.
        try
        {
            await db.Database.EnsureCreatedAsync();
        }
        catch (SqliteException e) when (e.SqliteErrorCode == 1 && e.Message.Contains("already exists"))
        {
        }

        try
        {
            if (await db.Users.AnyAsync()) return;

            var demo = new User { Username = "demo", PasswordHash = PasswordHasher.Hash("Password123!") };
            db.Users.Add(demo);
            await db.SaveChangesAsync();

            db.Tasks.AddRange(
                new TaskItem { Title = "Prepare demo", Description = "Walk through the app end to end", Status = TaskItemStatus.InProgress, DueDate = DateTime.Today.AddDays(1), UserId = demo.Id },
                new TaskItem { Title = "Review architecture", Description = "Explain Clean Architecture layers", Status = TaskItemStatus.Pending, DueDate = DateTime.Today.AddDays(3), UserId = demo.Id },
                new TaskItem { Title = "Write unit tests", Description = "TDD on services and repositories", Status = TaskItemStatus.Done, UserId = demo.Id });
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique Username index tripped: the other API seeded first — nothing to do.
        }
    }
}
