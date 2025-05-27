using AspNetCoreRateLimit;
using Business.Extensions;
using Business.Services;
using Business.Services.Base;
using CoreAPI.Extensions;
using CoreAPI.Logging;
using Data.Context;
using Data.Extensions;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace CoreAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Redis Cache
            var redisConfig = builder.Configuration["Redis:Configuration"] ??
                throw new InvalidOperationException("Redis configuration is not set!");
            var redisInstanceName = builder.Configuration["Redis:InstanceName"] ??
                throw new InvalidOperationException("Redis instance name is not set!");

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfig;
                options.InstanceName = redisInstanceName;
            });

            // Configure Redis as Message Broker and Hangfire Storage
            var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
            builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

            // Configure Hangfire
            builder.Services.AddHangfireConfiguration(builder.Configuration);
            builder.Services.AddHangfire(config =>
            {
                config.UseRedisStorage(multiplexer, new RedisStorageOptions
                {
                    Prefix = "hangfire:",
                    SucceededListSize = 1000
                });
            });
            builder.Services.AddHangfireServer();

            // Configure rate limiting with Redis
            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

            // Configure custom logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.FormatterName = "CustomConsole";
            }).AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();

            // Add services to the container
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Set the connection string with .env variables
            var connectionString = builder.Configuration.GetConnectionString("SqlConnection") ??
            throw new InvalidOperationException("Connection string 'SqlConnection' not found in configuration.");

            if (connectionString is not null)
            {
                connectionString = connectionString
                    .Replace("${DB_HOST}", Environment.GetEnvironmentVariable("DB_HOST"))
                    .Replace("${DB_PORT}", Environment.GetEnvironmentVariable("DB_PORT"))
                    .Replace("${DB_NAME}", Environment.GetEnvironmentVariable("DB_NAME"))
                    .Replace("${DB_USER}", Environment.GetEnvironmentVariable("DB_USER"))
                    .Replace("${DB_PASSWORD}", Environment.GetEnvironmentVariable("DB_PASSWORD"));
            }
            else
            {
                throw new InvalidOperationException("Connection string 'SqlConnection' not found in configuration.");
            }

            // Set the context (database)
            builder.Services.AddDbContext<CoreAPIContext>(options =>
                options.UseNpgsql(connectionString));

            // Add Identity, the user management
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Configuração de senha
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;

                // Configurações de lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Configuração 2FA
                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
            })
                .AddEntityFrameworkStores<CoreAPIContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<AuthenticatorTokenProvider<IdentityUser>>(TokenOptions.DefaultAuthenticatorProvider)
                .AddPasswordValidator<CustomPasswordValidator>();

            // Add JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:ValidAudience"] ??
                        throw new InvalidOperationException("JWT:ValidAudience not configured"),
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"] ??
                        throw new InvalidOperationException("JWT:ValidIssuer not configured"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        Environment.GetEnvironmentVariable("JWT_SECRET") ??
                        throw new InvalidOperationException("JWT_SECRET environment variable is not set!")
                    ))
                };
            });

            builder.Services.AddSingleton(provider =>
            {
                var jwtSecret = builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret not found in configuration");

                return new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:ValidAudience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Add Auto Mapper, the class mapper to database classes and vice versa
            builder.Services.AddAutoMapper(typeof(Program));

            // Register repositories
            builder.Services.AddRepositories();
            builder.Services.AddBusinessServices();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Core API",
                    Version = "v1",
                    Description = "Core API - .NET Web API",
                    Contact = new OpenApiContact
                    {
                        Name = "Omnos",
                        Email = "omnos.info@gmail.com"
                    }
                });

                // Use XML comments in Swagger
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);

                // Add JWT Authentication
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            var app = builder.Build();

            // Configure rate limiting middleware (have to be one of the first middlewares)
            app.UseIpRateLimiting();

            // Get logger for application messages
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<CoreAPIContext>();
                    context.Database.Migrate();

                    // Setup default roles and subscription plans
                    var roleService = services.GetRequiredService<IRoleService>();
                    await roleService.SetupRolesAndPlansAsync();

                    logger.LogInformation("Database migration and initial setup completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database or setting up initial data.");
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Core API v1");
                    c.EnableDeepLinking();
                    c.EnableFilter();
                    c.EnableValidator();
                    c.EnableTryItOutByDefault();
                });

                logger.LogInformation(">>> Swagger UI: http://localhost:5000/swagger <<<");
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
