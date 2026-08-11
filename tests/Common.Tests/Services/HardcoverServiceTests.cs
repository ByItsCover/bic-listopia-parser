using AwesomeAssertions;
using Common.Configs;
using Common.Entities;
using Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace Common.Tests.Services;

public class HardcoverServiceTests
{
    private IOptions<HardcoverOptions> _options;
    private HardcoverOptions _optionValues;
    private MockHttpMessageHandler _mockHttp;
    private Mock<ILogger<HardcoverService>> _loggerMock;
    private HardcoverService _sut;
    
    [SetUp]
    public void Setup()
    {
        _optionValues = new HardcoverOptions
        {
            HardcoverUrl = "https://api.hardcover.app/v1/graphql",
            Token = "randomToken"
        };
        _options = Options.Create(_optionValues);
        _mockHttp = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<HardcoverService>>();
        
        _sut = new HardcoverService(new HttpClient(_mockHttp), _options, _loggerMock.Object);
    }

    [Test]
    public async Task TestGetCoversByIsbn()
    {
        var isbnList = new List<string> { "9780439023481" };
        var expectedCovers = new List<Cover>
        {
            new()
            {
                CoverId = 1589497,
                BookId = 88639,
                Isbn13 = "9780439023481",
                CoverUrl = "https://assets.hardcover.app/editions/1589497/2979196565308831-lf%202.jpeg"
            }
        };
        var expectedRequest = _mockHttp.Expect(_optionValues.HardcoverUrl)
            .WithJsonContent(new
            {
                
                query = """
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
                operationName = "GetEditionsFromISBN",
                variables = new {
                    isbn_list = isbnList
                }
            })
            .Respond("application/json", """
                 {
                     "data": {
                         "books": [
                             {
                                 "id": 88639,
                                 "title": "The Hunger Games",
                                 "default_cover_edition": {
                                   "id": 1589497,
                                   "isbn_13": "9780439023481",
                                   "image": {
                                     "url": "https://assets.hardcover.app/editions/1589497/2979196565308831-lf%202.jpeg"
                                   }
                                 }
                             }
                         ]
                     }
                 }
                 """);
        
        var covers = await _sut.GetCoversByIsbn(isbnList, CancellationToken.None);
        var coverList = covers.ToList();
        
        Assert.That(_mockHttp.GetMatchCount(expectedRequest), Is.EqualTo(1));
        Assert.That(coverList, Is.Not.Null);
        Assert.That(coverList.Count, Is.EqualTo(1));
        coverList.Should().BeEquivalentTo(expectedCovers);
    }
    
    [Test]
    public async Task TestGetPopularCovers()
    {
        var popularCount = 25;
        var popularCover = new Cover()
        {
            CoverId = 1589497,
            BookId = 88639,
            Isbn13 = "9780439023481",
            CoverUrl = "https://assets.hardcover.app/editions/1589497/2979196565308831-lf%202.jpeg",
            UsersCount = 10896
        };
        var expectedCovers = Enumerable.Repeat(popularCover, popularCount);
        var expectedRequest = _mockHttp.Expect(_optionValues.HardcoverUrl)
            .WithJsonContent(new
            {
                
                query = """
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
                operationName = "PopularCovers",
                variables = new {
                    popular_count = popularCount
                }
            })
            .Respond("application/json", $$"""
                 {
                     "data": {
                         "books": [
                            {{DuplicateEntry("""
                             {
                                "id": 88639,
                                "title": "The Hunger Games",
                                "users_count": 10896,
                                "default_cover_edition": {
                                    "id": 1589497,
                                    "isbn_13": "9780439023481",
                                    "image": {
                                        "url": "https://assets.hardcover.app/editions/1589497/2979196565308831-lf%202.jpeg"
                                    }
                                }
                             }
                            """, popularCount)}}
                         ]
                     }
                 }
                 """);
        
        var covers = await _sut.GetPopularCovers(popularCount, CancellationToken.None);
        var coverList = covers.ToList();
        
        Assert.That(_mockHttp.GetMatchCount(expectedRequest), Is.EqualTo(1));
        Assert.That(coverList, Is.Not.Null);
        Assert.That(coverList.Count, Is.EqualTo(popularCount));
        Assert.That(coverList.All(c => c.UsersCount.HasValue));
        coverList.Should().BeEquivalentTo(expectedCovers);
    }
    
    [Test]
    public async Task TestGetTrendingCovers()
    {
        var trendingCount = 25;
        var trendingCover = new Cover()
        {
            CoverId = 3274049,
            BookId = 427578,
            Isbn13 = "9780593135204",
            CoverUrl = "https://assets.hardcover.app/editions/3274049/8741341047797682-91mYu67RfUL._SL1500_.jpg",
            UsersCount = 16996
        };
        var idList = Enumerable.Repeat(trendingCover.BookId, trendingCount).ToList();
        var expectedCovers = Enumerable.Repeat(trendingCover, trendingCount);
        var expectedIdsRequest = _mockHttp.Expect(_optionValues.HardcoverUrl)
            .WithJsonContent(new
            {
                
                query = """
                        query TrendingBookIds($trending_count: Int) {
                            books_trending(
                                duration: month,
                                limit: $trending_count
                            ) {
                                ids
                            }
                        }
                        """,
                operationName = "TrendingBookIds",
                variables = new {
                    trending_count = trendingCount
                }
            })
            .Respond("application/json", $$"""
                 {
                     "data": {
                         "books_trending": {
                            "ids": [
                                {{DuplicateEntry(trendingCover.BookId.ToString(), trendingCount)}}
                            ]
                         }
                     }
                 }
                 """);
        var expectedCoversRequest = _mockHttp.Expect(_optionValues.HardcoverUrl)
            .WithJsonContent(new
            {
                
                query = """
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
                operationName = "TrendingCovers",
                variables = new {
                    id_list = idList
                }
            })
            .Respond("application/json", $$"""
                 {
                     "data": {
                         "books": [
                            {{DuplicateEntry("""
                             {
                                "id": 427578,
                                "title": "Project Hail Mary",
                                "users_count": 16996,
                                "default_cover_edition": {
                                    "id": 3274049,
                                    "isbn_13": "9780593135204",
                                    "image": {
                                      "url": "https://assets.hardcover.app/editions/3274049/8741341047797682-91mYu67RfUL._SL1500_.jpg"
                                    }
                                }
                            }
                            """, trendingCount)}}
                         ]
                     }
                 }
                 """);
        
        var covers = await _sut.GetTrendingCovers(trendingCount, CancellationToken.None);
        var coverList = covers.ToList();
        
        Assert.That(_mockHttp.GetMatchCount(expectedIdsRequest), Is.EqualTo(1));
        Assert.That(_mockHttp.GetMatchCount(expectedCoversRequest), Is.EqualTo(1));
        Assert.That(coverList.Count, Is.EqualTo(trendingCount));
        Assert.That(coverList.All(c => c.UsersCount.HasValue));
        coverList.Should().BeEquivalentTo(expectedCovers);
    }
    
    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
    }

    private static string DuplicateEntry(string entry, int times)
    {
        var entryList = Enumerable.Repeat(entry, times);
        return string.Join(",\n", entryList);
    }
}