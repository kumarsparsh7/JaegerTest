using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var Configuration = builder.Configuration;
builder.Services.Configure<OtlpExporterOptions>(options =>
{
    var endpointUri = Environment.GetEnvironmentVariable("COLLECTOR_ENDPOINT");
    if (!string.IsNullOrEmpty(endpointUri))
    {
        options.Endpoint = new Uri(endpointUri);
    }
    else
    {
        options.Endpoint = new Uri("http://host.docker.internal:4317");
    }

    var protocol = Environment.GetEnvironmentVariable("COLLECTOR_PROTOCOL");
    if (!string.IsNullOrEmpty(protocol))
    {
        if(protocol.Equals("grpc", StringComparison.OrdinalIgnoreCase)) { options.Protocol = OtlpExportProtocol.Grpc; } else if (protocol.Equals("http", StringComparison.OrdinalIgnoreCase)) { options.Protocol = OtlpExportProtocol.HttpProtobuf; }
    }
    else
    {
        options.Protocol = OtlpExportProtocol.Grpc;
    }

    bool enableTls = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_TLS") ?? "true");

    if (!enableTls)
    {
        // TLS is disabled, configure insecure connection
        options.HttpClientFactory = () =>
        {
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            };
            return new HttpClient(httpClientHandler);
        };
    }
});

builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("JaegerTest"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
