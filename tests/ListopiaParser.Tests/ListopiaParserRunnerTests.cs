using Common.Configs;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using Common.Entities;
using Common.Interfaces;
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
    private Mock<ICoverDumpService> _coverDumpServiceMock;
    private ListopiaOptions _listopiaOptions;
    private AwsResourceOptions _awsResourceOptions;
    private Mock<ILogger<ListopiaParserRunner>> _loggerMock;
    private IServiceCollection _services;
    private Cover _coverResponse;
    private IHostedService? _sut;
    
    private const int PageSize = 52;
    
    [SetUp]
    public void Setup()
    {
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _listopiaServiceMock = new Mock<IListopiaService>();
        _hardcoverServiceMock = new Mock<IHardcoverService>();
        _coverDumpServiceMock = new Mock<ICoverDumpService>();
        _loggerMock = new Mock<ILogger<ListopiaParserRunner>>();
        
        _listopiaOptions = new ListopiaOptions
        {
            GoodreadsBase = "https://www.goodreads.com",
            ListopiaUrl = "https://www.goodreads.com/list/show/001.TestList",
            PageStart = 1,
            PageCount = 10,
            MaxParallelCount = 2
        };
        _awsResourceOptions = new AwsResourceOptions
        {
            AwsRegion = "us-east-1",
            DumpBucketName = "cover_dump",
            CoverDbUri = "s3://db-bucket/coverdb/"
        };
        
        _coverResponse = new Cover
        {
            CoverId = 1,
            BookId = 10,
            Isbn13 = "abc123",
            CoverUrl = "https://www.goodreads.com/my-image"
        };

        _listopiaServiceMock
            .Setup(x => x.GetListopiaIsbns(
                It.IsInRange(_listopiaOptions.PageStart,
                    _listopiaOptions.PageStart + _listopiaOptions.PageCount - 1, Moq.Range.Inclusive),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(Enumerable.Repeat(Task.FromResult<string?>("abc123"), PageSize).ToList());
        _hardcoverServiceMock
            .Setup(x => x.GetCoversByIsbn(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<string> isbnList, CancellationToken _) => Enumerable.Repeat(_coverResponse, isbnList.Count).ToList());
        _coverDumpServiceMock
            .Setup(x => x.DumpCovers(It.IsAny<IEnumerable<Cover>>(), _awsResourceOptions.DumpBucketName,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Cover> covers, string _, CancellationToken _) => covers.Count());
        
        _services = new ServiceCollection();
        
        _services.AddSingleton<IHostedService, ListopiaParserRunner>();
        _services.AddSingleton(_lifetimeMock.Object);
        _services.AddSingleton(_listopiaServiceMock.Object);
        _services.AddSingleton(_hardcoverServiceMock.Object);
        _services.AddSingleton(_coverDumpServiceMock.Object);
        _services.AddSingleton(Options.Create(_listopiaOptions));
        _services.AddSingleton(Options.Create(_awsResourceOptions));
        _services.AddSingleton(_loggerMock.Object);
        
        var serviceProvider = _services.BuildServiceProvider();
        _sut = serviceProvider.GetService<IHostedService>();
    }
    
    [Test]
    public async Task TestExecuteAsync()
    {
        Assert.That(_sut, Is.Not.Null);

        await _sut.StartAsync(CancellationToken.None);
        await Task.Delay(500, CancellationToken.None);
        await _sut.StopAsync(CancellationToken.None);
        
        _lifetimeMock.Verify(x => x.StopApplication(),
            Times.Once);
        _listopiaServiceMock.Verify(x => x.GetListopiaIsbns(
            It.IsInRange(
                _listopiaOptions.PageStart,
                _listopiaOptions.PageStart + _listopiaOptions.PageCount - 1,
                Moq.Range.Inclusive
            ),
            It.IsAny<CancellationToken>()
                ),
            Times.Exactly(_listopiaOptions.PageCount));
        _hardcoverServiceMock.Verify(x => x.GetCoversByIsbn(
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()
                ),
            Times.Exactly(_listopiaOptions.PageCount));
        _coverDumpServiceMock.Verify(x => x.DumpCovers(
                It.IsAny<IEnumerable<Cover>>(), _awsResourceOptions.DumpBucketName,
            It.IsAny<CancellationToken>()
                ),
            Times.Exactly(_listopiaOptions.PageCount));
    }
}