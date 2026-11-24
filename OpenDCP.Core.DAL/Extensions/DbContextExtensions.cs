using OpenDCP.Core.DAL.Dialects;
using OpenDCP.Core.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OpenDCP.Core.DAL.Extensions
{
    public static class DCPCoreDbContextExtensions
    {
        public static async Task CleanDbEntries(this DCPCoreDbContext dbContext, int siteId, bool cleanHistory, 
            CancellationToken ct = default)
        {
            var dialect = GetDialect(dbContext);

            var tables = new List<string> { "ShareLink", "ShareLinkAlias", "UserInfoBanner", "DashboardTab", "DashboardExport" };

            await dialect.CleanupAsync(dbContext, siteId, ct);

            if (cleanHistory)
            {
                await dialect.DisableTemporalTablesAsync(dbContext, tables, ct);
                await dialect.CleanupHistoryAsync(dbContext, siteId, ct);
                await dialect.EnableTemporalTablesAsync(dbContext, tables, ct);
            }
        }

        public static async Task EnableTemporalTablesAsync(this DCPCoreDbContext dbContext, List<string> tables,
            CancellationToken ct = default)
        {
            var dialect = GetDialect(dbContext);
            await dialect.EnableTemporalTablesAsync(dbContext, tables, ct);
        }

        public static IRawSqlQueryDialect GetDialect(this DCPCoreDbContext dbContext)
        {

            if (dbContext.Database.IsNpgsql())
            {
                return new PostgreSqlDialect();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
