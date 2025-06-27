namespace DevHabit.Api.Entities;

public class Tag
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    
    public static string NewId() => $"t_{Guid.CreateVersion7()}";
}
