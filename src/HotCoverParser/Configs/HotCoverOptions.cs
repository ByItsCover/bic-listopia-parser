namespace HotCoverParser.Configs;

public class HotCoverOptions
{
    public required int PopularCount { get; init; }
    public required int TrendingCount { get; init; }
    
    public const string HotCoversTableName = "hot_covers";
}