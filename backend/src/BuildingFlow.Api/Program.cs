using BuildingFlow.Api;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
        o.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
    });
}
else
{
    builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);
}
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
    // Deny by default: an endpoint without its own policy needs a signed-in user; public routes opt out with AllowAnonymous.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("CanReadProperties", policy => policy.RequireAuthenticatedUser().RequireClaim("sub"));
    options.AddPolicy("CanManageProperties", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN"));
    options.AddPolicy("CanManageHierarchy", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN"));
    options.AddPolicy("CanManagePeople", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER"));
    options.AddPolicy("CanManageDues", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"));
    options.AddPolicy("CanManageWebsite", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN", "MANAGER"));
    options.AddPolicy("CanPublishWebsite", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN", "SITE_ADMIN"));
    options.AddPolicy("CanManageQaCenter", policy => policy.RequireAuthenticatedUser().RequireClaim("sub").RequireRole("SUPER_ADMIN"));
});
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<HierarchyService>();
builder.Services.AddScoped<PeopleService>();
builder.Services.AddScoped<CentralPeopleService>();
builder.Services.AddScoped<PersonMessageService>();
builder.Services.AddScoped<DuesService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<LatePenaltyService>();
builder.Services.AddScoped<OnlinePaymentService>();
builder.Services.AddScoped<ResidentPaymentService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<MeterService>();
builder.Services.AddScoped<MaintenanceService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<CommonAreaService>();
builder.Services.AddScoped<CommonAreaClosureService>();
builder.Services.AddScoped<CommonAreaReservationService>();
builder.Services.AddScoped<ParkingService>();
builder.Services.AddScoped<SurveyService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddScoped<SmsConsentService>();
builder.Services.AddScoped<ConsentService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MobileOnboardingService>();
builder.Services.AddScoped<PersonAccountLinkService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CommunicationService>();
builder.Services.AddScoped<ListingService>();
builder.Services.AddScoped<WebsiteDomainService>();
builder.Services.AddScoped<WebsiteService>();
builder.Services.AddScoped<WebsiteMediaService>();
builder.Services.AddHostedService<ProfilePhotoMaintenance>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type")));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);
builder.Services.AddSystemStatus();
builder.Services.AddQaCenter(builder.Configuration);
builder.Services.AddWeather(builder.Configuration);

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
    await PersonCodeBackfill.RunAsync(app.Services, migrationLogger);
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
app.UseSessionContextAccess();
if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) => await context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString() })
    }, context.RequestAborted)
}).AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapGet("/api/v1/status", () => Results.Ok(new { application = "BuildingFlow", phase = "Foundation" }))
    .RequireAuthorization();
app.MapMobileSessionEndpoints();
app.MapMobileSyncEndpoints();
app.MapMobileProfileEndpoints();
app.MapAccountEndpoints();
app.MapContactVerificationEndpoints();
app.MapMobileOwnerEndpoints();
app.MapMobileManagerEndpoints();
app.MapMobileFinanceEndpoints();
app.MapMobileNoticeEndpoints();
app.MapPropertyEndpoints();
app.MapHierarchyEndpoints();
app.MapDirectUnitEndpoints();
app.MapPeopleEndpoints();
app.MapCentralPeopleEndpoints();
app.MapPersonTransitionEndpoints();
app.MapManagementAssignmentEndpoints();
app.MapPersonCenterEndpoints();
app.MapSessionContextEndpoints();
app.MapSmsEndpoints();
app.MapSmsConsentEndpoints();
app.MapSmsOptOutCallback();
app.MapConsentEndpoints();
app.MapMobileConsentEndpoints();
app.MapRequestManagementEndpoints();
app.MapUserEndpoints();
app.MapMobileOnboardingEndpoints();
app.MapDuesEndpoints();
app.MapPaymentEndpoints();
app.MapLatePenaltyEndpoints();
app.MapOnlinePaymentEndpoints();
app.MapExpenseEndpoints();
app.MapMeterEndpoints();
app.MapMaintenanceEndpoints();
app.MapSupplierEndpoints();
app.MapSupplierDocumentsEndpoints();
app.MapSupplierQuoteEndpoints();
app.MapSupplierWorkOrderEndpoints();
app.MapSupplierMaintenanceEndpoints();
app.MapSupplierInvoiceEndpoints();
app.MapSupplierInsightEndpoints();
app.MapCommonAreaEndpoints();
app.MapSurveyEndpoints();
app.MapReportEndpoints();
app.MapDashboardEndpoints();
app.MapWeatherEndpoints();
app.MapSystemStatusEndpoints();
app.MapDefinitionEndpoints();
app.MapQaCenterEndpoints();
app.MapCommunicationEndpoints();
app.MapWebsiteInquiryEndpoints();
app.MapListingEndpoints();
app.MapPublicListingEndpoints();
app.MapWebsiteDomainEndpoints();
app.MapWebsiteEndpoints();
app.MapWebsiteMediaEndpoints();
app.MapPublicWebsiteMediaEndpoints();
app.MapPublicWebsiteEndpoints();
app.MapParkingEndpoints();
app.Run();

public partial class Program { }
