using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;

namespace OpenDCP.Core.API.Controllers.v1
{
    /// <summary>
    /// Provides legacy version information for the Core MicroService.
    /// This controller is deprecated; use <c>VersionController</c> in API v2 instead.
    /// </summary>
    [Route("api/version")]
    [Route("api/v1/version/")]
    public class VersionLegacyController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionLegacyController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public VersionLegacyController(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Gets the version of the Core MicroService.
        /// </summary>
        /// <remarks>
        /// This endpoint is deprecated and will be removed in future releases.
        /// Use <c>/api/v2/version</c> instead.
        /// </remarks>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a JSON object with the Core MicroService version:
        /// <code>
        /// {
        ///     "CoreVersion": "major.minor.build.revision"
        /// }
        /// </code>
        /// </returns>
        [AllowAnonymous]
        [HttpGet]
        [Obsolete("This endpoint is deprecated. Please use /api/v2/version.")]
        [Route("GetBEVersion")]
        [Produces(typeof(JsonObject))]
        public IActionResult GetBEVersion()
        {
            Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version!;
            string versionDetails = string.Format(
                CultureInfo.InvariantCulture, @"{0}.{1}.{2}.{3}",
                assemblyVersion.Major,
                assemblyVersion.Minor,
                assemblyVersion.Build,
                assemblyVersion.Revision
            );
            var result = new JsonObject()
            {
                { "CoreVersion", versionDetails}
            };

            return this.Ok(result);
        }
    }
}
