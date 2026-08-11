using Apache.Arrow;
using Common.Entities;
using HotCoverParser.Tables;
using lancedb;

namespace HotCoverParser.Interfaces;

public interface IHotCoversTable
{
    Task<MergeResult> InsertCovers(Task<IEnumerable<Cover>> coverTasks, HotEnum type);
    static virtual (RecordBatch, string) MapCoversToRecords(IEnumerable<Cover> covers, HotEnum type, Schema schema)
    {
        throw new NotImplementedException();
    }
}