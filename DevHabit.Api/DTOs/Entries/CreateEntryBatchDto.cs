namespace DevHabit.Api.DTOs.Entries;

public class CreateEntryBatchDto
{
    public required List<CreateEntryDto> Entries { get; init; }
}
