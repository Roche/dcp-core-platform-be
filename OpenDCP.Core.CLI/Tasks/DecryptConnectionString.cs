using OpenDCP.Framework.Security.Classes;
using OpenDCP.Core.DAL;

namespace OpenDCP.Core.CLI.Tasks
{
    public class DecryptConnectionStringTask
    {
        public static async Task RunAsync(DCPCoreDbContext dbContext, string type, int id, string key)
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided.", nameof(type));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key must be provided.", nameof(key));

            string connectionString = string.Empty;

            if (type.Equals("module", StringComparison.OrdinalIgnoreCase))
            {
                var module = await dbContext.Module.FindAsync(id);
                if (module == null)
                {
                    Console.WriteLine($"Module with ID {id} not found.");
                    return;
                }
                connectionString = module.ConnectionString ?? string.Empty;
            }
            else if (type.Equals("datasource", StringComparison.OrdinalIgnoreCase))
            {
                var datasource = await dbContext.DataSource.FindAsync(id);
                if (datasource == null)
                {
                    Console.WriteLine($"Datasource with ID {id} not found.");
                    return;
                }
                connectionString = datasource.ConnectionString ?? string.Empty;
            }
            else
            {
                Console.WriteLine($"Invalid type: {type}. Must be 'module' or 'datasource'.");
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("Connection string is empty.");
                return;
            }

            try
            {
                var decryptedString = EncryptionHelper.DecryptConnectionString(connectionString, key);
                Console.WriteLine(decryptedString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting connection string: {ex.Message}");
            }
        }
    }
}
