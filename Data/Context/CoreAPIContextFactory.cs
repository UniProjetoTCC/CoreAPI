using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Data.Context
{
    public class CoreAPIContextFactory : IDesignTimeDbContextFactory<CoreAPIContext>
    {
        public CoreAPIContext CreateDbContext(string[] args)
        {
            // Configure o IConfiguration para ler o appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("SqlConnection");

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

            var optionsBuilder = new DbContextOptionsBuilder<CoreAPIContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new CoreAPIContext(optionsBuilder.Options);
        }
    }
}
