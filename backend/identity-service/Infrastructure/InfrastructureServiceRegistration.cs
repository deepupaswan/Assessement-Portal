using IdentityService.Application.Services;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddPersistenceServices(connectionString);
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IJwtService, JwtService>();
        return services;
    }
}
