using System.Diagnostics;
using System.Runtime.CompilerServices;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BuildingFlow.Api;

public sealed record QaStepResult(string Key, QaStepStatus Status, string Expected, string Actual, long DurationMs);
public sealed record QaScenarioResponse(string Key, int TimeoutSeconds, string Access);
public sealed record QaRunResponse(Guid Id, string Code, string ScenarioKey, string Status, int Attempt, Guid? RetryOfId, bool CancelRequested,
    DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? ResultCode, int PassedSteps, int FailedSteps, int TimeoutSeconds, long Version);
public sealed record QaStepResponse(int Sequence, string StepKey, string Status, string Expected, string Actual, long DurationMs);
public sealed record QaRunDetail(QaRunResponse Run, IReadOnlyList<QaStepResponse> Steps);
public sealed record QaRunPage(IReadOnlyList<QaRunResponse> Items, int Total, int Page, int PageSize);
public sealed record QaOverview(string Environment, int ScenarioCount, int EndpointCount, int ActiveRuns, IReadOnlyDictionary<string, int> RunCounts, QaRunResponse? LastRun);
public sealed record StartQaRunRequest(string? ScenarioKey, Guid RequestId);
public sealed record RetryQaRunRequest(Guid RequestId, long Version);
public sealed record CancelQaRunRequest(long Version);

// Scenarios are code-owned and read-only: the run queue never executes user-supplied commands, SQL or scripts.
public interface IQaScenario
{
    string Key { get; }
    int TimeoutSeconds { get; }
    IAsyncEnumerable<QaStepResult> RunAsync(IServiceProvider services, CancellationToken token);
}

public sealed class QaCenterOptions
{
    public bool Enabled { get; init; }
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);
    public static QaCenterOptions From(IConfiguration configuration) => new()
    {
        Enabled = configuration.GetValue("QaCenter:Enabled", false),
        PollInterval = TimeSpan.FromMilliseconds(Math.Clamp(configuration.GetValue("QaCenter:PollMilliseconds", 2000), 100, 60000))
    };
}

internal static class QaStep
{
    public static async Task<QaStepResult> Measure(string key, string expected, Func<Task<(bool Passed, string Actual)>> check)
    {
        var watch = Stopwatch.StartNew();
        var (passed, actual) = await check();
        return new QaStepResult(key, passed ? QaStepStatus.PASSED : QaStepStatus.FAILED, expected, actual, watch.ElapsedMilliseconds);
    }
}

internal sealed class SystemHealthScenario : IQaScenario
{
    public string Key => "system.health";
    public int TimeoutSeconds => 60;
    public async IAsyncEnumerable<QaStepResult> RunAsync(IServiceProvider services, [EnumeratorCancellation] CancellationToken token)
    {
        var watch = Stopwatch.StartNew();
        var status = await services.GetRequiredService<SystemStatusService>().CheckAsync(token);
        foreach (var component in status.Components)
            yield return new QaStepResult("component." + component.Id, component.Status == "UP" ? QaStepStatus.PASSED : QaStepStatus.FAILED, "UP", component.Status, component.ResponseTimeMs ?? watch.ElapsedMilliseconds);
    }
}

internal sealed class ApiAuthorizationScenario : IQaScenario
{
    public string Key => "api.authorization";
    public int TimeoutSeconds => 30;
    public async IAsyncEnumerable<QaStepResult> RunAsync(IServiceProvider services, [EnumeratorCancellation] CancellationToken token)
    {
        var endpoints = services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true).ToList();
        yield return await QaStep.Measure("endpoints.present", "> 0", () => Task.FromResult((endpoints.Count > 0, endpoints.Count.ToString())));
        var undeclared = endpoints.Count(endpoint => !endpoint.RoutePattern.RawText!.StartsWith("/api/public/", StringComparison.Ordinal)
            && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null && endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0);
        yield return await QaStep.Measure("endpoints.authorization", "0", () => Task.FromResult((undeclared == 0, undeclared.ToString())));
    }
}

