namespace DevHabit.Api.Entities;

public class GitHubAccessToken
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAtUtc {get; set; }
    public DateTime CreatedAtUtc { get; set; }
    
    public static string NewId() => $"gh_{Guid.CreateVersion7()}";
}
