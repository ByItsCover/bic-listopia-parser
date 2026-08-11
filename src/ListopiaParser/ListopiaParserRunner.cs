using Common.Configs;
using Common.Interfaces;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IListopiaService _listopiaService;
    private readonly IHardcoverService _hardcoverService;
    private readonly ICoverDumpService _coverDumpService;
    private readonly ListopiaOptions _listopiaOptions;
    private readonly AwsResourceOptions _awsResourceOptions;
    private readonly ParallelOptions _parallelOptions;
    private readonly ILogger<ListopiaParserRunner> _logger;

    public ListopiaParserRunner(IHostApplicationLifetime lifetime, IListopiaService listopiaService,
        IHardcoverService hardcoverService, ICoverDumpService coverDumpService, 
        IOptions<ListopiaOptions> listopiaOptions, IOptions<AwsResourceOptions> awsResourceOptions,
        ILogger<ListopiaParserRunner> logger)
    {
        _lifetime = lifetime;
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _coverDumpService = coverDumpService;
        _listopiaOptions = listopiaOptions.Value;
        _awsResourceOptions = awsResourceOptions.Value;
        _parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _listopiaOptions.MaxParallelCount
        };
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listopia Parser starting...");
        _parallelOptions.CancellationToken = cancellationToken;

        try
        {
            var pages = Enumerable.Range(
                _listopiaOptions.PageStart,
                _listopiaOptions.PageCount
            );
            var coversUploaded = 0;
            
            await Parallel.ForEachAsync(pages, _parallelOptions, async (page, token) =>
            {
                var isbnTasks = await _listopiaService.GetListopiaIsbns(page, token);
                var isbnList = (await Task.WhenAll(isbnTasks))
                    .Where(s => s != null)
                    .ToList();
                var coverTasks = _hardcoverService.GetCoversByIsbn(isbnList!, token);

                coversUploaded += await _coverDumpService.DumpCovers(
                    coverTasks,
                    _awsResourceOptions.DumpBucketName,
                    token
                );
            });

            _logger.LogInformation("Number of covers uploaded: {Count}", coversUploaded);
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