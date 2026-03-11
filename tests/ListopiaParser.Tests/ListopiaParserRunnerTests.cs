using Amazon.SQS;
using Amazon.SQS.Model;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ListopiaParser.Tests;

public class ListopiaParserRunnerTests
{
    private Mock<IHostApplicationLifetime> _lifetimeMock;
    private Mock<IListopiaService> _listopiaServiceMock;
    private Mock<IHardcoverService> _hardcoverServiceMock;
    private Mock<IAmazonSQS> _sqsClientMock;
    private IOptions<ListopiaOptions> _listopiaOptions;
    private ListopiaOptions _listopiaOptionValues;
    private Mock<ILogger<ListopiaParserRunner>> _loggerMock;
    private IServiceCollection _services;
    private IHostedService? _sut;
    
    private const int PageSize = 52;
    
    [SetUp]
    public void Setup()
    {
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _listopiaServiceMock = new Mock<IListopiaService>();
        _hardcoverServiceMock = new Mock<IHardcoverService>();
        _sqsClientMock = new Mock<IAmazonSQS>();
        _loggerMock = new Mock<ILogger<ListopiaParserRunner>>();
        _listopiaOptionValues = new ListopiaOptions
        {
            GoodreadsBase = "https://www.goodreads.com",
            ListopiaUrl = "https://www.goodreads.com/list/show/001.TestList",
            SqsUrl = "https://sqs.us-east-1.amazonaws.com/123456/my-sqs",
            Pages = 10
        };
        _listopiaOptions = Options.Create(_listopiaOptionValues);

        _hardcoverServiceMock
            .Setup(x => x.GetBookEditions(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Repeat(new Edition
            {
                Id = 1,
                Isbn13 = "abc123",
                Image = new EditionImage
                {
                    Url = "https://www.goodreads.com/my-image"
                }
            }, PageSize).ToList());
        
        _services = new ServiceCollection();
        
        _services.AddSingleton<IHostedService, ListopiaParserRunner>();
        _services.AddSingleton(_lifetimeMock.Object);
        _services.AddSingleton(_listopiaServiceMock.Object);
        _services.AddSingleton(_hardcoverServiceMock.Object);
        _services.AddSingleton(_sqsClientMock.Object);
        _services.AddSingleton(_listopiaOptions);
        _services.AddSingleton(_loggerMock.Object);
        
        var serviceProvider = _services.BuildServiceProvider();
        _sut = serviceProvider.GetService<IHostedService>();
    }
    
    [Test]
    public async Task TestExecuteAsync()
    {
        var expectedSqsCalls = (int) Math.Ceiling(PageSize / (double)Constants.SqsMessageLimit) * _listopiaOptionValues.Pages;
        
        Assert.That(_sut, Is.Not.Null);
        
        await _sut.StartAsync(CancellationToken.None);
        await Task.Delay(500, CancellationToken.None);
        await _sut.StopAsync(CancellationToken.None);
        
        _lifetimeMock.Verify(x => x.StopApplication(),
            Times.Once);
        _listopiaServiceMock.Verify(x => x.GetListopiaIsbns(
            It.IsInRange(1, _listopiaOptionValues.Pages, Moq.Range.Inclusive),
            It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.Pages));
        _hardcoverServiceMock.Verify(x => x.GetBookEditions(
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.Pages));
        _sqsClientMock.Verify(x => x.SendMessageBatchAsync(
                It.IsAny<SendMessageBatchRequest>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(expectedSqsCalls));
    }
}