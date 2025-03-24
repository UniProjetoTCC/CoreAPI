using Hangfire;
using Hangfire.Redis.StackExchange;
using StackExchange.Redis;
using Business.Jobs.Scheduled;
using Business.Jobs.Background;
using Hangfire.Dashboard;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CoreAPI.Extensions
{
    public static class HangfireConfigurationExtension
    {
        public static void AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConfig = configuration["Redis:Configuration"] ?? 
                throw new InvalidOperationException("Redis configuration is not set!");

            var multiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions 
            { 
                EndPoints = { redisConfig },
                AbortOnConnectFail = false
            });

            var storageOptions = new RedisStorageOptions
            {
                Prefix = "hangfire:",
                SucceededListSize = 1000,
                DeletedListSize = 1000,
                InvisibilityTimeout = TimeSpan.FromMinutes(30),
                ExpiryCheckInterval = TimeSpan.FromHours(1)
            };

            // Configure Hangfire
            services.AddHangfire((provider, config) =>
            {
                var storage = new RedisStorage(multiplexer, storageOptions);
                GlobalConfiguration.Configuration
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseStorage(storage);

                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseRedisStorage(multiplexer, storageOptions);

                // Configure retry attempts
                config.UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = 3,
                    DelaysInSeconds = new[] { 300, 600, 900 } // 5min, 10min, 15min
                });
            });

            // Configure Hangfire Server
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Environment.ProcessorCount * 2; // Workers based on the processor count * 2
                options.Queues = new[] { "critical", "default", "normal", "low" }; // Order matters - critical first
                options.ServerTimeout = TimeSpan.FromMinutes(5);
                options.ShutdownTimeout = TimeSpan.FromMinutes(1);
            });

            // Register jobs
            services.AddScoped<DeactivateExpiredGroupsJob>();
            services.AddScoped<NotifyExpiringGroupsJob>();
            services.AddScoped<UserDowngradeJob>();

            // Register RecurringJobConfigurator and RecurringJobManager
            services.AddScoped<RecurringJobConfigurator>();
            services.AddSingleton<IRecurringJobManager>(provider => new RecurringJobManager());
        }

        public static void UseHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                IsReadOnlyFunc = _ => false,
                DisplayStorageConnectionString = false,
                DashboardTitle = "Background Jobs",
                AppPath = "/",
                StatsPollingInterval = 5000
            });

            // Configure recurring jobs using the service
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var jobConfigurator = scope.ServiceProvider.GetRequiredService<RecurringJobConfigurator>();
                jobConfigurator.ConfigureRecurringJobs();
            }
        }
    }

    public class RecurringJobConfigurator
    {
        private readonly ILogger<RecurringJobConfigurator> _logger;
        private readonly IRecurringJobManager _jobManager;

        public RecurringJobConfigurator(ILogger<RecurringJobConfigurator> logger, IRecurringJobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }

        public void ConfigureRecurringJobs()
        {
            try
            {
                _jobManager.AddOrUpdate<DeactivateExpiredGroupsJob>(
                    "deactivate-expired-groups",
                    job => job.Execute(),
                    "0 0 * * *", // Cron expression for daily at midnight
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Utc,
                    }
                );

                _jobManager.AddOrUpdate<NotifyExpiringGroupsJob>(
                    "notify-expiring-groups",
                    job => job.Execute(),
                    "0 9 * * *", // Cron expression for daily at 9 AM UTC
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Utc,
                    }
                );

                _logger.LogInformation("Recurring jobs configured successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring recurring jobs");
                throw;
            }
        }
    }

    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var logger = httpContext.RequestServices.GetService<ILogger<HangfireAuthorizationFilter>>();
            
            try 
            {
                // Try to get token from query string
                var token = httpContext.Request.Query["authorization"].ToString();
                logger?.LogInformation($"Token from query: {token}");

                if (!string.IsNullOrEmpty(token))
                {
                    var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                        throw new InvalidOperationException("JWT_SECRET environment variable is not set!");

                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = "http://localhost:5000",
                        ValidateAudience = true,
                        ValidAudience = "http://localhost:5000",
                        ClockSkew = TimeSpan.Zero
                    };
                    
                    tokenHandler.ValidateToken(token.Replace("Bearer ", ""), tokenValidationParameters, out SecurityToken validatedToken);

                    var jwtToken = (JwtSecurityToken)validatedToken;
                    var role = jwtToken.Claims.First(x => x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Value;

                    logger?.LogInformation($"Token validated successfully. Role: {role}");
                    return role == "AdminUser";
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error validating token: {ex.Message}");
            }

            return false;
        }
    }
}
