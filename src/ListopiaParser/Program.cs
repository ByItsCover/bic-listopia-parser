using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Amazon.SQS;
using AwsSignatureVersion4;
using ListopiaParser;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Polly;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
var connString = Environment.GetEnvironmentVariable("PGVECTOR_CONN") ?? string.Empty;
var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? string.Empty;
var options = new HostApplicationBuilderSettings 
{
    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
};

var builder = Host.CreateApplicationBuilder(options);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{env}.json", true, true)
    .AddUserSecrets<Program>(true, true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
{
    NpgsqlDataSourceBuilder dataSourceBuilder = new(connString);
    dataSourceBuilder.UseVector();
    var datasource = dataSourceBuilder.Build();

    var retryPolicy = Policy
        .Handle<NpgsqlException>(ex => ex.IsTransient) 
        .Or<TimeoutException>()
        .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    retryPolicy.Execute(() =>
    {
        var conn = datasource.OpenConnection();
        using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", conn);
        cmd.ExecuteNonQuery();
    });
    
    return datasource;
});
builder.Services.AddPostgresVectorStore();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<AWSCredentials>(_ => DefaultAWSCredentialsIdentityResolver.GetCredentials());
    builder.Services.AddTransient<AwsSignatureHandler>()
        .AddTransient(sp =>
        {
            var credProvider = sp.GetRequiredService<AWSCredentials>();
            var immutableCreds = credProvider.GetCredentials();
            return new AwsSignatureHandlerSettings(
                awsRegion,
                "execute-api",
                immutableCreds);
        });
}

builder.Services.Configure<ListopiaOptions>(builder.Configuration.GetSection("ListopiaOptions"));
builder.Services.Configure<HardcoverOptions>(builder.Configuration.GetSection("HardcoverOptions"));
builder.Services.Configure<EmbedOptions>(builder.Configuration.GetSection("EmbedOptions"));
builder.Services.Configure<PgVectorOptions>(builder.Configuration.GetSection("PgVectorOptions"));
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddHttpClient<IListopiaService, ListopiaService>();
builder.Services.AddHttpClient<IHardcoverService, HardcoverService>();
var embedClientBuilder = builder.Services.AddHttpClient<IEmbedService, EmbedService>();
if (!builder.Environment.IsDevelopment())
    embedClientBuilder.AddHttpMessageHandler<AwsSignatureHandler>();
builder.Services.AddHostedService<ListopiaParserRunner>();

var host = builder.Build();
await host.RunAsync();