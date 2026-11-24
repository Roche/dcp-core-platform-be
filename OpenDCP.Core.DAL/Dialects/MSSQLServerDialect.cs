using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.DAL.Interfaces;
using OpenDCP.Framework.ShareLink.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace OpenDCP.Core.DAL.Dialects
{
    public sealed class MsSqlServerDialect : IRawSqlQueryDialect
    {
        public async Task CleanupAsync(DbContext db, int siteId, CancellationToken ct)
        {
            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM ShareLinkSystemGenerated
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE sla
            FROM ShareLinkAlias sla
            JOIN ShareLink sl ON sl.Id = sla.ShareLinkId
            WHERE sl.SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM ShareLink
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM UserInfoBanner
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM DashboardExport
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM DashboardTab
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE ipe
            FROM InProgressEvent ipe
            JOIN (
                SELECT Id, SiteId FROM DataSource
                UNION ALL
                SELECT Id, SiteId FROM DataSourceHistories
            ) ds ON ipe.DatasourceId = ds.Id
            WHERE ds.SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);
        }

        public async Task CleanupHistoryAsync(DbContext db, int siteId, CancellationToken ct)
        {
            await db.Database.ExecuteSqlRawAsync("""
                DELETE slah
                FROM ShareLinkAliasHistories slah
                JOIN ShareLink sl ON sl.Id = slah.ShareLinkId
                WHERE sl.SiteId = @siteId
                """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM UserInfoBannerHistories
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM DashboardExportHistories
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);

            await db.Database.ExecuteSqlRawAsync("""
            DELETE FROM DashboardTabHistories
            WHERE SiteId = @siteId
            """, new SqlParameter("@siteId", siteId), ct);
        }

        public async Task DisableTemporalTablesAsync(DbContext db, IEnumerable<string> tables, CancellationToken ct)
        {
            foreach (var table in tables)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"ALTER TABLE [dbo].[{table}] SET (SYSTEM_VERSIONING = OFF)",
                    ct);
            }
        }

        public async Task EnableTemporalTablesAsync(DbContext db, IEnumerable<string> tables, CancellationToken ct)
        {
            foreach (var table in tables)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $@"ALTER TABLE [dbo].[{table}] SET (SYSTEM_VERSIONING = ON)",
                    ct);
            }
        }

        public async Task<List<ShareLinkListItemDTO>> GetShareLinksAsync(DbContext db, int? siteId, int userId, CancellationToken ct)
        {
            return await db.Database
           .SqlQuery<ShareLinkListItemDTO>($@"
                SELECT 
                    LinkGuid AS Guid,
                    Title,
                    OwnerUserId,
                    SysStartTime AS LastModifiedAt,
                    JSON_value(JLink,'$.sharedPage') AS Page
                FROM ShareLink 
                WHERE SiteId = {siteId}
                  AND OwnerUserId = {userId}
                  AND NOT IsDeleted").ToListAsync();
        }

        public async Task<DashboardExport?> GetDashboardExportByGuidAsync(DbContext db, Guid exportGuid, CancellationToken ct = default)
        {
            return await db.Set<DashboardExport>()
                .FromSqlInterpolated($"""
                    SELECT TOP 1 * FROM (
                        SELECT * FROM [DashboardExport] WHERE [DashboardExportGuid] = {exportGuid}
                        UNION ALL
                        SELECT * FROM [DashboardExportHistories] WHERE [DashboardExportGuid] = {exportGuid}
                    ) combined
                """)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        public Task LockAsync(DbContext db, int key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task UnLockAsync(DbContext db, int key, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
