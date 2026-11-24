using OpenDCP.Core.Models.Models.AuditTrail;
using OpenDCP.Framework.Hosting.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IAuditTrailService
    {
        Task<MemoryStream> GetConfigurationAuditTrailAsync(DateTime startTime, DateTime endTime, UserContextDTO user);
        Task<MemoryStream> GetAdminActionsAuditTrailAsync(DateTime startTime, DateTime endTime, UserContextDTO user);
        Task<MemoryStream> GetSecurityAuditTrailAsync(DateTime startTime, DateTime endTime, UserContextDTO user);
        Task CreateAdminActionAuditTrailEntryAsync(AuditTrailAdminActionsEntryDTO model, UserContextDTO user);
    }
}
