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
        
        var client = new HttpClient(_mockHttp);
        _sut = new HardcoverService(client, _options, _loggerMock.Object);
    }

    [Test]
    public async Task TestGetBookEditions()
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
    
    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
    }
}