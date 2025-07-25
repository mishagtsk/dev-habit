using DevHabit.Api.Entities;
using Newtonsoft.Json;

namespace DevHabit.Api.DTOs.Habits;

public sealed record HabitWithTagsDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required HabitType Type { get; init; }
    public required FrequencyDto Frequency { get; init; }
    public required TargetDto Target { get; init; }
    public required HabitStatus Status { get; init; }
    public required bool IsArchived { get; init; }
    public required DateOnly? EndDate { get; init; }
    public required MilestoneDto? Milestone { get; init; }
    public AutomationSource? AutomationSource { get; set; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? LastCompletedAtUtc { get; init; }
    public required string[] Tags { get; set; }
}
