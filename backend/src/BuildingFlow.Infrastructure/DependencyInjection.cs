using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BuildingFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System); services.AddScoped<AuditInterceptor>();
        services.AddDbContext<BuildingFlowDbContext>((provider, options) => options.UseNpgsql(configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("ConnectionStrings:Database is required.")).AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));
        return services;
    }
}
