using OpenDCP.Framework.Dashboard.Models;
using OpenDCP.Framework.Dashboard.Requests;
using OpenDCP.Framework.Hosting.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardTabDTO> CreateDashboardTabAsync(DashboardTabConfigurationDTO model, UserContextDTO user);
        Task<DashboardTabDTO> UpdateDashboardTabAsync(Guid dashboardTabId, DashboardTabConfigurationDTO model, UserContextDTO user);
        Task<List<DashboardTabDTO>> SearchDashboardTabsAsync(UserContextDTO user, DashboardTabQueryDTO? model = null);
        Task DeleteDashboardTabAsync(Guid dashboardTabId, UserContextDTO user);
        Task<Guid> ExportDashboardAsync(Guid dashboardTabId, UserContextDTO user);
        Task<Guid> ImportDashboardAsync(DashboardImportDTO model, UserContextDTO user);
        Task DeleteDashboardExportAsync(Guid dashboardExportGuid, UserContextDTO user);
        Task<List<DashboardExportDTO>> SearchDashboardExportAsync(UserContextDTO user, DashboardExportQueryDTO? model = null);
        Task UpdateDashboardTabLayoutAsync(int siteId, DashboardTabLayoutConfigurationDTO model, UserContextDTO user);
    }
}
