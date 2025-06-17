namespace DevHabit.Api.DTOs.Habits;

public sealed class MilestoneDto
{
    public required int Target { get; init; }
    public required int Current { get; init; }
}
