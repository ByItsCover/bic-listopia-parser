using Common.Configs;
using Common.Entities;
using Common.Interfaces;
using HotCoverParser.Configs;
using HotCoverParser.Interfaces;
using HotCoverParser.Tables;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotCoverParser;

public class HotCoverParserRunner : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHardcoverService _hardcoverService;
    private readonly ICoverDumpService _coverDumpService;
    private readonly Task<IHotCoversTable> _hotCoversTableTask;
    private readonly HotCoverOptions _hotCoverOptions;
    private readonly AwsResourceOptions _awsResourceOptions;
    private readonly ILogger<HotCoverParserRunner> _logger;
    
    public HotCoverParserRunner(IHostApplicationLifetime lifetime,
        IHardcoverService hardcoverService, ICoverDumpService coverDumpService,
        Task<IHotCoversTable> hotCoversTableTask, IOptions<HotCoverOptions> hotCoverOptions,
        IOptions<AwsResourceOptions> awsResourceOptions, ILogger<HotCoverParserRunner> logger)
    {
        _lifetime = lifetime;
        _hardcoverService = hardcoverService;
        _coverDumpService = coverDumpService;
        _hotCoversTableTask = hotCoversTableTask;
        _hotCoverOptions = hotCoverOptions.Value;
        _awsResourceOptions = awsResourceOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hot Cover Parser starting...");

        try
        {
            var popularCoversTask = _hardcoverService.GetPopularCovers(_hotCoverOptions.PopularCount, cancellationToken);
            var trendingCoversTask = _hardcoverService.GetTrendingCovers(_hotCoverOptions.TrendingCount, cancellationToken);
            
            var coverDumpTask = DumpAllCovers(popularCoversTask, trendingCoversTask, cancellationToken);
            var popularInsertedTask = InsertPopularCovers(popularCoversTask);
            var trendingInsertedTask = InsertTrendingCovers(popularCoversTask);

            await Task.WhenAll(
                coverDumpTask,
                popularInsertedTask,
                trendingInsertedTask
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error: {Message}", e.Message);
        }
        finally
        {
            _logger.LogInformation("Hot Cover Parser completed");
            _lifetime.StopApplication();
        }
    }

    private async Task<int> DumpAllCovers(Task<IEnumerable<Cover>> popularCoversTask, Task<IEnumerable<Cover>> trendingCoversTask, CancellationToken token)
    {
        var popularCovers = await popularCoversTask;
        var trendingCovers = await trendingCoversTask;
        
        return await _coverDumpService.DumpCovers(
            popularCovers.UnionBy(trendingCovers, c => c.CoverId),
            _awsResourceOptions.DumpBucketName,
            token
        );
    }

    private async Task InsertPopularCovers(Task<IEnumerable<Cover>> coverTasks)
    {
        var hotCoversTable = await _hotCoversTableTask;
        var tableRes = await hotCoversTable.InsertCovers(coverTasks, HotEnum.Popular);
        
        _logger.LogInformation("{Inserted} popular covers inserted", tableRes.NumInsertedRows);
        _logger.LogInformation("{Deleted} popular covers deleted", tableRes.NumDeletedRows);
    }
    
    private async Task InsertTrendingCovers(Task<IEnumerable<Cover>> coverTasks)
    {
        var hotCoversTable = await _hotCoversTableTask;
        var tableRes = await hotCoversTable.InsertCovers(coverTasks, HotEnum.Trending);
        
        _logger.LogInformation("{Inserted} trending covers inserted", tableRes.NumInsertedRows);
        _logger.LogInformation("{Deleted} trending covers deleted", tableRes.NumDeletedRows);
    }
}
