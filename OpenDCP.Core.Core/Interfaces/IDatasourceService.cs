using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IDatasourceService
    {
        public Task DeleteDatasourceAsync(int datasourceId, string reason, UserContextDTO user);
        public Task<List<DatasourceListItemDTO>> SearchDatasourceDetailsAsync(DatasourceQueryDTO model, UserContextDTO user);
        public Task<DatasourceConfigurationForm> GetSingleDatasourceAsync(int datasourceId, UserContextDTO user);
        public Task<DatasourceConfigurationForm> CreateDatasourceAsync(DatasourceUserConfigurationDTO model, UserContextDTO user);
        public Task<DatasourceConfigurationForm> UpdateDatasourceAsync(int siteId, DatasourceUserConfigurationDTO model, UserContextDTO user);
    }
}
