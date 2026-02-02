using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IListopiaService _listopiaService;
    private readonly IHardcoverService _hardcoverService;
    private readonly IEmbedService _embedService;
    private readonly PostgresVectorStore _vectorStore;
    private readonly ListopiaOptions _listopiaOptions;
    private readonly PgVectorOptions _pgVectorOptions;
    private readonly ILogger<ListopiaParserRunner> _logger;

    public ListopiaParserRunner(IHostApplicationLifetime lifetime, IListopiaService listopiaService,
        IHardcoverService hardcoverService, IEmbedService embedService, PostgresVectorStore vectorStore,
        IOptions<ListopiaOptions> listopiaOptions, IOptions<PgVectorOptions> pgVectorOptions,
        ILogger<ListopiaParserRunner> logger)
    {
        _lifetime = lifetime;
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _embedService = embedService;
        _vectorStore = vectorStore;
        _listopiaOptions = listopiaOptions.Value;
        _pgVectorOptions = pgVectorOptions.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listopia Parser starting...");

        try
        {
            var collection = _vectorStore.GetCollection<int, Cover>(_pgVectorOptions.CollectionName);
            await collection.EnsureCollectionExistsAsync(cancellationToken);

            var pages = Enumerable.Range(1, _listopiaOptions.Pages);
            var hardcoverTaskList = new List<Task<List<Edition>>>();
            var embedTaskList = new List<Task<IEnumerable<Cover>>>();

            var isbnsTaskList = pages.Select(x => _listopiaService.GetListopiaIsbns(x, cancellationToken));

            await foreach (var isbnsTask in Task.WhenEach(isbnsTaskList).WithCancellation(cancellationToken))
            {
                try
                {
                    var editionsTask = _hardcoverService.GetBookEditions(await isbnsTask, cancellationToken);
                    hardcoverTaskList.Add(editionsTask);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error: {Message}", e.Message);
                }
            }

            await foreach (var hardcoverTask in Task.WhenEach(hardcoverTaskList).WithCancellation(cancellationToken))
            {
                try
                {
                    var coverEmbeddingsTask = _embedService.GetCoverEmbeddings(await hardcoverTask, cancellationToken);
                    embedTaskList.Add(coverEmbeddingsTask);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error: {Message}", e.Message);
                }
            }

            var embeddingsUploaded = 0;
            await foreach (var embedTask in Task.WhenEach(embedTaskList).WithCancellation(cancellationToken))
            {
                try
                {
                    var covers = (await embedTask).ToList();
                    await collection.UpsertAsync(covers, cancellationToken);
                    embeddingsUploaded += covers.Count(x => x.Embedding != null);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error: {Message}", e.Message);
                }
            }

            _logger.LogInformation("Number of embeddings uploaded: {Count}", embeddingsUploaded);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error: {Message}", e.Message);
        }
        finally
        {
            _logger.LogInformation("Listopia Parser completed");
            _lifetime.StopApplication();
        }
    }
}