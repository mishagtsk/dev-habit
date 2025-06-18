namespace DevHabit.Api.DTOs.Habits;

public sealed record UpdateMilestoneDto
{
    public required int Target { get; init; }
}
