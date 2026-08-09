using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace ListopiaParser.Tests;

public class ListopiaParserRunnerTests
{
    private Mock<IHostApplicationLifetime> _lifetimeMock;
    private Mock<HttpMessageHandler> _handlerMock;
    private Mock<IListopiaService> _listopiaServiceMock;
    private Mock<IHardcoverService> _hardcoverServiceMock;
    private Mock<IAmazonS3> _s3ClientMock;
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
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _listopiaServiceMock = new Mock<IListopiaService>();
        _hardcoverServiceMock = new Mock<IHardcoverService>();
        _s3ClientMock = new Mock<IAmazonS3>();
        _loggerMock = new Mock<ILogger<ListopiaParserRunner>>();
        _listopiaOptionValues = new ListopiaOptions
        {
            GoodreadsBase = "https://www.goodreads.com",
            ListopiaUrl = "https://www.goodreads.com/list/show/001.TestList",
            BucketName = "cover_dump",
            PageStart = 1,
            PageCount = 10,
            MaxParallelCount = 2
        };
        _listopiaOptions = Options.Create(_listopiaOptionValues);

        _listopiaServiceMock
            .Setup(x => x.GetListopiaIsbns(
                It.IsInRange(_listopiaOptionValues.PageStart,
                    _listopiaOptionValues.PageStart + _listopiaOptionValues.PageCount - 1, Moq.Range.Inclusive),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(Enumerable.Repeat(Task.FromResult<string?>("abc123"), PageSize).ToList());
        _hardcoverServiceMock
            .Setup(x => x.GetBookCovers(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Repeat(new Cover
            {
                CoverId = 1,
                BookId = 10,
                Isbn13 = "abc123",
                CoverUrl = "https://www.goodreads.com/my-image"
            }, PageSize).ToList());
        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK
            });
        
        _services = new ServiceCollection();
        
        _services.AddSingleton<IHostedService, ListopiaParserRunner>();
        _services.AddSingleton(_lifetimeMock.Object);
        _services.AddSingleton(new HttpClient(_handlerMock.Object));
        _services.AddSingleton(_listopiaServiceMock.Object);
        _services.AddSingleton(_hardcoverServiceMock.Object);
        _services.AddSingleton(_s3ClientMock.Object);
        _services.AddSingleton(_listopiaOptions);
        _services.AddSingleton(_loggerMock.Object);
        
        var serviceProvider = _services.BuildServiceProvider();
        _sut = serviceProvider.GetService<IHostedService>();
    }
    
    [Test]
    public async Task TestExecuteAsync()
    {
        var expectedS3Calls = PageSize * _listopiaOptionValues.PageCount;
        
        Assert.That(_sut, Is.Not.Null);

        await _sut.StartAsync(CancellationToken.None);
        await Task.Delay(500, CancellationToken.None);
        await _sut.StopAsync(CancellationToken.None);
        
        _lifetimeMock.Verify(x => x.StopApplication(),
            Times.Once);
        _listopiaServiceMock.Verify(x => x.GetListopiaIsbns(
            It.IsInRange(_listopiaOptionValues.PageStart, _listopiaOptionValues.PageStart + _listopiaOptionValues.PageCount - 1, Moq.Range.Inclusive),
            It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.PageCount));
        _hardcoverServiceMock.Verify(x => x.GetBookCovers(
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.PageCount));
        _s3ClientMock.Verify(x => x.PutObjectAsync(
                It.IsAny<PutObjectRequest>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(expectedS3Calls));
    }
}