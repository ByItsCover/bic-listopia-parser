namespace ListopiaParser.Configs;

public class ListopiaOptions
{
    public required string ListopiaUrl { get; init; }
    public required string GoodreadsBase { get; init; }
    public required int PageStart  { get; init; }
    public required int PageCount  { get; init; }
    public required int MaxParallelCount { get; init; }
}