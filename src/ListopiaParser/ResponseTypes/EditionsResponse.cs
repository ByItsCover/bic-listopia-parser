using System.Text.Json.Serialization;

namespace ListopiaParser.ResponseTypes;

public class EditionsResponse
{
    public required List<Edition> Editions { get; init; }
}

public class Edition
{
    [JsonPropertyName("id")]
    public required int Id  { get; init; }
    [JsonPropertyName("isbn_13")]
    public required string Isbn13  { get; init; }
    [JsonPropertyName("image")]
    public required EditionImage? Image { get; init; }
}

public class EditionImage
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}