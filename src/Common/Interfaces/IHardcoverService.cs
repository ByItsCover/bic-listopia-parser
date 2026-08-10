using Common.Entities;

namespace Common.Interfaces;

public interface IHardcoverService
{
    public Task<IEnumerable<Cover>> GetCoversByIsbn(IEnumerable<string> isbnList, CancellationToken cancellationToken);
    public Task<IEnumerable<Cover>> GetPopularCovers(int count, CancellationToken cancellationToken);
    public Task<IEnumerable<Cover>> GetTrendingCovers(int count, CancellationToken cancellationToken);
}