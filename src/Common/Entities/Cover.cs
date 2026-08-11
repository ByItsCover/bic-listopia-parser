namespace Common.Entities;

public class Cover
{
    public required int CoverId { get; init; }
    public required int BookId { get; init; }
    public required string Isbn13 { get; init; }
    public required string CoverUrl { get; init; }
    public int? UsersCount { get; init; }
}