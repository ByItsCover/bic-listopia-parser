using ListopiaParser.ResponseTypes;
using ListopiaParser.Services;
using Microsoft.Extensions.Hosting;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly ListopiaService _listopiaService;
    private readonly HardcoverService _hardcoverService;
    private readonly ClipService _clipService;
    private const int Pages = 2;

    public ListopiaParserRunner(ListopiaService listopiaService,  HardcoverService hardcoverService,  ClipService clipService)
    {
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _clipService = clipService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Howdy");

        var embeddingsUploaded = 0;
        var pages = Enumerable.Range(1, Pages);
        var hardcoverTaskList = new List<Task<List<Edition>>>();
        var clipTaskList = new List<Task<EmbeddingsResponse>>();
        
        var isbnsTaskList = pages.Select(x => _listopiaService.GetListopiaIsbns(x, cancellationToken));
        
        await foreach (var isbnsTask in Task.WhenEach(isbnsTaskList))
        {
            var editionsTask = _hardcoverService.GetBookEditions(await isbnsTask, cancellationToken);
            hardcoverTaskList.Add(editionsTask);
        }

        await foreach (var hardcoverTask in Task.WhenEach(hardcoverTaskList))
        {
            var embeddingsTask = _clipService.GetCoverEmbeddings(await hardcoverTask, cancellationToken);
            clipTaskList.Add(embeddingsTask);
        }
        
        await foreach (var clipTask in Task.WhenEach(clipTaskList))
        {
            var embeddings = await clipTask;
            embeddingsUploaded += embeddings.ImageEmbeddings.Count(x => x != null);
            // var embeddingsTask = _clipService.GetCoverEmbeddings(await hardcoverTask, cancellationToken);
            // clipTaskList.Add(embeddingsTask);
        }
        
        Console.WriteLine("Number of embeddings to upload: " + embeddingsUploaded);
    }
}