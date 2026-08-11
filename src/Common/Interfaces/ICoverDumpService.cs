using Common.Entities;

namespace Common.Interfaces;

public interface ICoverDumpService
{
    Task<int> DumpCovers(Task<IEnumerable<Cover>> coverTasks, string bucketName, CancellationToken cancellationToken);
}