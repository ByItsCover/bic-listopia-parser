namespace ListopiaParser.Configs;

public class ListopiaOptions
{
    public required string ListopiaUrl { get; set; }
    public required string GoodreadsBase { get; set; }
    public required string BucketName { get; set; }
    public required int PageStart  { get; set; }
    public required int PageCount  { get; set; }
    public required int MaxParallelCount { get; set; }
}