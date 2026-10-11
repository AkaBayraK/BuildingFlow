using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
namespace BuildingFlow.Infrastructure;

// Only bounded operational labels: never URLs, query strings, tenant/person IDs, payloads or exception text.
public static class BuildingFlowTelemetry
{
    public const string Source="BuildingFlow.Operations";
    public static readonly ActivitySource Activities=new(Source);
    private static readonly Meter Meter=new(Source);
    public static readonly Counter<long> Requests=Meter.CreateCounter<long>("buildingflow.http.requests");
    public static readonly Histogram<double> Duration=Meter.CreateHistogram<double>("buildingflow.http.duration","s");
    public static readonly Counter<long> WorkerMessages=Meter.CreateCounter<long>("buildingflow.worker.messages");
    public static readonly Counter<long> WorkerFailures=Meter.CreateCounter<long>("buildingflow.worker.failures");
    public static readonly Counter<long> Deliveries=Meter.CreateCounter<long>("buildingflow.worker.deliveries");
    public static IServiceCollection AddBuildingFlowTelemetry(this IServiceCollection services,ILoggingBuilder logging,IConfiguration configuration,string service)
    {
        if(!configuration.GetValue<bool>("Telemetry:Enabled"))return services;
        var endpoint=configuration["Telemetry:Endpoint"]??"http://localhost:14318";
        if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri)||uri.Scheme is not ("http" or "https")||!string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Query)||uri.AbsolutePath!="/")throw new InvalidOperationException("Use an OTLP HTTP base endpoint without credentials, path or query.");
        var resource=ResourceBuilder.CreateDefault().AddService(service,serviceVersion:"0.10.0");
        services.AddOpenTelemetry().ConfigureResource(r=>r.AddService(service,serviceVersion:"0.10.0"))
            .WithMetrics(m=>m.AddMeter(Source).AddOtlpExporter((o,reader)=>{o.Endpoint=new Uri(uri,"v1/metrics");o.Protocol=OtlpExportProtocol.HttpProtobuf;reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds=5000;}))
            .WithTracing(t=>{t.AddSource(Source).SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(.1)));if(configuration.GetValue<bool>("Telemetry:ExportTraces"))t.AddOtlpExporter(o=>{o.Endpoint=new Uri(uri,"v1/traces");o.Protocol=OtlpExportProtocol.HttpProtobuf;});});
        logging.AddFilter<OpenTelemetryLoggerProvider>((category,level)=>category is not null&&category.StartsWith("BuildingFlow.",StringComparison.Ordinal)&&level>=LogLevel.Information);
        logging.AddOpenTelemetry(o=>{o.SetResourceBuilder(resource);o.IncludeFormattedMessage=true;o.IncludeScopes=false;o.ParseStateValues=true;o.AddOtlpExporter(e=>{e.Endpoint=new Uri(uri,"v1/logs");e.Protocol=OtlpExportProtocol.HttpProtobuf;});});
        return services;
    }
}
