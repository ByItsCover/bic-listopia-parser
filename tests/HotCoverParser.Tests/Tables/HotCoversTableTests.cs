using Apache.Arrow;
using Apache.Arrow.Types;
using AwesomeAssertions;
using Common.Entities;
using HotCoverParser.Tables;

namespace HotCoverParser.Tests.Tables;

public class HotCoversTableTests
{
    //private Mock<Table> _tableMock;
    private Schema _hotCoversSchema;
    //private HotCoversTable _sut;

    [SetUp]
    public void Setup()
    {
        _hotCoversSchema = new Schema.Builder()
            .Field(new Field("cover_id", Int64Type.Default, nullable: false))
            .Field(new Field("type", StringType.Default, nullable: false))
            .Field(new Field("users_count", Int64Type.Default, nullable: false))
            .Build();
    }

    [TestCase(HotEnum.Popular, "Popular")]
    [TestCase(HotEnum.Trending, "Trending")]
    public async Task TestMapCoversToRecords(HotEnum type, string typeString)
    {
        var covers = new List<Cover>
        {
            new()
            {
                CoverId = 1589497,
                BookId = 88639,
                Isbn13 = "9780439023481",
                CoverUrl = "https://assets.hardcover.app/editions/1589497/2979196565308831-lf%202.jpeg",
                UsersCount = 10896
            },
            new()
            {
                CoverId = 3274049,
                BookId = 427578,
                Isbn13 = "9780593135204",
                CoverUrl = "https://assets.hardcover.app/editions/3274049/8741341047797682-91mYu67RfUL._SL1500_.jpg",
                UsersCount = 16996
            }
        };
        var expectedCoverRecords = new RecordBatch(
            _hotCoversSchema,
            new IArrowArray[]
            {
                new Int64Array.Builder()
                    .AppendRange([1589497, 3274049])
                    .Build(),
                new StringArray.Builder()
                    .AppendRange([typeString, typeString])
                    .Build(),
                new Int64Array.Builder()
                    .AppendRange([10896, 16996])
                    .Build()
            },
            2
        );

        var (coverRecords, typeValue) = HotCoversTable.MapCoversToRecords(covers, type, _hotCoversSchema);

        var temp = coverRecords.Column(1);
        Assert.That(typeValue, Is.EqualTo(typeString));
        Assert.That(coverRecords.Length, Is.EqualTo(2));
        Assert.That(coverRecords.ColumnCount, Is.EqualTo(3));
        coverRecords.Schema.Should().BeEquivalentTo(expectedCoverRecords.Schema);
        ((Int64Array)coverRecords.Column(0)).Should().BeEquivalentTo((Int64Array)expectedCoverRecords.Column(0));
        ((StringArray)coverRecords.Column(1)).Should().BeEquivalentTo((StringArray)expectedCoverRecords.Column(1));
        ((Int64Array)coverRecords.Column(2)).Should().BeEquivalentTo((Int64Array)expectedCoverRecords.Column(2));
    }
}