using Amazon.SQS;
using Amazon.SQS.Model;
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
    private readonly IAmazonSQS _sqsClient;
    private readonly ListopiaOptions _listopiaOptions;
    private readonly ILogger<ListopiaParserRunner> _logger;

    public ListopiaParserRunner(IHostApplicationLifetime lifetime, HttpClient httpClient, IListopiaService listopiaService,
        IHardcoverService hardcoverService, IAmazonSQS sqsClient, IOptions<ListopiaOptions> listopiaOptions,
        ILogger<ListopiaParserRunner> logger)
    {
        _lifetime = lifetime;
        _client = httpClient;
        _listopiaService = listopiaService;
        _hardcoverService = hardcoverService;
        _sqsClient = sqsClient;
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
            var pages = Enumerable.Range(_listopiaOptions.PageStart, _listopiaOptions.PageCount).ToList();
            var embeddingsUploaded = 0;
            
            await Parallel.ForEachAsync(pages, options, async (page, token) =>
            {
                var isbnList = await _listopiaService.GetListopiaIsbns(page, token);
                var editions = await _hardcoverService.GetBookEditions(isbnList, token);
                
                try
                {
                    var editionChunks = editions.Chunk(Constants.SqsMessageLimit).ToList();
                    
                    foreach (var chunk in editionChunks)
                    {
                        var messages = chunk.Select(e => new SendMessageBatchRequestEntry
                        {
                            Id = $"{e.Id}-{e.Isbn13}",
                            MessageAttributes = new Dictionary<string, MessageAttributeValue>
                            {
                                {"cover_id", new MessageAttributeValue
                                {
                                    DataType = "Number",
                                    StringValue = e.Id.ToString()
                                }},
                                {"book_id", new MessageAttributeValue
                                {
                                    DataType = "Number",
                                    StringValue = e.BookId.ToString()
                                }},
                                {"isbn_13", new MessageAttributeValue
                                {
                                    DataType = "String",
                                    StringValue = e.Isbn13
                                }},
                                {"image_url", new MessageAttributeValue
                                {
                                    DataType = "String",
                                    StringValue = e.Image?.Url
                                }}
                            }
                        }).ToList();
                        var imageTasks = messages.Select(async (m, i) =>
                        {
                            messages[i].MessageBody = await FetchBase64(chunk[i].Image?.Url, token);
                        });
                        await Task.WhenAll(imageTasks);
                        
                        if (chunk.Length > 0)
                        {
                            var temp = await FetchBase64(chunk[0].Image?.Url, token);
                        }
                        
                        
                        var batchRequest = new SendMessageBatchRequest
                        {
                            QueueUrl = _listopiaOptions.SqsUrl,
                            Entries = messages
                        };
                    
                        var batchResponse = await _sqsClient.SendMessageBatchAsync(batchRequest, token);
                        embeddingsUploaded += batchResponse.Successful.Count;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error: {Message}", e.Message);
                }
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

    private async Task<string> FetchBase64(string? url, CancellationToken cancellationToken)
    {
        var image = await _client.GetByteArrayAsync(url, cancellationToken);
        return Convert.ToBase64String(image);
    }
}