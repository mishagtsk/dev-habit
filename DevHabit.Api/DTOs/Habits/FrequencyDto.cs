using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Habits;

public sealed record FrequencyDto
{
    public required FrequencyType Type { get; init; }
    public required int TimesPerPeriod { get; init; }
}
