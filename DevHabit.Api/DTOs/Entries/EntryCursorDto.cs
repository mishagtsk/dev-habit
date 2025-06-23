using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace DevHabit.Api.DTOs.Entries;

public sealed record EntryCursorDto(string Id, DateOnly Date)
{
    public static string Encode(string id, DateOnly date)
    {
        var cursor = new EntryCursorDto(id, date);
        string json = JsonSerializer.Serialize(cursor);
        
        return Base64UrlEncoder.Encode(json);
    }

    public static EntryCursorDto? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string json = Base64UrlEncoder.Decode(cursor);
            return JsonSerializer.Deserialize<EntryCursorDto>(json);
        }
        catch
        {
            return null;
        }
    }
}
