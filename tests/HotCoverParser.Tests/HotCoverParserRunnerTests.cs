using Common.Configs;
using HotCoverParser.Configs;
using HotCoverParser.Interfaces;
using Common.Entities;
using Common.Interfaces;
using HotCoverParser.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HotCoverParser.Tests;

public class HotCoverParserRunnerTests
{
    private Mock<IHostApplicationLifetime> _lifetimeMock;
    private Mock<IHardcoverService> _hardcoverServiceMock;
    private Mock<ICoverDumpService> _coverDumpServiceMock;
    private Mock<IHotCoversTable> _hotCoversTableMock;
    private HotCoverOptions _hotCoverOptions;
    private AwsResourceOptions _awsResourceOptions;
    private Mock<ILogger<HotCoverParserRunner>> _loggerMock;
    private IServiceCollection _services;
    private Cover _coverResponse;
    private IHostedService? _sut;
    
    [SetUp]
    public void Setup()
    {
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _hardcoverServiceMock = new Mock<IHardcoverService>();
        _coverDumpServiceMock = new Mock<ICoverDumpService>();
        _hotCoversTableMock = new Mock<IHotCoversTable>();
        _loggerMock = new Mock<ILogger<HotCoverParserRunner>>();
        
        _hotCoverOptions = new HotCoverOptions
        {
            PopularCount = 21,
            TrendingCount = 19
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
        
        _hardcoverServiceMock
            .Setup(x => x.GetPopularCovers(_hotCoverOptions.PopularCount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Repeat(_coverResponse, _hotCoverOptions.PopularCount).ToList());
        _hardcoverServiceMock
            .Setup(x => x.GetTrendingCovers(_hotCoverOptions.TrendingCount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Repeat(_coverResponse, _hotCoverOptions.TrendingCount).ToList());
        _coverDumpServiceMock
            .Setup(x => x.DumpCovers(It.IsAny<Task<IEnumerable<Cover>>>(), _awsResourceOptions.DumpBucketName,
                It.IsAny<CancellationToken>()))
            .Returns(async (Task<IEnumerable<Cover>> coverTasks, string _, CancellationToken _) =>
            {
                var covers = (await coverTasks).ToList();
                return covers.Count;
            });
        _hotCoversTableMock
            .Setup(x => x.InsertCovers(
                It.IsAny<Task<IEnumerable<Cover>>>(),
                It.IsAny<HotEnum>()
            ));
        
        _services = new ServiceCollection();
        
        _services.AddSingleton<IHostedService, HotCoverParserRunner>();
        _services.AddSingleton(_lifetimeMock.Object);
        _services.AddSingleton(_hardcoverServiceMock.Object);
        _services.AddSingleton(_coverDumpServiceMock.Object);
        _services.AddSingleton(Task.FromResult(_hotCoversTableMock.Object));
        _services.AddSingleton(Options.Create(_hotCoverOptions));
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
        _hardcoverServiceMock.Verify(x => x.GetPopularCovers(
                _hotCoverOptions.PopularCount,
                It.IsAny<CancellationToken>()
                ),
            Times.Once);
        _hardcoverServiceMock.Verify(x => x.GetTrendingCovers(
                _hotCoverOptions.TrendingCount,
                It.IsAny<CancellationToken>()
            ),
            Times.Once);
        _coverDumpServiceMock.Verify(x => x.DumpCovers(
                It.IsAny<Task<IEnumerable<Cover>>>(), _awsResourceOptions.DumpBucketName,
            It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2));
        _hotCoversTableMock
            .Verify(x => x.InsertCovers(
                It.IsAny<Task<IEnumerable<Cover>>>(),
                HotEnum.Popular
            ), Times.Once);
        _hotCoversTableMock
            .Verify(x => x.InsertCovers(
                It.IsAny<Task<IEnumerable<Cover>>>(),
                HotEnum.Trending
            ), Times.Once);
    }
}