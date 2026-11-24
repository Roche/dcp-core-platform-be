using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using OpenDCP.Framework.Connections.Manager.Constants;
using OpenDCP.Framework.FormBuilder.Models;
using OpenDCP.Framework.Hosting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides endpoints for managing system and module configurations, including datasource configurations,
    /// module settings, and API Gateway status. 
    /// This controller is part of API version 2.
    /// </summary>
    [Route("api/v2/configuration/")]
    public class ConfigurationController : BaseController
    {
        private readonly IConfigurationService _configurationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="configurationService">The configuration service used to retrieve and manage system and module configurations.</param>
        public ConfigurationController(IConfiguration configuration, IConfigurationService configurationService) : base(configuration)
        {
            _configurationService = configurationService;
        }

        /// <summary>
        /// Searches for datasource configurations based on the specified query parameters.
        /// </summary>
        /// <param name="model">
        /// The query parameters for searching datasource configurations, encapsulated in a <see cref="DatasourceQueryDTO"/> object.
        /// </param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>
        /// A <see cref="Task{IActionResult}"/> representing the result of the operation. 
        /// On success, returns a 200 OK response with a list of <see cref="DatasourceConfigurationDTO"/> objects matching the query.
        /// </returns>
        /// <remarks>
        /// This endpoint is intended to be used only internally server-to-server for modules consuming the configuration data, not for the frontend 
        /// </remarks>
        [HttpGet]
        [Route("datasource/query")]
        [Produces(typeof(List<DatasourceConfigurationDTO>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> SearchDatasource([FromQuery] DatasourceQueryDTO model,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();
            var result = await _configurationService.SearchDatasourceConfigurationAsync(model, user);
            return Ok(result);
        }


        /// <summary>
        /// Searches for datasource configurations based on the specified query parameters.
        /// </summary>
        /// <param name="model">
        /// The query parameters for searching datasource configurations, encapsulated in a <see cref="DatasourceQueryDTO"/> object.
        /// </param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>
        /// A <see cref="Task{IActionResult}"/> representing the result of the operation. 
        /// On success, returns a 200 OK response with a list of <see cref="DatasourcePublicConfigurationDTO"/> objects matching the query.
        /// </returns>
        /// <remarks>
        /// This endpoint is used for external systems having an overview on current connections 
        /// </remarks>
        [HttpGet]
        [Route("/api/v2/public/configuration/datasource/query")]
        [Produces(typeof(List<DatasourcePublicConfigurationDTO>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> PublicSearchDatasource([FromQuery] DatasourceQueryDTO model,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();
            var result = await _configurationService.PublicSearchDatasourceConfigurationAsync(model, user);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the connection string configuration form for a specific data source.
        /// </summary>
        /// <remarks>
        /// This endpoint fetches the connection string configuration form based on the provided <paramref name="type"/> (data source connection type) 
        /// and optionally a <paramref name="datasourceId"/>. The returned form contains input fields required for configuring the connection string.
        /// </remarks>
        /// <param name="type">
        /// The <see cref="ConnectionType"/> that specifies the type of data source connection (e.g., SQL, NoSQL, etc.).
        /// </param>
        /// <param name="datasourceId">
        /// An optional identifier of the data source to fetch the configuration form for. If not provided, the form will have no default values.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of <see cref="InputField"/> objects that represent the form's input fields required for configuring the data source connection string.
        /// </returns>
        [HttpGet]
        [Route("datasource/connection-string/form")]
        [Produces(typeof(List<InputField>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetDatasourceConnectionStringConfigurationForm([FromQuery] string type, int? datasourceId)
        {
            var user = GetUserContext();
            var result = await _configurationService.GetDatasourceConnectionStringConfigurationFormAsync(type, datasourceId, user);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the configuration details of a specific datasource by its unique identifier.
        /// </summary>
        /// <param name="datasourceId">
        /// The unique identifier of the datasource to retrieve. This parameter is required.
        /// </param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>
        /// A <see cref="Task{IActionResult}"/> representing the result of the operation.
        /// On success, returns a 200 OK response with the <see cref="DatasourceConfigurationDTO"/> representing the datasource configuration.
        /// </returns>
        /// <remarks>
        /// This endpoint is intended to be used only internally server-to-server for modules consuming the configuration data, not for the frontend 
        /// </remarks>
        [HttpGet]
        [Route("datasource/{datasourceId}/get")]
        [Produces(typeof(DatasourceConfigurationDTO))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetDatasource([BindRequired] int datasourceId,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();
            var result = await _configurationService.GetDatasourceConfigurationByIdAsync(datasourceId, user);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the connection string configuration form for a specific module type.
        /// </summary>
        /// <remarks>
        /// This endpoint is used to fetch a form schema that defines the input fields required 
        /// to configure a connection string for a module, based on the specified calculation node type.
        /// </remarks>
        /// <param name="type">
        /// The <see cref="CalculationNodeType"/> parameter specifies the type of the calculation node.
        /// </param>
        /// <param name="moduleId">
        /// The optional <c>moduleId</c> parameter represents the ID of the module for which the connection string 
        /// configuration form is requested. If not provided, the system may return a default or global form.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing an HTTP 200 OK response with a list of 
        /// <see cref="InputField"/> objects representing the configuration form.
        /// </returns>
        /// <response code="200">
        /// Returns a list of <see cref="InputField"/> objects that define the form structure.
        /// </response>
        /// <remark>
        /// This endpoint is only intended to be used by the frontend for the user interaction for configuration and should not be used elsewhere
        /// </remark>
        [HttpGet]
        [Route("module/connection-string/form")]
        [Produces(typeof(List<InputField>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetModuleConnectionStringConfigurationForm([FromQuery] CalculationNodeType type, int? moduleId)
        {
            var user = GetUserContext();
            var result = await _configurationService.GetModuleConnectionStringConfigurationFormAsync(type, moduleId, user);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the connection string configuration form for a specific connection bridge strategy
        /// </summary>
        /// <remarks>
        /// This endpoint is used to fetch a form schema that defines the input fields required 
        /// to configure a connection string for a connection bridge based on a specified strategy
        /// </remarks>
        /// <param name="type">
        /// The <see cref="ConnectionBridgeStrategyType"/> parameter specifies the type of the calculation node.
        /// </param>
        /// <param name="connectionBridgeId">
        /// The optional <c>connectionBridgeId</c> parameter represents the ID of the connection bridge
        /// configuration form is requested. If not provided, the system may return a default or global form.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing an HTTP 200 OK response with a list of 
        /// <see cref="InputField"/> objects representing the configuration form.
        /// </returns>
        /// <response code="200">
        /// Returns a list of <see cref="InputField"/> objects that define the form structure.
        /// </response>
        /// <remark>
        /// This endpoint is only intended to be used by the frontend for the user interaction for configuration and should not be used elsewhere
        /// </remark>
        [HttpGet]
        [Route("connection-bridge/connection-string/form")]
        [Produces(typeof(List<InputField>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetConnectionBridgeConfigurationForm([FromQuery] ConnectionBridgeStrategyType type, int? connectionBridgeId)
        {
            var user = GetUserContext();
            var result = await _configurationService.GetConnectionBridgeConfigurationFormAsync(type, connectionBridgeId, user);
            return Ok(result);
        }

        /// <summary>
        /// Searches for module configurations based on the specified query parameters.
        /// </summary>
        /// <remarks>
        /// This endpoint performs a search for module configurations, using the provided query model (<paramref name="model"/>), 
        /// and returns a list of matching configurations.
        /// It is not exposed through the API Gateway as indicated by the <see cref="ApiGatewayAttribute"/> with <c>IsExposed</c> set to <c>false</c>.
        /// The operation considers the current user's context when performing the search.
        /// </remarks>
        /// <param name="model">
        /// A <see cref="ModuleQueryDTO"/> object containing the query parameters for searching module configurations.
        /// </param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of <see cref="ModuleConfigurationDTO"/> objects representing the matching module configurations.
        /// </returns>
        [HttpGet]
        [Route("module/query")]
        [Produces(typeof(List<ModuleConfigurationDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> SearchModule([FromQuery] ModuleQueryDTO model,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();
            var result = await _configurationService.SearchModuleConfigurationAsync(model, user);
            return Ok(result);
        }


        /// <summary>
        /// Retrieves the configuration details for a specific module based on its unique identifier.
        /// </summary>
        /// <param name="moduleId">The unique identifier of the module to retrieve.</param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the module configuration details as a <see cref="ModuleConfigurationDTO"/> object.
        /// </returns>
        /// <response code="200">Returns the module configuration details.</response>
        /// <response code="400">If the <paramref name="moduleId"/> is invalid or missing.</response>
        /// <response code="404">If no module with the specified <paramref name="moduleId"/> exists.</response>
        /// <remarks>
        /// This endpoint is intended to be used only internally server-to-server for modules consuming the configuration data, not for the frontend 
        /// </remarks>
        [HttpGet]
        [Route("module/{moduleId}/get")]
        [Produces(typeof(ModuleConfigurationDTO))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetModule([BindRequired] int moduleId,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();
            var result = await _configurationService.GetModuleConfigurationByIdAsync(moduleId, user);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a list of modules and their associated data types.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a list of modules as <see cref="Models.Models.Module.ModuleSystemConfigurationDTO"/> objects.
        /// </returns>
        /// <response code="200">Returns the list of modules and their data types.</response>
        /// <response code="500">If an unexpected error occurs on the server.</response>
        /// <remarks>
        /// This endpoint is intended to be used only internally server-to-server for modules consuming the configuration data, not for the frontend 
        /// </remarks>
        [HttpGet]
        [Route("system")]
        [Produces(typeof(SystemConfigurationDTO))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> GetModuleDataTypes()
        {
            var user = GetUserContext();
            var result = await _configurationService.GetActiveSystemConfigurationAsync(user);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the current status of the API Gateway, including active service configurations.
        /// </summary>
        /// <remarks>
        /// This endpoint returns a list of active API service configurations available in the system.
        /// </remarks>
        [HttpGet]
        [Route("gateway/status")]
        [Produces(typeof(List<ApiServiceConfigurationDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetApiGatewayStatus()
        {
            var user = GetUserContext();
            var result = await _configurationService.GetApiGatewayStatusAsync(user);
            return Ok(result);
        }

        /// <summary>
        /// Updates the API Gateway's status with the specified service configurations.
        /// </summary>
        /// <remarks>
        /// This endpoint accepts a list of API service configurations as input and updates the status of the API Gateway accordingly.
        /// </remarks>
        [HttpPut]
        [Route("gateway/status")]
        [Produces(typeof(List<ApiServiceConfigurationDTO>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> SetApiGatewayStatus([FromBody] List<ApiServiceConfigurationDTO> model, string reason)
        {
            var user = GetUserContext();
            var result = await _configurationService.SetApiGatewayStatusAsync(model, user, reason);
            return Ok(result);
        }

        /// <summary>
        /// Reloads the API Gateway's route cache for the provided services
        /// </summary>
        /// <remarks>
        /// This endpoint allows for reloading the API Gateway's status using a specified list of API service configurations.
        /// </remarks>
        [HttpPut]
        [Route("gateway/reload")]
        [Produces(typeof(List<ApiServiceConfigurationDTO>))]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> ReloadApiGatewayStatus([FromBody] List<ApiServiceConfigurationDTO> model, string reason)
        {
            var user = GetUserContext();
            var result = await _configurationService.ReloadApiGatewayStatusAsync(model, user, reason);
            return Ok(result);
        }
    }
}
