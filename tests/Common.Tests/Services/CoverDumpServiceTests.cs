using Amazon.S3;
using Amazon.S3.Model;
using Common.Entities;
using Common.Services;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;

namespace Common.Tests.Services;

public class CoverDumpServiceTests
{
    private Mock<IAmazonS3> _s3ClientMock;
    private MockHttpMessageHandler _mockHttp;
    private Mock<ILogger<CoverDumpService>> _loggerMock;
    private Cover _coverRequest;
    private CoverDumpService _sut;

    [SetUp]
    public void Setup()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _mockHttp = new MockHttpMessageHandler();
        _loggerMock = new Mock<ILogger<CoverDumpService>>();
        
        _coverRequest = new Cover
        {
            CoverId = 1,
            BookId = 10,
            Isbn13 = "abc123",
            CoverUrl = "https://www.goodreads.com/my-image"
        };
        
        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());
        
        _sut = new CoverDumpService(_s3ClientMock.Object, new HttpClient(_mockHttp), _loggerMock.Object);
    }

    [Test]
    public async Task TestDumpCovers()
    {
        var coversCount = 20;
        var coverList = Enumerable.Repeat(_coverRequest, coversCount);
        var bucketName = "my_s3_bucket";
        var imageFileRequest = _mockHttp.When(_coverRequest.CoverUrl)
            .Respond("image/jpeg", "surely an image");

        var coversUploaded = await _sut.DumpCovers(coverList, bucketName, CancellationToken.None);
        
        Assert.That(_mockHttp.GetMatchCount(imageFileRequest), Is.EqualTo(coversCount));
        Assert.That(coversUploaded, Is.EqualTo(coversCount));
        
        _s3ClientMock.Verify(x => x.PutObjectAsync(
                It.IsAny<PutObjectRequest>(),
                It.IsAny<CancellationToken>()
            ), 
            Times.Exactly(coversCount));
    }
    
    [TearDown]
    public void TearDown()
    {
        _mockHttp.Dispose();
    }
}