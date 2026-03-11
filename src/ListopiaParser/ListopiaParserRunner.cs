using Amazon.SQS;
using Amazon.SQS.Model;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
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

        try
        {
            var pages = Enumerable.Range(1, _listopiaOptions.Pages);
            var hardcoverTaskList = new List<Task<List<Edition>>>();

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

            var embeddingsUploaded = 0;
            await foreach (var hardcoverTask in Task.WhenEach(hardcoverTaskList).WithCancellation(cancellationToken))
            {
                try
                {
                    var editionChunks = (await hardcoverTask).Chunk(Constants.SqsMessageLimit);
                    var sqsTaskList = new List<Task<SendMessageBatchResponse>>();
                    
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
                    
                        var sqsTask = _sqsClient.SendMessageBatchAsync(batchRequest, cancellationToken);
                        sqsTaskList.Add(sqsTask);
                    }

                    var batchResponses = await Task.WhenAll(sqsTaskList);
                    foreach (var batchResponse in batchResponses)
                    {
                        embeddingsUploaded += batchResponse.Successful.Count;
                    }
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