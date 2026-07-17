using Basic.Core.Entities;
using Basic.Core.Repositories;

namespace Basic.Core.Services;

public class TaskService(ITaskRepository tasks)
{
    public Task<List<TaskItem>> GetAllAsync(int userId) => tasks.GetByUserAsync(userId);

    public async Task<TaskItem?> GetAsync(int id, int userId)
    {
        var task = await tasks.GetByIdAsync(id);
        return task?.UserId == userId ? task : null;
    }

    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        Validate(task);
        return tasks.AddAsync(task);
    }

    public async Task<TaskItem?> UpdateAsync(TaskItem task)
    {
        Validate(task);
        var existing = await tasks.GetByIdAsync(task.Id);
        if (existing is null || existing.UserId != task.UserId) return null;
        existing.Title = task.Title;
        existing.Description = task.Description;
        existing.Status = task.Status;
        existing.DueDate = task.DueDate;
        await tasks.UpdateAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var existing = await tasks.GetByIdAsync(id);
        if (existing is null || existing.UserId != userId) return false;
        await tasks.DeleteAsync(existing);
        return true;
    }

    private static void Validate(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Title)) throw new ArgumentException("Title is required.");
        if (!Enum.IsDefined(task.Status)) throw new ArgumentException("Invalid status.");
    }
}
