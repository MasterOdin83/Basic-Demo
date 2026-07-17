using Basic.Core.Entities;
using Basic.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Basic.Data;

public class EfTaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<List<TaskItem>> GetByUserAsync(int userId) =>
        db.Tasks.Where(t => t.UserId == userId).OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ToListAsync();

    public Task<TaskItem?> GetByIdAsync(int id) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TaskItem> AddAsync(TaskItem task)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public Task UpdateAsync(TaskItem task) => db.SaveChangesAsync();

    public async Task DeleteAsync(TaskItem task)
    {
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }
}
