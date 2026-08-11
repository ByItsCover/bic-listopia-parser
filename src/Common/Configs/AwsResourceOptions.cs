namespace Common.Configs;

public class AwsResourceOptions
{
    public required string AwsRegion { get; init; }
    public required string DumpBucketName { get; init; }
    public string? CoverDbUri { get; init; }
}