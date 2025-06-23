namespace DevHabit.Api.Settings;

public class CorsOptions
{
    public const string PolicyName = "DevHabitCorsPolicy";
    public const string SectionName = "Cors";
    
    public required string[] AllowedOrigins { get; init; }
}
