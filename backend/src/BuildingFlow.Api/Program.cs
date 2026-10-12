using BuildingFlow.Api;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);
builder.Services.AddBuildingFlowTelemetry(builder.Logging,builder.Configuration,"buildingflow-api");
builder.Services.AddReleaseSecurity(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.Customize);
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddBuildingFlowAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReadProperties", policy => policy.RequireAuthenticatedUser().RequireClaim("sub"));
    options.AddPolicy("CanManageProperties", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN"));
    options.AddPolicy("CanManageHierarchy", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER"));
    options.AddPolicy("CanManagePeople", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER"));
    options.AddPolicy("CanManageDues", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"));
});
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<HierarchyService>();
builder.Services.AddScoped<PeopleService>();
builder.Services.AddScoped<PersonMessageService>();
builder.Services.AddScoped<DuesService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<MeterService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<CommunicationService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type")));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();
var migrateOnly = args.Contains("--migrate", StringComparer.Ordinal);
if (migrateOnly || app.Configuration.GetValue("Database:AutoMigrate", app.Environment.IsDevelopment()))
{
    var migrationLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("BuildingFlow.Migrations");
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        await DatabaseMigration.ApplyPendingAsync(scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>(), migrationLogger);
    }
    catch (Exception exception)
    {
        migrationLogger.LogCritical("BuildingFlow.Migrations: schema migration failed ({ExceptionType}); the API will not start", exception.GetType().Name);
        throw;
    }
    if (migrateOnly) return;
}

app.UseReleaseSecurity();
app.UseBuildingFlowRequestTelemetry();
app.UseExceptionHandler();
app.UseCors();
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    if (!context.Request.Path.StartsWithSegments("/api")) return;
    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails { Status = context.Response.StatusCode, Instance = context.Request.Path };
    ApiProblems.Apply(problem, ApiProblems.ForStatus(context.Response.StatusCode));
    await ApiProblems.Write(context, problem, context.RequestAborted);
});
app.UseAuthentication();
app.UseRateLimiter();
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
app.MapPropertyEndpoints();
app.MapHierarchyEndpoints();
app.MapDirectUnitEndpoints();
app.MapPeopleEndpoints();
app.MapDuesEndpoints();
app.MapPaymentEndpoints();
app.MapExpenseEndpoints();
app.MapMeterEndpoints();
app.MapReportEndpoints();
app.MapCommunicationEndpoints();
app.Run();

public partial class Program { }
