namespace ListopiaParser.Interfaces;

public interface IListopiaService
{
    Task<List<Task<string?>>> GetListopiaIsbns(int pageNumber, CancellationToken cancellationToken);
}