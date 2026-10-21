using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BuildingFlow.Api;

// Sistem durumu: TraceFlow'daki ekranın BuildingFlow karşılığı. Yalnız okuma amaçlı, canlı bileşen kontrolleri üretir.
// Kontrol hatası endpoint'i düşürmez; bileşen DOWN/UNKNOWN döner ve açıklama güvenli sabit metinle sınırlı kalır
// (adres, kullanıcı adı, parola veya exception mesajı yanıta yazılmaz).
public sealed record SystemStatusComponent(string Id, string Name, string Type, string Status, long? ResponseTimeMs, DateTimeOffset CheckedAt, string Description);
public sealed record SystemStatusResponse(string OverallStatus, int TotalComponents, int UpComponents, int DegradedComponents, int DownComponents, int UnknownComponents, DateTimeOffset LastCheckedAt, string Environment, string Version, string Cluster, long UptimeSeconds, IReadOnlyList<SystemStatusComponent> Components);

public sealed class SystemStatusService(IServiceScopeFactory scopes, IConfiguration configuration, IHostEnvironment environment, IHttpClientFactory httpClients, TimeProvider clock)
{
    private static readonly DateTimeOffset Started = DateTimeOffset.UtcNow;
    private readonly SemaphoreSlim gate = new(1, 1);
    private SystemStatusResponse? cached;
    private TimeSpan Timeout => TimeSpan.FromMilliseconds(Math.Clamp(configuration.GetValue("SystemStatus:TimeoutMs", 2000), 100, 10000));

    public async Task<SystemStatusResponse> CheckAsync(CancellationToken token)
    {
        // Alt bilgi her kullanıcıda dakikada bir sorgular; kısa önbellek altyapıya giden yükü sınırlar.
        if (cached is { } fresh && clock.GetUtcNow() - fresh.LastCheckedAt < TimeSpan.FromSeconds(10)) return fresh;
        await gate.WaitAsync(token);
        try
        {
            if (cached is { } again && clock.GetUtcNow() - again.LastCheckedAt < TimeSpan.FromSeconds(10)) return again;
            var components = await Task.WhenAll(
                Task.FromResult(Component("buildingflow-api", "BuildingFlow API", "SERVICE", "UP", 0, "Servis çalışıyor")),
                Http("buildingflow-web", "BuildingFlow Web", "SERVICE", Url("SystemStatus:WebUrl", "/health"), "Web uygulaması yanıt veriyor", token),
                Database(token),
                Redis(token),
                Tcp("rabbitmq", "RabbitMQ", configuration["RabbitMQ:Host"], configuration.GetValue("RabbitMQ:Port", 5672), token),
                Http("keycloak", "Keycloak", "INFRASTRUCTURE", KeycloakDiscovery(), "Kimlik doğrulama servisi", token),
                Http("object-storage", "Object Storage (SeaweedFS)", "INFRASTRUCTURE", configuration["ObjectStorage:Endpoint"], "Nesne deposu yanıt veriyor", token, anyResponse: true),
                Telemetry(token),
                Http("loki", "Loki", "INFRASTRUCTURE", Url("SystemStatus:LokiUrl", "/ready"), "Loki hazır", token),
                Http("prometheus", "Prometheus", "INFRASTRUCTURE", Url("SystemStatus:PrometheusUrl", "/-/ready"), "Prometheus hazır", token),
                Http("grafana", "Grafana", "EXTERNAL_SERVICE", Url("SystemStatus:GrafanaUrl", "/api/health"), "Grafana health endpoint", token),
                Http("mailpit", "E-posta (SMTP)", "EXTERNAL_SERVICE", Url("SystemStatus:MailUrl", "/readyz"), "E-posta servisi yanıt veriyor", token));
            int Count(string status) => components.Count(c => c.Status == status);
            int up = Count("UP"), degraded = Count("DEGRADED"), down = Count("DOWN"), unknown = Count("UNKNOWN");
            var overall = down > 0 ? "DOWN" : degraded > 0 ? "DEGRADED" : unknown > 0 ? "UNKNOWN" : "UP";
            var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "—";
            cached = new SystemStatusResponse(overall, components.Length, up, degraded, down, unknown, clock.GetUtcNow(), environment.EnvironmentName,
                configuration["SystemStatus:Version"] is { Length: > 0 } configured ? configured : version,
                configuration["SystemStatus:Cluster"] is { Length: > 0 } cluster ? cluster : "docker-compose",
                (long)(DateTimeOffset.UtcNow - Started).TotalSeconds, components);
            return cached;
        }
        finally { gate.Release(); }
    }

    private SystemStatusComponent Component(string id, string name, string type, string status, long? elapsed, string description) =>
        new(id, name, type, status, elapsed, clock.GetUtcNow(), description);

