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
builder.Services.AddScoped<DuesService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<CommunicationService>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type")));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BuildingFlowDbContext>().Database.MigrateAsync();
    return;
}

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
app.MapCommunicationEndpoints();
app.Run();

public partial class Program { }
