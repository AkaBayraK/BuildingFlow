using BuildingFlow.Api;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddBuildingFlowAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>().Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) => await context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString() })
    }, context.RequestAborted)
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapGet("/api/v1/status", () => Results.Ok(new { application = "BuildingFlow", phase = "Foundation" }))
    .RequireAuthorization();
app.Run();

public partial class Program { }
