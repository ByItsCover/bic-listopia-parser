using Apache.Arrow;
using Apache.Arrow.Types;
using Common.Configs;
using HotCoverParser.Configs;
using HotCoverParser.Interfaces;
using HotCoverParser.Tables;
using lancedb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Orchestrator.Extensions;

public static class LanceDbExtensions 
{
    public static IServiceCollection AddLanceDb(this IServiceCollection services)
    {
        services.AddSingleton<Task<Connection>>(async sp =>
        {
            var awsResourceOptions = sp.GetRequiredService<IOptions<AwsResourceOptions>>();
            if (awsResourceOptions.Value.CoverDbUri == null)
            {
                throw new ArgumentNullException(nameof(awsResourceOptions.Value.CoverDbUri), "Cover DB URI config missing");
            }
            var db = new Connection();
            await db.Connect(awsResourceOptions.Value.CoverDbUri, new ConnectionOptions()
            {
                Region = awsResourceOptions.Value.AwsRegion
            });

            return db;
        });

        return services;
    }
    
    public static IServiceCollection AddHotCoversTable(this IServiceCollection services)
    {
        services.AddSingleton<Task<IHotCoversTable>>(async sp =>
        {
            var dbTask = sp.GetRequiredService<Task<Connection>>();
            var hotCoversSchema = new Schema.Builder()
                .Field(new Field("cover_id", Int64Type.Default, nullable: false))
                .Field(new Field("type", StringType.Default, nullable: false))
                .Field(new Field("users_count", Int64Type.Default, nullable: false))
                .Build();
            var db = await dbTask;
            var hotCoversTable = await db.CreateTable(HotCoverOptions.HotCoversTableName, new CreateTableOptions()
            {
                Schema = hotCoversSchema,
                ExistOk = true
            });

            var coverIdStats = await hotCoversTable.IndexStats("cover_id_idx");
            var typeStats = await hotCoversTable.IndexStats("type_idx");
            if (coverIdStats == null || typeStats == null)
            {
                await hotCoversTable.CreateIndex(["cover_id"], new BTreeIndex(), name: "cover_id_idx");
                await hotCoversTable.CreateIndex(["type"], new BTreeIndex(), name: "type_idx");
            }

            return new HotCoversTable(hotCoversTable, hotCoversSchema);
        });

        return services;
    }
}