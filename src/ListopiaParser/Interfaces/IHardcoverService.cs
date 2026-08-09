using ListopiaParser.Entities;

namespace ListopiaParser.Interfaces;

public interface IHardcoverService
{
    public Task<IEnumerable<Cover>> GetBookCovers(IEnumerable<string> isbnList, CancellationToken cancellationToken);
}