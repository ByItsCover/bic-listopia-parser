using System.Text.Json.Serialization;

namespace Common.Entities;

public class BooksResponse
{
    public required List<Book> Books { get; init; }
}

public class Book
{
    public required int Id { get; init; }
    public required string? Title { get; init; }
    [JsonPropertyName("users_count")]
    public int UsersCount { get; init; }
    [JsonPropertyName("default_cover_edition")]
    public required Edition? DefaultCoverEdition { get; init; }
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