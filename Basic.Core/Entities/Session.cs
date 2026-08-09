namespace Basic.Core.Entities;

public class Session
{
    public string Id { get; set; } = "";
    public int UserId { get; set; }
    public string Username { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
}
