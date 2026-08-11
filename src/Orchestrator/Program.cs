using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Amazon.S3;
using Common.Configs;
using Common.Interfaces;
using Common.Services;
using HotCoverParser;
using HotCoverParser.Configs;
using ListopiaParser;
using ListopiaParser.Configs;
using ListopiaParser.Interfaces;
using ListopiaParser.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orchestrator.Extensions;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var jobType = Environment.GetEnvironmentVariable("JOB_TYPE");

var options = new HostApplicationBuilderSettings 
{
    EnvironmentName = env
};
var builder = Host.CreateApplicationBuilder(options);

builder.Configuration.AddAppSettings(env);
builder.Services.Configure<AwsResourceOptions>(builder.Configuration.GetSection("AwsResourceOptions"));
builder.Services.Configure<HardcoverOptions>(builder.Configuration.GetSection("HardcoverOptions"));
builder.Services.AddSingleton<AWSCredentials>(_ => DefaultAWSCredentialsIdentityResolver.GetCredentials());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddHttpClient<IHardcoverService, HardcoverService>();
builder.Services.AddHttpClient<ICoverDumpService, CoverDumpService>();


if (jobType == "hot_cover_parser")
{
    builder.Services.Configure<HotCoverOptions>(builder.Configuration.GetSection("HotCoverOptions"));
    builder.Services.AddLanceDb();
    builder.Services.AddHotCoversTable();
    builder.Services.AddHostedService<HotCoverParserRunner>();
}
else if (jobType == "listopia_parser")
{
    builder.Services.Configure<ListopiaOptions>(builder.Configuration.GetSection("ListopiaOptions"));
    builder.Services.AddHttpClient<IListopiaService, ListopiaService>();
    builder.Services.AddHostedService<ListopiaParserRunner>();
}

var host = builder.Build();
await host.RunAsync();