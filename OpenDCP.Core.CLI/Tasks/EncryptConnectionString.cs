using System.Text.Json.Nodes;
using System.Text.Json;
using OpenDCP.Framework.Security.Classes;

namespace OpenDCP.Core.CLI.Tasks
{
    public class EncryptConnectionStringTask
    {
        public static Task RunAsync(string key)
        {
            var connectionString = File.ReadAllText("connection-string.json");
            var doc = JsonDocument.Parse(connectionString).RootElement;
            var connectionStringJson = JsonNode.Parse(doc.GetRawText())!.AsObject();
            Console.WriteLine(EncryptionHelper.EncryptConnectionString(connectionStringJson, key));
            return Task.CompletedTask;
        }
    }
}
