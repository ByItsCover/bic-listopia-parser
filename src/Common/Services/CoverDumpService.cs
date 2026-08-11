using Amazon.S3;
using Amazon.S3.Model;
using Common.Entities;
using Common.Interfaces;
using Common.Utils;
using Microsoft.Extensions.Logging;

namespace Common.Services;

public class CoverDumpService : ICoverDumpService
{
    private readonly IAmazonS3 _s3Client;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoverDumpService> _logger;
    
    public CoverDumpService(IAmazonS3 s3Client, HttpClient httpClient, ILogger<CoverDumpService> logger)
    {
        _s3Client = s3Client;
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<int> DumpCovers(IEnumerable<Cover> covers, string bucketName, CancellationToken cancellationToken)
    {
        var s3Tasks = covers.Select(async c =>
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = $"{c.CoverId}-{c.Isbn13}.bin",
                InputStream = await RequestUtils.FetchStream(c.CoverUrl, _httpClient, cancellationToken)
            };
            request.Metadata.Add("cover_id", c.CoverId.ToString());
            request.Metadata.Add("book_id", c.BookId.ToString());
            request.Metadata.Add("isbn_13", c.Isbn13);
            request.Metadata.Add("image_url", c.CoverUrl);

            try
            {
                await _s3Client.PutObjectAsync(request, cancellationToken);
                return true;
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "Error: {Message}", e.Message);
                return false;
            }
        });
                
        var responseSuccesses = await Task.WhenAll(s3Tasks);
        return responseSuccesses.Count(r => r);
    }
}