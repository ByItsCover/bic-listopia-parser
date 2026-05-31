using AwesomeAssertions;
using ListopiaParser.Configs;
using ListopiaParser.ResponseTypes;
using ListopiaParser.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace ListopiaParser.Tests.Services;

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
        var isbnList = new List<string> { "1111111111111" };
        var expectedEditions = new List<Edition>
        {
            new()
            {
                Id = 1,
                Image = new EditionImage()
                {
                    Url = "https://www.randomsite.com/test.png"
                },
                Isbn13 = "1111111111111",
                BookId = 10
            }
        };
        var expectedRequest = _mockHttp.Expect(_optionValues.HardcoverUrl)
            .WithJsonContent(new
            {
                
                query = """
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
                operationName = "GetEditionsFromISBN",
                variables = new {
                    isbn_list = new[]
                    {
                        "1111111111111"
                    } 
                }
            })
            .Respond("application/json", """
                 {
                     "data": {
                         "editions": [
                             {
                                 "id": 1,
                                 "isbn_13": "1111111111111",
                                 "book_id": 10,
                                 "asin": null,
                                 "image": {
                                     "url": "https://www.randomsite.com/test.png"
                                 }
                             }
                         ]
                     }
                 }
                 """);
        
        var editionsList = await _sut.GetBookEditions(isbnList, CancellationToken.None);
        
        Assert.That(_mockHttp.GetMatchCount(expectedRequest), Is.EqualTo(1));
        Assert.That(editionsList, Is.Not.Null);
        Assert.That(editionsList.Count, Is.EqualTo(1));
        editionsList.Should().BeEquivalentTo(expectedEditions);
    }
    
    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
    }
}