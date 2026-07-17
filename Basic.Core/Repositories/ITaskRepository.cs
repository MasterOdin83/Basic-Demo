using Basic.Core.Entities;

namespace Basic.Core.Repositories;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetByUserAsync(int userId);
    Task<TaskItem?> GetByIdAsync(int id);
    Task<TaskItem> AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(TaskItem task);
}
