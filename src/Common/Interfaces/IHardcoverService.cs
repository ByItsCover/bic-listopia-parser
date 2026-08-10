using Common.Entities;

namespace Common.Interfaces;

public interface IHardcoverService
{
    public Task<IEnumerable<Cover>> GetCoversByIsbn(IEnumerable<string> isbnList, CancellationToken cancellationToken);
}