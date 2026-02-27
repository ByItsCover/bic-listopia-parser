using System.Text.Json.Serialization;

namespace ListopiaParser.ResponseTypes;

public class EditionsResponse
{
    public required List<Edition> Editions { get; init; }
}

public class Edition
{
    public required int Id  { get; init; }
    [JsonPropertyName("isbn_13")]
    public required string Isbn13  { get; init; }
    public required EditionImage? Image { get; init; }
}

public class EditionImage
{
    public required string Url { get; init; }
}