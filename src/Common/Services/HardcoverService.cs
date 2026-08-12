using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Common.Configs;
using Common.Interfaces;
using Common.Entities;
using Common.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Services;

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

    public async Task<IEnumerable<Cover>> GetCoversByIsbn(IEnumerable<string> isbnList, CancellationToken cancellationToken)
    {
        var editionsFromIsbnRequest = new GraphQLRequest
        {
            Query = """
                    query GetEditionsFromISBN($isbn_list: [String]) {
                        books(
                            where: { 
                                default_cover_edition: { isbn_13: { _in: $isbn_list } } 
                            }
                        ) {
                            id
                            title
                            
                            default_cover_edition {
                                id
                                isbn_13
                                image {
                                    url
                                }
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

        var response = await _client.SendQueryAsync<BooksResponse>(editionsFromIsbnRequest, cancellationToken);

        RequestUtils.HandleGqlErrors(response);

        var covers = response.Data.Books
            .Where(b => b.DefaultCoverEdition?.Image?.Url != null)
            .Select(b => new Cover
            {
                CoverId = b.DefaultCoverEdition!.Id,
                BookId = b.Id,
                Isbn13 = b.DefaultCoverEdition.Isbn13,
                CoverUrl = b.DefaultCoverEdition.Image!.Url
            });
        
        _logger.LogInformation("Retrieved {Count} books", response.Data.Books.Count);
        
        return covers;
    }

    public async Task<IEnumerable<Cover>> GetPopularCovers(int count, CancellationToken cancellationToken)
    {
        var popularCoversRequest = new GraphQLRequest
        {
            Query = """
                    query PopularCovers($popular_count: Int) {
                        books(
                            order_by: [{users_count: desc}]
                            limit: $popular_count
                        ) {
                            id
                            title
                            users_count
                            
                            default_cover_edition {
                                id
                                isbn_13
                                image {
                                    url
                                }
                            }
                        }
                    }
                    """,
            OperationName = "PopularCovers",
            Variables = new
            {
                popular_count = count
            }
        };

        var response = await _client.SendQueryAsync<BooksResponse>(popularCoversRequest, cancellationToken);

        RequestUtils.HandleGqlErrors(response);

        var covers = response.Data.Books
            .Where(b => b.DefaultCoverEdition?.Image?.Url != null)
            .Select(b => new Cover
            {
                CoverId = b.DefaultCoverEdition!.Id,
                BookId = b.Id,
                Isbn13 = b.DefaultCoverEdition.Isbn13,
                CoverUrl = b.DefaultCoverEdition.Image!.Url,
                UsersCount = b.UsersCount
            });
        
        _logger.LogInformation("Retrieved {Count} popular books", response.Data.Books.Count);
        
        return covers;
    }
    
    public async Task<IEnumerable<Cover>> GetTrendingCovers(int count, CancellationToken cancellationToken)
    {
        var trendingBookIdsRequest = new GraphQLRequest
        {
            Query = """
                    query TrendingBookIds($trending_count: Int) {
                        books_trending(
                            duration: month,
                            limit: $trending_count
                        ) {
                            ids
                        }
                    }
                    """,
            OperationName = "TrendingBookIds",
            Variables = new
            {
                trending_count = count
            }
        };

        var idsResponse = await _client.SendQueryAsync<TrendingIdsResponse>(trendingBookIdsRequest, cancellationToken);

        RequestUtils.HandleGqlErrors(idsResponse);
        if (idsResponse.Data.BooksTrending.Ids.Count == 0)
        {
            _logger.LogWarning("No trending books found");
            return [];
        }
        
        var trendingCoversRequest = new GraphQLRequest
        {
            Query = """
                    query TrendingCovers($id_list: [Int]) {
                        books(
                            where: {
                                id: {_in: $id_list}
                            }
                        ) {
                            id
                            title
                            users_count
                            
                            default_cover_edition {
                                id
                                isbn_13
                                image {
                                    url
                                }
                            }
                        }
                    }
                    """,
            OperationName = "TrendingCovers",
            Variables = new
            {
                id_list = idsResponse.Data.BooksTrending.Ids
            }
        };

        var coversResponse = await _client.SendQueryAsync<BooksResponse>(trendingCoversRequest, cancellationToken);

        RequestUtils.HandleGqlErrors(coversResponse);

        var covers = coversResponse.Data.Books
            .Where(b => b.DefaultCoverEdition?.Image?.Url != null)
            .Select(b => new Cover
            {
                CoverId = b.DefaultCoverEdition!.Id,
                BookId = b.Id,
                Isbn13 = b.DefaultCoverEdition.Isbn13,
                CoverUrl = b.DefaultCoverEdition.Image!.Url,
                UsersCount = b.UsersCount
            });
        
        _logger.LogInformation("Retrieved {Count} trending books", coversResponse.Data.Books.Count);
        
        return covers;
    }
}