using Microsoft.Extensions.DependencyInjection;
using Business.Services;
using Business.Services.Base;
using Business.DataRepositories;

namespace Business.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IMessageBrokerService, MessageBrokerService>();
            services.AddScoped<IEmailSenderService, EmailSenderService>();
            services.AddScoped<IBackgroundJobService, BackgroundJobService>();
            services.AddScoped<ILinkedUserService, LinkedUserService>();

            return services;
        }
    }
}
