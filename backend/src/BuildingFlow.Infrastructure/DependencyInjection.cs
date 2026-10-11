using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BuildingFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System); services.AddScoped<AuditInterceptor>();
        services.AddDbContext<BuildingFlowDbContext>((provider, options) => options.UseBuildingFlowNpgsql(configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("ConnectionStrings:Database is required.")).AddInterceptors(provider.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<BuildingFlow.Application.IPropertyStore, PropertyStore>();
        services.AddScoped<BuildingFlow.Application.IHierarchyStore, HierarchyStore>();
        services.AddScoped<BuildingFlow.Application.IPeopleStore, PeopleStore>();
        services.AddScoped<BuildingFlow.Application.IDuesStore, DuesStore>();
        services.AddScoped<BuildingFlow.Application.IPaymentStore, PaymentStore>();
        services.AddScoped<BuildingFlow.Application.IReportStore, ReportStore>();
        services.AddScoped<BuildingFlow.Application.IMeterStore, MeterStore>();
        services.AddScoped<BuildingFlow.Application.IExpenseStore, ExpenseStore>();
        services.AddScoped<BuildingFlow.Application.ICommunicationStore, CommunicationStore>();
        services.AddSingleton<BuildingFlow.Application.IFileStorage, MinioFileStorage>();
        services.AddSingleton(_ => RabbitMessagingOptions.From(configuration));
        services.AddSingleton<BuildingFlow.Application.IMessagePublisher, RabbitMessagePublisher>();
        services.AddScoped<BuildingFlow.Application.IMessageOutbox, EfMessageOutbox>();
        services.AddScoped<OutboxRelay>();
        services.AddScoped<MessageProcessor>();
        return services;
    }
}
