using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ListopiaParser.Tests;

public class ListopiaParserRunnerTests
{
    private Mock<IHostApplicationLifetime> _lifetimeMock;
    private Mock<IListopiaService> _listopiaServiceMock;
    private Mock<IHardcoverService> _hardcoverServiceMock;
    private Mock<IEmbedService> _embedServiceMock;
    private PostgreSqlContainer _pgVectorContainer;
    private IOptions<ListopiaOptions> _listopiaOptions;
    private IOptions<PgVectorOptions> _pgVectorOptions;
    private ListopiaOptions _listopiaOptionValues;
    private PgVectorOptions _pgVectorOptionValues;
    private Mock<ILogger<ListopiaParserRunner>> _loggerMock;
    private IServiceCollection _services;
    private IHostedService? _sut;
    
    [SetUp]
    public async Task Setup()
    {
        _lifetimeMock = new Mock<IHostApplicationLifetime>();
        _listopiaServiceMock = new Mock<IListopiaService>();
        _hardcoverServiceMock = new Mock<IHardcoverService>();
        _embedServiceMock = new Mock<IEmbedService>();
        _loggerMock = new Mock<ILogger<ListopiaParserRunner>>();
        _listopiaOptionValues = new ListopiaOptions
        {
            GoodreadsBase = "https://www.goodreads.com",
            ListopiaUrl = "https://www.goodreads.com/list/show/001.TestList",
            Pages = 10
        };
        _pgVectorOptionValues = new PgVectorOptions
        {
            VectorDimensions = 512,
            CollectionName = "covers_scraped"
        };
        _listopiaOptions = Options.Create(_listopiaOptionValues);
        _pgVectorOptions = Options.Create(_pgVectorOptionValues);

        _pgVectorContainer = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .Build();
        await _pgVectorContainer.StartAsync();
        
        _services = new ServiceCollection();
        
        _services.AddSingleton<NpgsqlDataSource>(_ =>
        {
            NpgsqlDataSourceBuilder dataSourceBuilder = new(_pgVectorContainer.GetConnectionString());
            dataSourceBuilder.UseVector();
            var datasource = dataSourceBuilder.Build();
        
            var conn = datasource.OpenConnection();
            using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", conn);
            cmd.ExecuteNonQuery();
            return datasource;
        });
        _services.AddPostgresVectorStore();
        
        _services.AddSingleton<IHostedService, ListopiaParserRunner>();
        _services.AddSingleton(_lifetimeMock.Object);
        _services.AddSingleton(_listopiaServiceMock.Object);
        _services.AddSingleton(_hardcoverServiceMock.Object);
        _services.AddSingleton(_embedServiceMock.Object);
        _services.AddSingleton(_listopiaOptions);
        _services.AddSingleton(_pgVectorOptions);
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
            It.IsInRange(1, _listopiaOptionValues.Pages, Moq.Range.Inclusive),
            It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.Pages));
        _hardcoverServiceMock.Verify(x => x.GetBookEditions(
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.Pages));
        _embedServiceMock.Verify(x => x.GetCoverEmbeddings(
                It.IsAny<List<Edition>>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(_listopiaOptionValues.Pages));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _pgVectorContainer.DisposeAsync();
    }
}