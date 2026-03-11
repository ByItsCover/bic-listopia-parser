namespace ListopiaParser.Configs;

public class ListopiaOptions
{
    public required string ListopiaUrl { get; set; }
    public required string GoodreadsBase { get; set; }
    public required string SqsUrl { get; set; }
    public required int Pages  { get; set; }
}