using OpenDCP.Core.CLI.Tasks;
using OpenDCP.Core.DAL;
using OpenDCP.Framework.ConfigurationProvider.Factories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenDCP.Core.CLI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet run -- <command>");
                return;
            }

            var config = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .Build();

            var services = new ServiceCollection();

            var secretConfigurationFactory = new SecretConfigurationProviderFactory();
            var secretConfigurationProvider = secretConfigurationFactory.Load("HashiCorpVault", null, config);

            services.AddDbContext<DCPCoreDbContext>(options =>
               options.UseNpgsql(secretConfigurationProvider.GetConnectionString("PostgreSQL"),
               sqlOptions => sqlOptions.CommandTimeout(60)),
               ServiceLifetime.Scoped
           );


            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<DCPCoreDbContext>();
            var key = secretConfigurationProvider.GetEncryptKeyDetailsAsync();

            var sString = secretConfigurationProvider.GetSecretAsJSON(
                config["SecretConfigurationProvider:Settings:Paths:SDatabase"],
                config["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]
            );

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = GetRequired(sString, "Server"),
                InitialCatalog = GetRequired(sString, "DatabaseName"),
                UserID = GetRequired(sString, "UserId"),
                Password = GetRequired(sString, "Password"),
                TrustServerCertificate = true
            };

            string sourceDBconnectionString = builder.ConnectionString;

            switch (args[0].ToLower())
            {
                case "encrypt-connection-strings":
                    Console.WriteLine("Encrypting connection strings...");
                    EncryptConnectionStringTask.RunAsync(key).Wait();
                    break;
                case "decrypt-connection-strings":
                    Console.WriteLine("Decrypting connection strings...");
                    DecryptConnectionStringTask.RunAsync(dbContext, args[1], int.Parse(args[2]), key).Wait();
                    break;
                case "update-datasource-connection-strings":
                    Console.WriteLine("Updating datasource connection strings...");
                    MigrateDatasourceConnectionStringsTask.RunAsync(dbContext, key, sourceDBconnectionString).Wait();
                    break;
                case "migrate-server-clients":
                    Console.WriteLine("Migrating server clients...");
                    MigrateServerClientsTask.RunAsync(dbContext, key, sourceDBconnectionString, secretConfigurationProvider, config).Wait();
                    break;
                case "update-module-connection-strings":
                    Console.WriteLine("Updating module connection strings...");
                    MigrateModuleConnectionStringsTask.RunAsync(dbContext, key, sourceDBconnectionString).Wait();
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }

        private static string GetRequired(System.Text.Json.Nodes.JsonObject obj, string key)
        {
            return obj[key]?.ToString()
                ?? throw new InvalidOperationException($"Missing required secret value: '{key}'");
        }
    }
}
