using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.AuditTrail;
using OpenDCP.Framework.AuditTrail.Classes;
using OpenDCP.Framework.AuditTrail.Models;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using OpenDCP.Framework.Configuration.Model.Extensions;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Datasource.Base.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Extensions;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class AuditTrailService : BaseService, IAuditTrailService
    {
        protected IServiceProvider _serviceProvider;
        protected DatasourceCache _cache;
        protected DbConnectionStringLoader _loader;

        public AuditTrailService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory,
            IMemoryCache memoryCache, IServiceProvider serviceProvider, DatasourceCache cache) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceProvider = serviceProvider;
            _cache = cache;
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task CreateAdminActionAuditTrailEntryAsync(AuditTrailAdminActionsEntryDTO model, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, 0, true);

            var auditTrailEntry = new AuditTrailAdminAction
            {
                ActionConfiguration = model.ActionConfiguration.ToString(),
                Exception = model.Exception,
                Module = model.Module,
                Reason = model.Reason,
                Timestamp = model.Timestamp,
                UserId = model.UserId,
            };

            await _dbContext.AddAsync(auditTrailEntry);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<MemoryStream> GetAdminActionsAuditTrailAsync(DateTime startTime, DateTime endTime, UserContextDTO user)
        {
            ValidateParameters(user, startTime, endTime);

            var auditTrailEntries = await _dbContext.AuditTrailAdminAction
                .Where(x => x.Timestamp >= startTime
                            && x.Timestamp <= endTime)
                .ToListAsync();

            var userIds = auditTrailEntries.Select(x => x.UserId).Distinct().ToList();

            var users = await _dbContext.User
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.UnixId
                })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.UnixId);

            MemoryStream ms = new MemoryStream();
            var streamWriter = new StreamWriter(ms, Encoding.GetEncoding("ISO-8859-1"));

            //Set table header
            await streamWriter.WriteLineAsync("User Id,Timestamp,ActionConfiguration,Reason,Module,Exception");

            //Write table body
            foreach (var entry in auditTrailEntries)
            {
                var userName = string.Empty;

                if (entry.UserId.HasValue &&
                    users.TryGetValue(entry.UserId.Value, out var foundUser))
                {
                    userName = foundUser ?? string.Empty;
                }

                entry.ActionConfiguration = FormatJsonValue(entry.ActionConfiguration);
                entry.Exception = FormatJsonValue(entry.Exception);

                await streamWriter.WriteLineAsync($"{userName},{entry.Timestamp},{entry.ActionConfiguration},{entry.Reason},{entry.Module},{entry.Exception}");
            }
            await streamWriter.FlushAsync();

            return ms;
        }

        public async Task<MemoryStream> GetConfigurationAuditTrailAsync(DateTime startTime, DateTime endTime, UserContextDTO user)
        {
            ValidateParameters(user, startTime, endTime);

            var auditHelper = new AuditHelper(_dbContext);

            //Get the changes
            var changes = await auditHelper.GetAuditForTablesAsync(
                startTime: startTime,
                endTime: endTime,
                recordId: null,
                siteId: null,
                new List<Type> {
                    typeof(Site),
                    typeof(Module),
                    typeof(DataSource),
                    typeof(DataType),
                    typeof(ConnectionBridge)
                });

            //For the user mapping
            var users = await _dbContext.User
                .Select(x => new
                {
                    x.UnixId,
                    x.Id
                })
                .AsNoTracking()
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.UnixId
                );

            //Handle the connection strings
            var key = GetEncryptionKey();
            var secretKeys = await GetSecretKeysAsync();

            var connectionChanges = changes
                .Where(x => x.FieldName == "ConnectionString")
                .SelectMany(x => GetConnectionStringDifferences(x, key, secretKeys));

            changes = changes
                .Where(x => x.FieldName != "ConnectionString")
                .Concat(connectionChanges)
                .OrderBy(x => x.Timestamp)
                .ToList();

            //Perfrom CSV formatting
            var result = await auditHelper.FormatAuditTrailFile(changes, users);

            return result;
        }

        public async Task<MemoryStream> GetSecurityAuditTrailAsync(DateTime startTime, DateTime endTime, UserContextDTO user)
        {
            ValidateParameters(user, startTime, endTime);

            var securityUserLog = await _dbContext.SecurityUserLog
                .Where(x => x.Timestamp >= startTime
                            && x.Timestamp <= endTime
                            && x.Action > 0)
                .AsNoTracking()
                .Select(x => new SecurityAuditTrailDTO
                {
                    Action = x.Action,
                    Type = "User",
                    Identifier = x.UnixId,
                    Timestamp = x.Timestamp,
                })
                .ToListAsync();


            var securitySystemTrail = await _dbContext.SecuritySystemLog
                .Where(x => x.Timestamp >= startTime
                            && x.Timestamp <= endTime
                            && x.Action > 0)
                .AsNoTracking()
                .Select(x => new SecurityAuditTrailDTO
                {
                    Action = x.Action,
                    Type = "System",
                    Identifier = x.ClientId,
                    Timestamp = x.Timestamp,
                })
                .ToListAsync();

            var securityAuditTrail = securityUserLog
                .Concat(securitySystemTrail)
                .OrderByDescending(x => x.Timestamp);

            MemoryStream ms = new MemoryStream();
            var streamWriter = new StreamWriter(ms, Encoding.GetEncoding("ISO-8859-1"));

            //Set Headers

            await streamWriter.WriteLineAsync("Identifier,Type,Timestamp,Action");

            foreach (var item in securityAuditTrail)
            {
                await streamWriter.WriteLineAsync($"{item.Identifier},{item.Type},{item.Timestamp},{item.Action.ToName()}");
            }
            await streamWriter.FlushAsync();

            return ms;
        }

        private static string FormatJsonValue(string? colVal)
        {
            if (colVal == null)
            {
                return "{}";
            }
            else
            {
                var cleaned = colVal
                    .Replace("\r\n", "")
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("\"", "\"\"");

                return $"\"{cleaned}\"";
            }
        }

        private static void ValidateParameters(UserContextDTO user, DateTime startTime, DateTime endTime)
        {
            if (!user.Permissions.Any(p => p.Permission == Permissions.Global.ADMIN))
            {
                throw new DCPForbiddenException(ErrorMessages.GlobalAdminPermissionRequired);
            }

            if (endTime <= startTime)
            {
                throw new DCPBadRequestException(ErrorMessages.StartAndEndTimeRequired);
            }
        }

        private static IEnumerable<AuditEntryDTO> GetConnectionStringDifferences(AuditEntryDTO entry, string encryptionKey, List<string> secretKeys)
        {
            var oldObj = TryDecryptJson(entry.OldValue, encryptionKey);
            var newObj = TryDecryptJson(entry.NewValue, encryptionKey);

            var allKeys = oldObj.Keys.Union(newObj.Keys);

            foreach (var key in allKeys)
            {
                oldObj.TryGetValue(key, out var oldVal);
                newObj.TryGetValue(key, out var newVal);

                if (!Equals(oldVal, newVal) || entry.ActionType == "Delete")
                {
                    if (secretKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        oldVal = string.Empty;
                        newVal = string.Empty;
                    }

                    yield return new AuditEntryDTO
                    {
                        SiteId = entry.SiteId,
                        ActionType = entry.ActionType,
                        FieldName = $"{entry.FieldName}.{key}",
                        Reason = entry.Reason,
                        RecordId = entry.RecordId,
                        TableName = entry.TableName,
                        Timestamp = entry.Timestamp,
                        UserId = entry.UserId,
                        OldValue = oldVal,
                        NewValue = newVal
                    };
                }
            }
        }

        private static Dictionary<string, string> TryDecryptJson(string? value, string key)
        {
            try
            {
                if (value is null) return new Dictionary<string, string>();
                return EncryptionHelper.DecryptConnectionString(value, key)!.AsObject()!
                    .ToDictionary(kv => kv.Key, kv => kv.Value?.ToString() ?? string.Empty);
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private async Task<List<string>> GetSecretKeysAsync()
        {
            var pipeline = new PluginPipeline<NoRequest, List<string>>(_cache, _serviceProvider, _loader)
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var res = await pipeline.RunOnAllTypesAsync<IDatasource>(
                (ds, args, ct) => Task.FromResult(ds.GetSecretFields()),
                args: Array.Empty<object>()
            );
            var result = res.SelectMany(r => r.Request).Distinct().ToList();

            var calculationNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);

            foreach (var type in Enum.GetValues<CalculationNodeType>())
            {
                var node = calculationNodeFactory.LoadByType(type);
                result.AddRange(node.GetSecretFields());
            }

            return result.Distinct().ToList();
        }
    }
}
