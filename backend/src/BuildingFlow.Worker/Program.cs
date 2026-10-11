using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using BuildingFlow.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
if (string.IsNullOrWhiteSpace(builder.Configuration["RabbitMQ:Host"])) throw new InvalidOperationException("RabbitMQ:Host is required.");
builder.Services.AddSingleton<ICurrentUserService, SystemUser>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxRelayWorker>();
builder.Services.AddHostedService<MessageConsumerWorker>();
await builder.Build().RunAsync();
