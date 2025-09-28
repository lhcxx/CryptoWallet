using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CryptoWallet.Data;

namespace CryptoWallet.Configuration
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, string connectionString, string provider = "InMemory")
        {
            switch (provider.ToLower())
            {
                case "sqlserver":
                    services.AddDbContext<WalletDbContext>(options =>
                        options.UseSqlServer(connectionString));
                    break;
                case "postgresql":
                    services.AddDbContext<WalletDbContext>(options =>
                        options.UseNpgsql(connectionString));
                    break;
                case "mysql":
                    services.AddDbContext<WalletDbContext>(options =>
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
                    break;
                case "inmemory":
                default:
                    services.AddDbContext<WalletDbContext>(options =>
                        options.UseInMemoryDatabase("WalletDb"));
                    break;
            }

            return services;
        }
    }
}
