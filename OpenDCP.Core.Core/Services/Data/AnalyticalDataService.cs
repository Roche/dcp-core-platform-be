using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.DAL;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Analytical.Interfaces;
using OpenDCP.Framework.DataTypes.Analytical.Requests.AnalyticalData;
using OpenDCP.Framework.DataTypes.Analytical.Responses.AnalyticalData;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace OpenDCP.Core.Core.Services.Data
{
    public class AnalyticalDataService : BaseDataService, IAnalyticalDataService
    {
        private readonly DbConnectionStringLoader _loader;

        public AnalyticalDataService(IConfiguration configuration, DCPCoreDbContext dbContext, IServiceProvider serviceProvider, DatasourceCache cache)
            : base(configuration, dbContext, serviceProvider, cache)
        {
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<ServiceResponse<AttributeDataResponse>> GetAttributeDataAsync(int siteId, string module, AttributeDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<AttributeDataQueryDTO, AttributeDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultDecodeFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var translatePipeline = new EventTranslationPipeline(_cache, _serviceProvider, _loader);

            var dict = await translatePipeline.TranslateForward<AttributeDataQueryDTO>(
                siteId: siteId,
                sDataType: Framework.Configuration.Model.Constants.Datasource.DataType.EventframeContext,
                tDataType: Framework.Configuration.Model.Constants.Datasource.DataType.AnalyticalData,
                model
            );

            var response = await pipeline.RunMapReduceAsync<IAnalyticalDataSource>(
                (datasource, model, ct) => datasource.GetAnalyticalDataAsync(model),
                dict.Item1
            );

            translatePipeline.TranslateBackward(dict.Item2, response);

            var result = new ServiceResponse<AttributeDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<AttributeListResponse>> GetAttributesAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, AttributeListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAnalyticalDataSource>(
                async (datasource, args, ct) => await datasource.GetAttributesAsync((string)args[0]),
                mapArgIndex: 0,
                args: new object[] { webId }
            );

            var result = new ServiceResponse<AttributeListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }
    }
}
