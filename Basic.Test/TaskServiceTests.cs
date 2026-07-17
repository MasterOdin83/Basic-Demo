using Basic.Core.Entities;
using Basic.Core.Services;

namespace Basic.Test;

public class TaskServiceTests
{
    private readonly FakeTaskRepository _repo = new();
    private readonly TaskService _service;

    public TaskServiceTests() => _service = new TaskService(_repo);

    private Task<TaskItem> SeedTask(int userId, string title = "Seed") =>
        _service.CreateAsync(new TaskItem { Title = title, UserId = userId });

    [Fact]
    public async Task Create_assigns_id_and_persists()
    {
        var task = await _service.CreateAsync(new TaskItem { Title = "Buy milk", UserId = 1 });

        Assert.True(task.Id > 0);
        Assert.Single(_repo.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_without_title_throws(string title)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(new TaskItem { Title = title, UserId = 1 }));
    }

    [Fact]
    public async Task Create_with_invalid_status_throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateAsync(new TaskItem { Title = "T", Status = (TaskItemStatus)99, UserId = 1 }));
    }

    [Fact]
    public async Task Get_returns_own_task_but_not_anothers()
    {
        var task = await SeedTask(userId: 1);

        Assert.NotNull(await _service.GetAsync(task.Id, userId: 1));
        Assert.Null(await _service.GetAsync(task.Id, userId: 2));
    }

    [Fact]
    public async Task GetAll_returns_only_own_tasks()
    {
        await SeedTask(userId: 1);
        await SeedTask(userId: 1);
        await SeedTask(userId: 2);

        Assert.Equal(2, (await _service.GetAllAsync(1)).Count);
    }

    [Fact]
    public async Task Update_edits_own_task()
    {
        var task = await SeedTask(userId: 1);

        var updated = await _service.UpdateAsync(new TaskItem
        {
            Id = task.Id, UserId = 1, Title = "New title", Status = TaskItemStatus.Done
        });

        Assert.NotNull(updated);
        Assert.Equal("New title", _repo.Items[0].Title);
        Assert.Equal(TaskItemStatus.Done, _repo.Items[0].Status);
    }

    [Fact]
    public async Task Update_of_anothers_task_returns_null()
    {
        var task = await SeedTask(userId: 1);

        var updated = await _service.UpdateAsync(new TaskItem { Id = task.Id, UserId = 2, Title = "Hijack" });

        Assert.Null(updated);
        Assert.Equal("Seed", _repo.Items[0].Title);
    }

    [Fact]
    public async Task Delete_removes_own_task_but_not_anothers()
    {
        var task = await SeedTask(userId: 1);

        Assert.False(await _service.DeleteAsync(task.Id, userId: 2));
        Assert.True(await _service.DeleteAsync(task.Id, userId: 1));
        Assert.Empty(_repo.Items);
    }
}
