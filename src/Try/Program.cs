using System.Text.Json.Serialization;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tiger.Stripes;
using static System.Text.Encodings.Web.JavaScriptEncoder;
using static System.Text.Json.JsonSerializerDefaults;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

#pragma warning disable CA1848
Amazon.AWSConfigs.LoggingConfig.LogResponses = Amazon.ResponseLoggingOption.Always;
Amazon.AWSConfigs.LoggingConfig.LogMetrics = true;
Amazon.AWSConfigs.LoggingConfig.LogTo = Amazon.LoggingOptions.SystemDiagnostics;
using var listener = new System.Diagnostics.ConsoleTraceListener();
Amazon.AWSConfigs.AddTraceListener("Amazon", listener);
var builder = LambdaApplication.CreateSlimBuilder();
_ = builder.Logging.AddJsonConsole(o =>
{
  o.IncludeScopes = true;
  o.JsonWriterOptions = o.JsonWriterOptions with
  {
    Encoder = UnsafeRelaxedJsonEscaping,
  };
});
_ = builder.Services.AddSingleton<IAmazonS3, AmazonS3Client>();

var app = builder.Build();
_ = app.MapInvoke(
  "Try",
  static async (int limit, IAmazonS3 sss, ILogger logger, CancellationToken ct) =>
  {
    var paginator = sss.Paginators.ListObjectsV2(new()
    {
      BucketName = "SORRY YOU HAVE TO PROVIDE YOUR OWN BUCKET",
    });
    await foreach(var s3Object in paginator.S3Objects.Take(limit).WithCancellation(ct).ConfigureAwait(false))
    {
      logger.LogInformation("Found object '{Key}' ('{Size} bytes').", s3Object.Key, s3Object.Size);
    }
  },
  Context.Default.Int32);
await app.RunAsync().ConfigureAwait(false);

[JsonSerializable(typeof(int))]
[JsonSourceGenerationOptions(Web, DefaultIgnoreCondition = WhenWritingDefault)]
sealed partial class Context
  : JsonSerializerContext
{
}
