using DevHabit.Api.DTOs.Common;

namespace DevHabit.Api.DTOs.Tags;

public sealed class TagsCollectionDto : ICollectionResponse<TagDto>
{
    public List<TagDto> Items { get; init; }
}
