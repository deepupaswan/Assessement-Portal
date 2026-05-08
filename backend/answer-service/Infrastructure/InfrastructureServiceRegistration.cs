using AnswerService.Application.Repositories;
using AnswerService.Application.Services;
using AnswerService.Infrastructure.Persistence;
using AnswerService.Infrastructure.Persistence.Repositories;
using AnswerService.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerService.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            services.AddPersistenceServices(connectionString);
            services.AddScoped<IAnswerRepository, AnswerRepository>();
            services.AddScoped<IAnswerService, AnswerAppService>();
            return services;
        }
    }
}