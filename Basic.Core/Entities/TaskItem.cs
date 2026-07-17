namespace Basic.Core.Entities;

public enum TaskItemStatus { Pending, InProgress, Done }

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public TaskItemStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public int UserId { get; set; }
}
