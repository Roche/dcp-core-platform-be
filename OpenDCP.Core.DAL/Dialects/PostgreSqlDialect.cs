using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.DAL.Interfaces;
using OpenDCP.Framework.ShareLink.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace OpenDCP.Core.DAL.Dialects
{
    public sealed class PostgreSqlDialect : IRawSqlQueryDialect
    {
        public async Task CleanupAsync(DbContext db, int siteId, CancellationToken ct)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
             $"""
            DELETE FROM "ShareLinkSystemGenerated"
            WHERE "SiteId" = {siteId}
            """,
             ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
                   $"""
                    DELETE FROM "ShareLinkAlias" sla
                    USING "ShareLink" sl
                    WHERE sl."Id" = sla."ShareLinkId"
                      AND sl."SiteId" = {siteId}
                    """,
                                ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "ShareLink"
                WHERE "SiteId" = {siteId}
                """,
                ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "UserInfoBanner"
                WHERE "SiteId" = {siteId}
                """,
                ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "DashboardExport"
                WHERE "SiteId" = {siteId}
                """,
                ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "DashboardTab"
                WHERE "SiteId" = {siteId}
                """,
                ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "InProgressEvent" ipe
                USING (
                    SELECT "Id", "SiteId" FROM "DataSource"
                    UNION ALL
                    SELECT "Id", "SiteId" FROM "DataSource_history"
                ) ds
                WHERE ipe."DatasourceId" = ds."Id"
                  AND ds."SiteId" = {siteId}
                """,
                ct);

        }

        public async Task CleanupHistoryAsync(DbContext db, int siteId, CancellationToken ct)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM "ShareLinkAlias_history" slah
                USING "ShareLink" sl
                WHERE sl."Id" = slah."ShareLinkId"
                  AND sl."SiteId" = {siteId}
                """,
               ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
              $"""
                DELETE FROM "ShareLink_history"
                WHERE "SiteId" = {siteId}
                """,
              ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
               $"""
                DELETE FROM "UserInfoBanner_history"
                WHERE "SiteId" = {siteId}
                """,
               ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
               $"""
                DELETE FROM "DashboardExport_history"
                WHERE "SiteId" = {siteId}
                """,
               ct);

            await db.Database.ExecuteSqlInterpolatedAsync(
               $"""
                DELETE FROM "DashboardTab_history"
                WHERE "SiteId" = {siteId}
                """,
               ct);
        }

        public async Task DisableTemporalTablesAsync(DbContext db, IEnumerable<string> tables, CancellationToken ct)
        {
            foreach (var table in tables)
            {
                var escapedTable = table.Replace("\"", "\\\"");
                var sql = $@"SELECT periods.drop_system_versioning('""{escapedTable}""');";

                await db.Database.ExecuteSqlRawAsync(sql, ct);

                var grantSql = $@"GRANT DELETE ON TABLE ""{table}_history"" TO CURRENT_USER;";
                await db.Database.ExecuteSqlRawAsync(grantSql, ct);
            }
        }

        public async Task EnableTemporalTablesAsync(DbContext db, IEnumerable<string> tables, CancellationToken ct)
        {
            foreach (var table in tables)
            {
                try
                {
                    var escapedTable = table.Replace("\"", "\\\"");

                    var sql = $@"SELECT periods.add_system_versioning('""{escapedTable}""');";

                    await db.Database.ExecuteSqlRawAsync(sql, ct);
                }
                catch (PostgresException ex) when (ex.SqlState == "P0001")
                {
                    // Already versioned
                }

            }
        }

        public async Task LockAsync(DbContext db, int key, CancellationToken ct)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_lock({key});",
                cancellationToken: ct);
        }

        public async Task UnLockAsync(DbContext db, int key, CancellationToken ct)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_unlock({key});",
                cancellationToken: ct);
        }

        public async Task<List<ShareLinkListItemDTO>> GetShareLinksAsync(DbContext db, int? siteId, int userId, CancellationToken ct)
        {
            return await db.Database
             .SqlQuery<ShareLinkListItemDTO>($@"
                SELECT 
                    sl.""LinkGuid"" AS ""Guid"",
                    sl.""Title"",
                    sl.""OwnerUserId"",
                    sl.""SysStartTime"" AS ""LastModifiedAt"",
                    sl.""JLink""::jsonb ->> 'sharedPage' AS ""Page""
                FROM ""ShareLink"" sl
                WHERE sl.""SiteId"" = {siteId}
                  AND sl.""OwnerUserId"" = {userId}
                  AND NOT sl.""IsDeleted""
            ").ToListAsync();
        }

        public async Task<DashboardExport?> GetDashboardExportByGuidAsync(DbContext db, Guid exportGuid, CancellationToken ct = default)
        {
            return await db.Set<DashboardExport>()
                .FromSqlInterpolated($"""
                    SELECT * FROM "DashboardExport" WHERE "DashboardExportGuid" = {exportGuid}
                    UNION ALL
                    SELECT * FROM "DashboardExport_history" WHERE "DashboardExportGuid" = {exportGuid}
                    LIMIT 1
                """)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }
    }
}
