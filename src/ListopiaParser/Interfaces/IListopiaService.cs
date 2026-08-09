namespace ListopiaParser.Interfaces;

public interface IListopiaService
{
    public Task<List<Task<string?>>> GetListopiaIsbns(int pageNumber, CancellationToken cancellationToken);
}