using Microsoft.EntityFrameworkCore;
using Data.Context;

namespace CoreAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Set the connection string with .env variabless
            var connectionString = builder.Configuration.GetConnectionString("SqlConnection");

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

            builder.Services.AddDbContext<CoreAPIContext>(options =>
                options.UseNpgsql(connectionString));

            // Configuração do AutoMapper
            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Get logger for application messages
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            // Aplica as migrações automaticamente
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<CoreAPIContext>();
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "A Error occurred while migrating the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();

                logger.LogInformation(@"
                ╔════════════════════════════════════════════╗
                ║             API Documentation              ║
                ╠════════════════════════════════════════════╣
                ║  Swagger UI: http://localhost:5000/swagger ║
                ╚════════════════════════════════════════════╝
                ");
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
