using Apache.Arrow;
using Common.Entities;
using HotCoverParser.Interfaces;
using lancedb;
using Table = lancedb.Table;

namespace HotCoverParser.Tables;

public enum HotEnum
{
    Popular,
    Trending
}

public class HotCoversTable : IHotCoversTable
{
    private readonly Table _table;
    private readonly Schema _schema;

    public HotCoversTable(Table table, Schema schema)
    {
        _table = table;
        _schema = schema;
    }

    public async Task<MergeResult> InsertCovers(Task<IEnumerable<Cover>> coverTasks, HotEnum type)
    {
        var covers = await coverTasks;
        var (coverRecords, typeValue) = MapCoversToRecords(covers, type, _schema);
        Console.WriteLine("Logging res");
        var res = await _table.Head(51);
        Console.WriteLine(res);
        var insertQuery = _table.MergeInsert(["cover_id", "type"])
            .WhenMatchedUpdateAll()
            .WhenNotMatchedInsertAll()
            .WhenNotMatchedBySourceDelete($"type = '${typeValue}'");

        return await insertQuery.Execute(coverRecords);
    }
    
    public static (RecordBatch, string) MapCoversToRecords(IEnumerable<Cover> covers, HotEnum type, Schema schema)
    {
        var typeValue = Enum.GetName(type);
        if (typeValue == null)
        {
            throw new ArgumentNullException(nameof(typeValue), "Type value could not be retrieved");
        }
        var cidBuilder = new Int64Array.Builder();
        var typeBuilder = new StringArray.Builder();
        var uCountBuilder = new Int64Array.Builder();

        var coverCount = 0;
        foreach (var cover in covers)
        {
            cidBuilder.Append(cover.CoverId);
            typeBuilder.Append(typeValue);
            uCountBuilder.Append(cover.UsersCount);
            coverCount += 1;
        }

        return (
            new RecordBatch(
                schema,
                [cidBuilder.Build(), typeBuilder.Build(), uCountBuilder.Build()],
                coverCount
            ), 
            typeValue
        );
    }
}
