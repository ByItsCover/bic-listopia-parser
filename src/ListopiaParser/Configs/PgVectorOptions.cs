using Microsoft.Extensions.Configuration;

namespace ListopiaParser.Configs;

public class PgVectorOptions
{
    public required int VectorDimensions  { get; set; }
    public required string CollectionName  { get; set; }
    [ConfigurationKeyName("SQS_URL")]
    public required string SQSUrl  { get; set; }
}