using Common.Entities;

namespace Common.Interfaces;

public interface ICoverDumpService
{
    Task<int> DumpCovers(IEnumerable<Cover> covers, string bucketName, CancellationToken cancellationToken);
}