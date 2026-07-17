using Basic.Core.Entities;
using Basic.Core.Repositories;

namespace Basic.Test;

public class FakeTaskRepository : ITaskRepository
{
    public List<TaskItem> Items { get; } = [];
    private int _nextId = 1;

    public Task<List<TaskItem>> GetByUserAsync(int userId) =>
        Task.FromResult(Items.Where(t => t.UserId == userId).ToList());

    public Task<TaskItem?> GetByIdAsync(int id) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

    public Task<TaskItem> AddAsync(TaskItem task)
    {
        task.Id = _nextId++;
        Items.Add(task);
        return Task.FromResult(task);
    }

    public Task UpdateAsync(TaskItem task) => Task.CompletedTask;

    public Task DeleteAsync(TaskItem task)
    {
        Items.Remove(task);
        return Task.CompletedTask;
    }
}

public class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];
    private int _nextId = 1;

    public Task<User?> GetByIdAsync(int id) =>
        Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByUsernameAsync(string username) =>
        Task.FromResult(Items.FirstOrDefault(u => u.Username == username));

    public Task<User> AddAsync(User user)
    {
        user.Id = _nextId++;
        Items.Add(user);
        return Task.FromResult(user);
    }
}
