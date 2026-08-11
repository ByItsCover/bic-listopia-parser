using Common.Entities;

namespace Common.Interfaces;

public interface IHardcoverService
{
    Task<IEnumerable<Cover>> GetCoversByIsbn(IEnumerable<string> isbnList, CancellationToken cancellationToken);
    Task<IEnumerable<Cover>> GetPopularCovers(int count, CancellationToken cancellationToken);
    Task<IEnumerable<Cover>> GetTrendingCovers(int count, CancellationToken cancellationToken);
}