using System.Security.Claims;
using Basic.Core.Entities;
using Basic.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Basic.API.Controllers;

public record TaskRequest(string Title, string? Description, TaskItemStatus Status, DateTime? DueDate);

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController(TaskService tasks) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [AllowAnonymous]
    [HttpGet("statuses")]
    public IActionResult GetStatuses() => Ok(Enum.GetNames<TaskItemStatus>());

    [HttpGet]
    public Task<List<TaskItem>> GetAll() => tasks.GetAllAsync(UserId);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var task = await tasks.GetAsync(id, UserId);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskRequest request)
    {
        try
        {
            var task = await tasks.CreateAsync(ToTask(request, id: 0));
            return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, TaskRequest request)
    {
        try
        {
            var task = await tasks.UpdateAsync(ToTask(request, id));
            return task is null ? NotFound() : Ok(task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await tasks.DeleteAsync(id, UserId) ? NoContent() : NotFound();

    private TaskItem ToTask(TaskRequest request, int id) => new()
    {
        Id = id,
        Title = request.Title,
        Description = request.Description ?? "",
        Status = request.Status,
        DueDate = request.DueDate,
        UserId = UserId
    };
}
