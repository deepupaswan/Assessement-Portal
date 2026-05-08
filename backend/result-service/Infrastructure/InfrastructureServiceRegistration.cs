using Microsoft.Extensions.DependencyInjection;
using ResultService.Application.Repositories;
using ResultService.Application.Services;
using ResultService.Infrastructure.Persistence;
using ResultService.Infrastructure.Persistence.Repositories;
using ResultService.Infrastructure.Services;

namespace ResultService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddPersistenceServices(connectionString);
        services.AddScoped<IResultRepository, ResultRepository>();
        services.AddScoped<IResultService>(serviceProvider =>
            new ResultAppService(
                serviceProvider.GetRequiredService<IResultRepository>(),
                serviceProvider.GetRequiredService<MassTransit.IPublishEndpoint>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResultAppService>>(),
                new HttpClient(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()));
        return services;
    }
}
