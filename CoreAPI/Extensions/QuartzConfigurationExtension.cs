using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Business.Services;
using Business.Services.Base;
using Business.Jobs.Scheduled;

namespace CoreAPI.Extensions
{
    public static class QuartzConfigurationExtension
    {
        public static void AddQuartzConfiguration(this IServiceCollection services)
        {
            services.AddQuartz(q =>
            {
                // Configure global jobs
                
                // Daily job to deactivate expired groups (runs at 00:00 every day)
                q.AddJob<DeactivateExpiredGroupsJob>(opts => opts.WithIdentity("DeactivateExpiredGroupsJob"))
                    .AddTrigger(opts => opts
                        .ForJob("DeactivateExpiredGroupsJob")
                        .WithIdentity("DeactivateExpiredGroupsTrigger")
                        .WithCronSchedule("0 0 0 * * ?"));

                // Daily job to notify about expiring groups (runs at 00:00 every day)
                q.AddJob<NotifyExpiringGroupsJob>(opts => opts.WithIdentity("NotifyExpiringGroupsJob"))
                    .AddTrigger(opts => opts
                        .ForJob("NotifyExpiringGroupsJob")
                        .WithIdentity("NotifyExpiringGroupsTrigger")
                        .WithCronSchedule("0 0 0 * * ?"));
            });

            services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        }
    }
}