internal sealed class DatabaseMigrationScenario : IQaScenario
{
    public string Key => "db.migrations";
    public int TimeoutSeconds => 60;
    public async IAsyncEnumerable<QaStepResult> RunAsync(IServiceProvider services, [EnumeratorCancellation] CancellationToken token)
    {
        var database = services.GetRequiredService<BuildingFlowDbContext>().Database;
        yield return await QaStep.Measure("database.connect", "true", async () => await database.CanConnectAsync(token) is var connected ? (connected, connected ? "true" : "false") : default);
        yield return await QaStep.Measure("database.pendingMigrations", "0", async () => (await database.GetPendingMigrationsAsync(token)).Count() is var pending ? (pending == 0, pending.ToString()) : default);
    }
}

public sealed class QaCenterService(BuildingFlowDbContext db, IEnumerable<IQaScenario> scenarios, EndpointDataSource endpoints, IHostEnvironment environment, TimeProvider clock)
{
    public IReadOnlyList<QaScenarioResponse> Scenarios() =>
        scenarios.OrderBy(scenario => scenario.Key, StringComparer.Ordinal).Select(scenario => new QaScenarioResponse(scenario.Key, scenario.TimeoutSeconds, "READ_ONLY")).ToList();

    public async Task<QaOverview> Overview(CancellationToken token)
    {
        var counts = await db.QaRuns.AsNoTracking().GroupBy(run => run.Status).Select(group => new { group.Key, Count = group.Count() }).ToListAsync(token);
        var last = await db.QaRuns.AsNoTracking().OrderByDescending(run => run.QueuedAt).FirstOrDefaultAsync(token);
        var byStatus = Enum.GetValues<QaRunStatus>().ToDictionary(status => status.ToString(), status => counts.FirstOrDefault(count => count.Key == status)?.Count ?? 0);
        var apiCount = endpoints.Endpoints.OfType<RouteEndpoint>().Count(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/", StringComparison.Ordinal) == true);
        return new QaOverview(environment.EnvironmentName, scenarios.Count(), apiCount, byStatus["QUEUED"] + byStatus["RUNNING"], byStatus, last is null ? null : Map(last));
    }

    public async Task<QaRunPage> Runs(int page, int pageSize, string? status, CancellationToken token)
    {
        if (page < 1 || pageSize is < 1 or > 100) throw new ArgumentException("Invalid paging.");
        var query = db.QaRuns.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<QaRunStatus>(status, false, out var filter) || !Enum.IsDefined(filter)) throw new ArgumentException("Invalid status.");
            query = query.Where(run => run.Status == filter);
        }
        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(run => run.QueuedAt).ThenBy(run => run.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new QaRunPage(items.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<QaRunDetail?> Get(Guid id, CancellationToken token)
    {
        var run = await db.QaRuns.AsNoTracking().FirstOrDefaultAsync(value => value.Id == id, token);
        if (run is null) return null;
        var steps = await db.QaRunSteps.AsNoTracking().Where(step => step.RunId == id).OrderBy(step => step.Sequence)
            .Select(step => new QaStepResponse(step.Sequence, step.StepKey, step.Status.ToString(), step.Expected, step.Actual, step.DurationMs)).ToListAsync(token);
        return new QaRunDetail(Map(run), steps);
    }

    public async Task<(QaRunResponse Run, bool Created)?> Start(StartQaRunRequest request, CancellationToken token)
    {
        if (request.RequestId == Guid.Empty) throw new ArgumentException("RequestId is required.");
        var scenario = scenarios.FirstOrDefault(value => value.Key == request.ScenarioKey);
        if (scenario is null) return null;
        return await Enqueue(request.RequestId, scenario, null, token);
    }

    public async Task<(QaRunResponse Run, bool Created)?> Retry(Guid id, RetryQaRunRequest request, CancellationToken token)
    {
        if (request.RequestId == Guid.Empty) throw new ArgumentException("RequestId is required.");
        var source = await db.QaRuns.FirstOrDefaultAsync(run => run.Id == id, token);
        if (source is null) return null;
        if (await Replay(request.RequestId, source.ScenarioKey, token) is { } replay) return (replay, false);
        if (source.Version != request.Version || !source.CanRetry) throw new HierarchyConflictException(ErrorCatalog.QaRunState.Create());
        if (source.Attempt >= QaRun.MaxAttempts) throw new HierarchyConflictException(ErrorCatalog.QaRetryLimit.Create());
        var scenario = scenarios.FirstOrDefault(value => value.Key == source.ScenarioKey) ?? throw new HierarchyConflictException(ErrorCatalog.QaRunState.Create());
        return await Enqueue(request.RequestId, scenario, source, token);
    }

    public async Task<QaRunResponse?> Cancel(Guid id, CancelQaRunRequest request, CancellationToken token)
    {
        var run = await db.QaRuns.FirstOrDefaultAsync(value => value.Id == id, token);
        if (run is null) return null;
        if (run.Version != request.Version || !run.IsActive) throw new HierarchyConflictException(ErrorCatalog.QaRunState.Create());
        run.RequestCancel(clock.GetUtcNow());
        await db.SaveChangesAsync(token);
        return Map(run);
    }

    private async Task<(QaRunResponse Run, bool Created)> Enqueue(Guid requestId, IQaScenario scenario, QaRun? retryOf, CancellationToken token)
    {
        if (await Replay(requestId, scenario.Key, token) is { } replay) return (replay, false);
        if (await db.QaRuns.AnyAsync(run => run.ScenarioKey == scenario.Key && (run.Status == QaRunStatus.QUEUED || run.Status == QaRunStatus.RUNNING), token))
            throw new HierarchyConflictException(ErrorCatalog.QaRunActive.Create());
        var now = clock.GetUtcNow();
        var run = new QaRun(NewCode(now), scenario.Key, requestId, scenario.TimeoutSeconds, now, retryOf);
        db.QaRuns.Add(run);
        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            db.Entry(run).State = EntityState.Detached;
            if (await Replay(requestId, scenario.Key, token) is { } raced) return (raced, false);
            throw new HierarchyConflictException(ErrorCatalog.QaRunActive.Create());
        }
        return (Map(run), true);
    }

    private async Task<QaRunResponse?> Replay(Guid requestId, string scenarioKey, CancellationToken token)
    {
        var existing = await db.QaRuns.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(run => run.RequestId == requestId, token);
        if (existing is null) return null;
        if (existing.ScenarioKey != scenarioKey || existing.IsDeleted) throw new HierarchyConflictException(ErrorCatalog.QaRequestReused.Create());
        return Map(existing);
    }

    private static string NewCode(DateTimeOffset now) => $"qa-{now.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}-{Convert.ToHexString(Guid.NewGuid().ToByteArray(), 0, 3).ToLowerInvariant()}";

    internal static QaRunResponse Map(QaRun run) => new(run.Id, run.Code, run.ScenarioKey, run.Status.ToString(), run.Attempt, run.RetryOfId, run.CancelRequested,
        run.QueuedAt, run.StartedAt, run.FinishedAt, run.ResultCode, run.PassedSteps, run.FailedSteps, run.TimeoutSeconds, run.Version);
}

// Single in-process runner: one run at a time, oldest first. A restart marks runs left RUNNING as interrupted.
public sealed class QaRunner(IServiceScopeFactory scopes, QaCenterOptions options, TimeProvider clock, ILogger<QaRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RecoverAsync(stoppingToken);
            using var timer = new PeriodicTimer(options.PollInterval);
            do
            {
                try
                {
                    while (await RunNextAsync(stoppingToken)) { }
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    logger.LogWarning("BuildingFlow.QaCenter run processing failed ({ExceptionType}); retrying on the next interval.", error.GetType().Name);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task RecoverAsync(CancellationToken token)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
        foreach (var run in await db.QaRuns.Where(run => run.Status == QaRunStatus.RUNNING).ToListAsync(token)) run.Interrupt(clock.GetUtcNow());
        await db.SaveChangesAsync(token);
    }

    public async Task<bool> RunNextAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>();
        var run = await db.QaRuns.Where(value => value.Status == QaRunStatus.QUEUED).OrderBy(value => value.QueuedAt).ThenBy(value => value.Id).FirstOrDefaultAsync(stoppingToken);
        if (run is null) return false;
        run.Start(clock.GetUtcNow());
        try
        {
            await db.SaveChangesAsync(stoppingToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return true;
        }
        var scenario = scope.ServiceProvider.GetServices<IQaScenario>().FirstOrDefault(value => value.Key == run.ScenarioKey);
        if (scenario is null)
        {
            await SaveAsync(db, run, null, value => value.Interrupt(clock.GetUtcNow()), stoppingToken);
            return true;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(run.TimeoutSeconds));
        var sequence = 0;
        try
        {
            await foreach (var result in scenario.RunAsync(scope.ServiceProvider, timeout.Token).WithCancellation(timeout.Token))
            {
                var step = new QaRunStep(run.Id, ++sequence, result.Key, result.Status, result.Expected, result.Actual, result.DurationMs);
                await SaveAsync(db, run, step, value => value.Record(result.Status), stoppingToken);
                if (run.CancelRequested)
                {
                    await SaveAsync(db, run, null, value => value.Cancel(clock.GetUtcNow()), stoppingToken);
                    return true;
                }
            }
            await SaveAsync(db, run, null, value => value.Complete(clock.GetUtcNow()), stoppingToken);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            await SaveAsync(db, run, null, value => value.TimeOut(clock.GetUtcNow()), stoppingToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("BuildingFlow.QaCenter scenario {Scenario} failed ({ExceptionType}).", run.ScenarioKey, error.GetType().Name);
            var step = new QaRunStep(run.Id, ++sequence, "runner.error", QaStepStatus.FAILED, "Senaryo hatasız tamamlanır", "Beklenmeyen hata: " + error.GetType().Name, 0);
            await SaveAsync(db, run, step, value => value.Record(QaStepStatus.FAILED), stoppingToken);
            await SaveAsync(db, run, null, value => value.Complete(clock.GetUtcNow()), stoppingToken);
        }
        return true;
    }

    // Cancel requests change the run concurrently; reload so the runner never overwrites them.
    private static async Task SaveAsync(BuildingFlowDbContext db, QaRun run, QaRunStep? step, Action<QaRun> change, CancellationToken token)
    {
        if (step is not null) db.QaRunSteps.Add(step);
        for (var attempt = 1; ; attempt++)
        {
            await db.Entry(run).ReloadAsync(token);
            change(run);
            try
            {
                await db.SaveChangesAsync(token);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 3) { }
        }
    }
}

internal static class QaCenterEndpoints
{
    public static IServiceCollection AddQaCenter(this IServiceCollection services, IConfiguration configuration)
    {
        var options = QaCenterOptions.From(configuration);
        services.AddSingleton(options);
        if (!options.Enabled) return services;
        services.AddSingleton<IQaScenario, SystemHealthScenario>();
        services.AddSingleton<IQaScenario, ApiAuthorizationScenario>();
        services.AddSingleton<IQaScenario, DatabaseMigrationScenario>();
        services.AddScoped<QaCenterService>();
        services.AddSingleton<QaRunner>();
        services.AddHostedService(provider => provider.GetRequiredService<QaRunner>());
        return services;
    }

    // Disabled: no route exists, so the screen reports the centre as closed. Production can never enable it.
    public static void MapQaCenterEndpoints(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<QaCenterOptions>();
        if (!options.Enabled) return;
        if (app.Environment.IsProduction()) throw new InvalidOperationException("The QA centre cannot run in Production.");
        var root = app.MapGroup("/api/v1/qa-center").RequireAuthorization("CanManageQaCenter").WithTags("QA centre");
        root.MapGet("/overview", async (QaCenterService service, CancellationToken token) => Results.Ok(await service.Overview(token)));
        root.MapGet("/scenarios", (QaCenterService service) => Results.Ok(service.Scenarios()));
        root.MapGet("/runs", async (QaCenterService service, CancellationToken token, int page = 1, int pageSize = 20, string? status = null) =>
            Results.Ok(await service.Runs(page, pageSize, status, token)));
        root.MapGet("/runs/{runId:guid}", async (Guid runId, QaCenterService service, CancellationToken token) =>
            await service.Get(runId, token) is { } detail ? Results.Ok(detail) : Results.NotFound());
        root.MapPost("/runs", async (StartQaRunRequest request, QaCenterService service, CancellationToken token) => Accepted(await service.Start(request, token)));
        root.MapPost("/runs/{runId:guid}/retry", async (Guid runId, RetryQaRunRequest request, QaCenterService service, CancellationToken token) =>
            Accepted(await service.Retry(runId, request, token)));
        root.MapPut("/runs/{runId:guid}/cancel", async (Guid runId, CancelQaRunRequest request, QaCenterService service, CancellationToken token) =>
            await service.Cancel(runId, request, token) is { } run ? Results.Ok(run) : Results.NotFound());
    }

    private static IResult Accepted((QaRunResponse Run, bool Created)? result) => result switch
    {
        null => Results.NotFound(),
        { Created: true } created => Results.Created($"/api/v1/qa-center/runs/{created.Run.Id}", created.Run),
        { } replay => Results.Ok(replay.Run)
    };
}
