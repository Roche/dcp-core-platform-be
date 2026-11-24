using OpenDCP.Core.DAL.Entities;
using OpenDCP.Framework.ShareLink.Models;
using Microsoft.EntityFrameworkCore;

namespace OpenDCP.Core.DAL.Interfaces
{
    public interface IRawSqlQueryDialect
    {
        Task DisableTemporalTablesAsync(
          DbContext db,
          IEnumerable<string> tables,
          CancellationToken ct);

        Task EnableTemporalTablesAsync(
            DbContext db,
            IEnumerable<string> tables,
            CancellationToken ct);

        Task CleanupAsync(
            DbContext db,
            int siteId,
            CancellationToken ct);

        Task CleanupHistoryAsync(
            DbContext db,
            int siteId,
            CancellationToken ct);

        Task LockAsync(
            DbContext db,
            int key,
            CancellationToken ct);

        Task UnLockAsync(
            DbContext db,
            int key,
            CancellationToken ct);
        public Task<List<ShareLinkListItemDTO>> GetShareLinksAsync(
            DbContext db,
            int? siteId,
            int userId,
            CancellationToken ct);
        Task<DashboardExport?> GetDashboardExportByGuidAsync(
            DbContext db,
            Guid exportGuid,
            CancellationToken ct = default);
    }
}
