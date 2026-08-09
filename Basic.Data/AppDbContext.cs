using Basic.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Basic.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();

    // ponytail: piggybacking sessions on the app's own SQLite file is what lets both
    // APIs (separate processes) see the same session state without standing up Redis
    // yet. Swap EfSessionStore for a Redis/separate-service-backed ISessionStore
    // (Docker/K8s phase) to decouple identity state from business data properly.
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Session>().HasKey(s => s.Id);

        modelBuilder.Entity<TaskItem>().Property(t => t.Title).IsRequired();
        modelBuilder.Entity<TaskItem>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