    private async Task<SystemStatusComponent> Timed(string id, string name, string type, Func<CancellationToken, Task<(string Status, string Description)>> probe, CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(Timeout);
        (string Status, string Description) result;
        try { result = await probe(timeout.Token); }
        catch (Exception) when (!token.IsCancellationRequested) { result = ("DOWN", "Bağlantı kurulamadı"); }
        return Component(id, name, type, result.Status, watch.ElapsedMilliseconds, result.Description);
    }

    private Task<SystemStatusComponent> Database(CancellationToken token) => Timed("postgresql", "PostgreSQL", "DATABASE", async t =>
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
        return await db.Database.CanConnectAsync(t) ? ("UP", "Veritabanı bağlantısı başarılı") : ("DOWN", "Veritabanı bağlantısı kurulamadı");
    }, token);

    private Task<SystemStatusComponent> Redis(CancellationToken token)
    {
        var value = configuration["Redis:ConnectionString"]?.Split(',')[0].Trim();
        if (string.IsNullOrEmpty(value)) return Task.FromResult(Component("redis", "Redis", "INFRASTRUCTURE", "UNKNOWN", null, "Bağlantı ayarı bulunamadı"));
        var separator = value.LastIndexOf(':');
        var host = separator > 0 ? value[..separator] : value;
        var port = separator > 0 && int.TryParse(value[(separator + 1)..], out var parsed) ? parsed : 6379;
        return Timed("redis", "Redis", "INFRASTRUCTURE", async t =>
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, t);
            var stream = client.GetStream();
            await stream.WriteAsync("*1\r\n$4\r\nPING\r\n"u8.ToArray(), t);
            var buffer = new byte[5];
            var read = await stream.ReadAtLeastAsync(buffer, 5, throwOnEndOfStream: false, t);
            return Encoding.ASCII.GetString(buffer, 0, read) == "+PONG" ? ("UP", "PONG") : ("DEGRADED", "Redis yanıtı beklenen biçimde değil");
        }, token);
    }

    private Task<SystemStatusComponent> Tcp(string id, string name, string? host, int port, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(host)) return Task.FromResult(Component(id, name, "INFRASTRUCTURE", "UNKNOWN", null, "Bağlantı ayarı bulunamadı"));
        return Timed(id, name, "INFRASTRUCTURE", async t =>
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, t);
            return ("UP", "TCP bağlantısı başarılı");
        }, token);
    }

    private Task<SystemStatusComponent> Telemetry(CancellationToken token)
    {
        if (!configuration.GetValue("Telemetry:Enabled", false))
            return Task.FromResult(Component("otel-collector", "OpenTelemetry Collector", "INFRASTRUCTURE", "DISABLED", null, "Telemetri kapalı"));
        return Http("otel-collector", "OpenTelemetry Collector", "INFRASTRUCTURE", configuration["Telemetry:Endpoint"], "Telemetri toplayıcı yanıt veriyor", token, anyResponse: true);
    }

    private Task<SystemStatusComponent> Http(string id, string name, string type, string? url, string healthy, CancellationToken token, bool anyResponse = false)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return Task.FromResult(Component(id, name, type, "UNKNOWN", null, "Endpoint ayarı bulunamadı"));
        return Timed(id, name, type, async t =>
        {
            using var response = await httpClients.CreateClient("system-status").GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, t);
            var code = (int)response.StatusCode;
            // S3 ve OTLP kökü kimliksiz isteğe 403/404 döner; yanıt gelmesi erişilebilirlik için yeterlidir.
            if (code is >= 200 and < 300 || anyResponse && code < 500) return ("UP", healthy);
            if (code == 429 || code >= 500) return ("DEGRADED", "Endpoint yanıtı gecikmeli veya geçici olarak kullanılamıyor");
            return ("DOWN", "Endpoint beklenmeyen HTTP yanıtı döndürdü");
        }, token);
    }

    private string? Url(string key, string path) => configuration[key] is { Length: > 0 } value ? value.TrimEnd('/') + path : null;

    private string? KeycloakDiscovery() =>
        configuration["Authentication:MetadataAddress"] is { Length: > 0 } metadata ? metadata
        : configuration["Authentication:Authority"] is { Length: > 0 } authority ? authority.TrimEnd('/') + "/.well-known/openid-configuration" : null;
}

internal static class SystemStatusEndpoints
{
    public static IServiceCollection AddSystemStatus(this IServiceCollection services)
    {
        services.AddHttpClient("system-status", client => client.Timeout = TimeSpan.FromSeconds(10)).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, ConnectTimeout = TimeSpan.FromSeconds(3) });
        return services.AddSingleton<SystemStatusService>();
    }

    // Yönetim > Sistem Ayarları ekranının ve alt bilgi durum göstergesinin kaynağı; yönetim rolleriyle sınırlı.
    public static void MapSystemStatusEndpoints(this WebApplication app) =>
        app.MapGet("/api/v1/system/status", async (SystemStatusService service, CancellationToken token) => Results.Ok(await service.CheckAsync(token)))
            .RequireAuthorization("CanManageHierarchy").WithTags("System");
}
