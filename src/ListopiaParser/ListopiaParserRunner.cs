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
    private readonly IListopiaService _listopiaService;
    private readonly IHardcoverService _hardcoverService;
    private readonly IAmazonSQS _sqsClient;
    private readonly ListopiaOptions _listopiaOptions;
    private readonly ILogger<ListopiaParserRunner> _logger;

    public ListopiaParserRunner(IHostApplicationLifetime lifetime, IListopiaService listopiaService,
        IHardcoverService hardcoverService, IAmazonSQS sqsClient, IOptions<ListopiaOptions> listopiaOptions,
        ILogger<ListopiaParserRunner> logger)
    {
        _lifetime = lifetime;
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
                        var messages = chunk.Select(x => new SendMessageBatchRequestEntry
                        {
                            Id = $"{x.Id}-{x.Isbn13}",
                            MessageBody = x.Image?.Url,
                            MessageAttributes = new Dictionary<string, MessageAttributeValue>
                            {
                                {"cover_id", new MessageAttributeValue
                                {
                                    DataType = "Number",
                                    StringValue = x.Id.ToString()
                                }},
                                {"book_id", new MessageAttributeValue
                                {
                                    DataType = "Number",
                                    StringValue = x.BookId.ToString()
                                }},
                                {"isbn_13", new MessageAttributeValue
                                {
                                    DataType = "String",
                                    StringValue = x.Isbn13
                                }}
                            }
                        }).ToList();
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
}