using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ListopiaParser.Services;

public class HardcoverService : IHardcoverService
{
    private readonly GraphQLHttpClient _client;
    private readonly ILogger<HardcoverService> _logger;
    
    public HardcoverService(HttpClient httpClient, IOptions<HardcoverOptions> hardcoverOptions, ILogger<HardcoverService> logger)
    {
        var options = hardcoverOptions.Value;
        _logger = logger;
        
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + options.Token);
        _client = new GraphQLHttpClient(
            new Uri(options.HardcoverUrl),
            new SystemTextJsonSerializer(),
            httpClient
        );
    }

    public async Task<List<Edition>> GetBookEditions(IEnumerable<string> isbnList, CancellationToken cancellationToken)
    {
        var editionsFromIsbnRequest = new GraphQLRequest
        {
            Query = """
                    query GetEditionsFromISBN($isbn_list: [String]) {
                        editions(where: { isbn_13: { _in: $isbn_list } }) {
                            id
                            isbn_13
                            book_id
                            image {
                                url
                            }
                        }
                    }
                    """,
            OperationName = "GetEditionsFromISBN",
            Variables = new
            {
                isbn_list = isbnList
            }
        };

        var response = await _client.SendQueryAsync<EditionsResponse>(editionsFromIsbnRequest, cancellationToken);
        
        if (response.Errors != null && response.Errors.Any())
        {
            var responseDetails = response.AsGraphQLHttpResponse();
            var exceptions = response.Errors
                .Select(e =>
                    new GraphQLHttpRequestException(responseDetails.StatusCode, responseDetails.ResponseHeaders,
                        e.Message));
            throw new AggregateException(exceptions);
        }
        
        _logger.LogInformation("Retrieved {Count} editions", response.Data.Editions.Count);
        
        return response.Data.Editions;
    }
}