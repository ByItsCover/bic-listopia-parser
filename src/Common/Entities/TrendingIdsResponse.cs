using System.Text.Json.Serialization;

namespace Common.Entities;

public class TrendingIdsResponse
{
    [JsonPropertyName("books_trending")]
    public required BookIds BooksTrending { get; init; }
}

public class BookIds
{
    public required List<int> Ids { get; init; }
}