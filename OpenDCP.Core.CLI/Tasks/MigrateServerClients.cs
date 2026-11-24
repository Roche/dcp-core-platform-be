using OpenDCP.Core.DAL;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.Security.Classes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OpenDCP.Core.CLI.Tasks
{
    public static class MigrateServerClientsTask
    {
        public static async Task RunAsync(DCPCoreDbContext dbContext, string key, string sourceDBconnectionString,
            ISecretConfigurationProvider secretConfigurationProvider, IConfiguration config)
        {
            var records = await FetchClientsAsync(sourceDBconnectionString);

            if (records is null)
                throw new InvalidDataException("Could not load connection");

            var sql = @"
                UPDATE ""Client""
                SET ""ClientSecret"" = @encryptedValue
                WHERE ""Name"" = @originalValue;";

            foreach (var record in records.Select(r => r.DecryptDetails))
            {
                var jSecret = secretConfigurationProvider.GetSecretAsJSON(
                    $"{config["SecretConfigurationProvider:Settings:Paths:AuthorizationServer"]}/{record}",
                    config["SecretConfigurationProvider:Settings:MountPoints:AppConfig"]
                );

                if (jSecret is null) continue;

                var nsecret = EncryptionHelper.Encrypt(jSecret![1]!.ToString(), key);

                await dbContext.Database.ExecuteSqlRawAsync(
                 sql,
                 new NpgsqlParameter("@encryptedValue", nsecret),
                 new NpgsqlParameter("@originalValue", record)
                );
            }
        }

        private static async Task<List<ExportedConnectionString>> FetchClientsAsync(string sourceDbConnectionString)
        {
            const string sqlQuery = $@"SELECT DISTINCT NAME
                              FROM dbo.Clients";

            var records = new List<ExportedConnectionString>();

            await using var conn = new SqlConnection(sourceDbConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sqlQuery, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                records.Add(new ExportedConnectionString { DecryptDetails = reader.GetString(0) });
            }

            return records;
        }
    }
}
