using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.CalculationNode;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Hosting.CustomAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Controller responsible for querying version information
    /// </summary>
    /// <remarks>
    /// This controller is part of the API version 2 ("api/v2/version/") and requires authorization
    /// for access. It inherits from <see cref="BaseController"/> and utilizes <see cref="IVersionService"/>
    /// to retrieve version information or perform version-related operations.
    /// </remarks>
    /// <seealso cref="BaseController"/>
    /// <seealso cref="IVersionService"/>
    [Route("api/v2/version/")]
    [Authorize]
    public class VersionController : BaseController
    {
        private readonly IVersionService _versionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="versionService">Service for retrieving version information.</param>
        public VersionController(IConfiguration configuration, IVersionService versionService) : base(configuration)
        {
            _versionService = versionService;
        }

        /// <summary>
        /// Get Core MicroService version 
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("backend/get")]
        [Produces(typeof(JsonObject))]
        [ApiGateway(IsExposed = true)]
        public IActionResult GetBEVersion()
        {
            Version? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            if (assemblyVersion is null)
                throw new InvalidOperationException("Can't read assembly");

            string versionDetails = string.Format(
                CultureInfo.InvariantCulture, @"{0}.{1}.{2}.{3}",
                assemblyVersion.Major,
                assemblyVersion.Minor, 
                assemblyVersion.Build,
                assemblyVersion.Revision
             );

            var result = new {
                CoreVersion = versionDetails
            };

            return Ok(JsonSerializer.Serialize(result));
        }

        /// <summary>
        /// Get the version of all registered calculation nodes and their packages
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("calculation-node/get")]
        [Produces(typeof(List<SiteCalculationNodeInfoDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetCalculationNodeVersion()
        {
            var result = await _versionService.GetCalculationNodeVersionsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Get the version of all registered datasources
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("datasources/get")]
        [Produces(typeof(List<RegisteredDataInterfaceDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetDatasourceVersions()
        {
            var result = await _versionService.GetRegisteredDatasourcesAsync();
            return Ok(result);
        }
    }
}
