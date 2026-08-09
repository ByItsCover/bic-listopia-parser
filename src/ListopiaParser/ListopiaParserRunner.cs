using Amazon.S3;
using Amazon.S3.Model;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ListopiaParser;

public class ListopiaParserRunner : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly HttpClient _client;
    private readonly IListopiaService _listopiaService;
    private readonly IHardcoverService _hardcoverService;
    private readonly IAmazonS3 _s3Client;
    private readonly ListopiaOptions _listopiaOptions;
    private readonly ILogger<ListopiaParserRunner> _logger;

    public ListopiaParserRunner(IHostApplicationLifetime lifetime, HttpClient httpClient, IListopiaService listopiaService,
        IHardcoverService hardcoverService, IAmazonS3 s3Client, IOptions<ListopiaOptions> listopiaOptions,
        ILogger<ListopiaParserRunner> logger)
    {
        _lifetime = lifetime;
        _client = httpClient;
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _s3Client = s3Client;
        _listopiaOptions = listopiaOptions.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listopia Parser starting...");
        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _listopiaOptions.MaxParallelCount,
            CancellationToken = cancellationToken
        };

        try
        {
            var pages = Enumerable.Range(_listopiaOptions.PageStart, _listopiaOptions.PageCount);
            var embeddingsUploaded = 0;
            
            await Parallel.ForEachAsync(pages, options, async (page, token) =>
            {
                var isbnTasks = await _listopiaService.GetListopiaIsbns(page, token);
                var isbnList = (await Task.WhenAll(isbnTasks))
                    .Where(s => s != null)
                    .ToList();
                var covers = await _hardcoverService.GetBookCovers(isbnList!, token);

                var s3Tasks = covers.Select(async c =>
                {
                    var request = new PutObjectRequest
                    {
                        BucketName = _listopiaOptions.BucketName,
                        Key = $"{c.CoverId}-{c.Isbn13}.bin",
                        InputStream = await FetchStream(c.CoverUrl, token)
                    };
                    request.Metadata.Add("cover_id", c.CoverId.ToString());
                    request.Metadata.Add("book_id", c.BookId.ToString());
                    request.Metadata.Add("isbn_13", c.Isbn13);
                    request.Metadata.Add("image_url", c.CoverUrl);

                    try
                    {
                        await _s3Client.PutObjectAsync(request, token);
                        embeddingsUploaded += 1;
                    }
                    catch (AmazonS3Exception e)
                    {
                        _logger.LogError(e, "Error: {Message}", e.Message);
                    }
                });
                
                await Task.WhenAll(s3Tasks);
            });

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

    private async Task<Stream> FetchStream(string url, CancellationToken cancellationToken)
    {
        var image = await _client.GetByteArrayAsync(url, cancellationToken);
        return new MemoryStream(image);
    }
}