using OpenDCP.Core.DAL;
using OpenDCP.Framework.Security.Classes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.CLI.Tasks
{
    public static class MigrateModuleConnectionStringsTask
    {
        public static async Task RunAsync(DCPCoreDbContext dbContext, string key, string sourceDBconnectionString)
        {
            var records = await FetchConnectionStringsAsync(sourceDBconnectionString);

            if (records is null)
                throw new InvalidDataException("Could not load connection");

            await dbContext.Database.ExecuteSqlAsync($"SELECT periods.drop_system_versioning('\"Module\"')");
            await dbContext.Database.ExecuteSqlAsync($"GRANT UPDATE ON TABLE \"Module_history\" TO CURRENT_USER;");

            foreach (var record in records.Select(r => r.DecryptDetails))
            {
                var connectionStringJson = ParseConnectionStringAsync(record, key);
                if (connectionStringJson is null) continue;

                await UpdateConnectionStringsAsync(dbContext, record, connectionStringJson, key);
            }

            await dbContext.Database.ExecuteSqlAsync($"SELECT periods.add_system_versioning('\"Module\"')");
        }

        private static async Task<List<ExportedConnectionString>> FetchConnectionStringsAsync(string sourceDbConnectionString)
        {
            const string sqlQuery = $@"SELECT DISTINCT RConnection
                              FROM dbo.Modules FOR SYSTEM_TIME ALL 
                              WHERE RConnection IS NOT NULL";

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

        private static JsonObject? ParseConnectionStringAsync(string encryptedValue, string key)
        {
            if (string.IsNullOrEmpty(encryptedValue)) return null;

            JsonDocument? jDoc;

            try
            {
                jDoc = JsonDocument.Parse(encryptedValue);
            }
            catch (JsonException)
            {
                try
                {
                    jDoc = JsonDocument.Parse(EncryptionHelper.DecryptLegacy(encryptedValue, key));
                }
                catch
                {
                    Console.WriteLine($"Can't read: {encryptedValue}");
                    return null;
                }
            }

            var root = jDoc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Can't cast JSON string");

            var targetElement = root.EnumerateObject().Count() == 1 ? root.EnumerateObject().First().Value : root;

            if (targetElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"Expected JSON object but got {targetElement.ValueKind}");

            return JsonNode.Parse(targetElement.GetRawText())!.AsObject();
        }

        // Update the encrypted connection strings in the database
        private static async Task UpdateConnectionStringsAsync(
            DCPCoreDbContext dbContext,
            string originalValue,
            JsonObject connectionStringJson,
            string key)
        {
            var encryptedValue = EncryptionHelper.EncryptConnectionString(connectionStringJson, key);

            var sql = @"
                UPDATE ""Module""
                SET ""ConnectionString"" = @encryptedValue
                WHERE ""ConnectionString"" = @originalValue;

                UPDATE ""Module_history""
                SET ""ConnectionString"" = @encryptedValue
                WHERE ""ConnectionString"" = @originalValue;
                ";

            await dbContext.Database.ExecuteSqlRawAsync(
                sql,
                new NpgsqlParameter("@encryptedValue", encryptedValue),
                new NpgsqlParameter("@originalValue", originalValue)
            );
        }
    }
}
