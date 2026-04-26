using Microsoft.Extensions.DependencyInjection;
using ResultService.Application.Services;
using ResultService.Infrastructure.Persistence;
using ResultService.Infrastructure.Services;

namespace ResultService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddPersistenceServices(connectionString);
        services.AddScoped<IResultService>(serviceProvider =>
            new ResultAppService(
                serviceProvider.GetRequiredService<ResultDbContext>(),
                serviceProvider.GetRequiredService<MassTransit.IPublishEndpoint>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResultAppService>>(),
                new HttpClient(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()));
        return services;
    }
}
